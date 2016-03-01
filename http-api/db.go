package main

import (
	"encoding/json"
	"fmt"
	"time"

	"github.com/google/go-github/github"
	"github.com/jackc/pgx"
)

var githubClient *github.Client

var connPool *pgx.ConnPool

func (r *Run) ensureBenchmarksAndMetricsExist(database *pgx.Tx, benchmarks map[string]bool) *requestError {
	if benchmarks != nil && !benchmarks[r.Benchmark] {
		_, err := database.Exec("insertBenchmark", r.Benchmark)
		if err != nil {
			return badRequestError("Couldn't insert Benchmark: " + r.Benchmark)
		}
		benchmarks[r.Benchmark] = true
	}
	for m, v := range r.Results {
		if !metricIsAllowed(m, v) {
			return badRequestError("Metric not supported or results of wrong type: " + m)
		}
	}
	return nil
}

func ensureProductExists(database *pgx.Tx, product Product) (string, *requestError) {
	var commitDate time.Time
	// FIXME: check and update mergeBaseHash
	err := database.QueryRow("queryCommitWithProduct", product.Commit, product.Name).Scan(&commitDate)
	if err == nil {
		return product.Commit, nil
	}

	if err == pgx.ErrNoRows {
		var sha string
		fmt.Printf("Commit %s for product %s not found in DB.\n", product.Commit, product.Name)

		owner, repo := githubRepoForProduct(product.Name)
		if owner == "" || repo == "" {
			return "", badRequestError("Unknown product")
		}

		commit, _, err := githubClient.Git.GetCommit(owner, repo, product.Commit)
		if err == nil {
			sha = *commit.SHA
			commitDate = *commit.Committer.Date
		} else {
			// try to fall back to provided commitDate
			if product.CommitDate != nil {
				sha = product.Commit
				commitDate = *product.CommitDate
			} else {
				return "", badRequestError("Could not get date for provided commit " + product.Commit)
			}
		}

		_, err = database.Exec("insertCommit", sha, commitDate, product.Name, product.MergeBaseHash)
		if err != nil {
			return "", internalServerError("Couldn't insert commit")
		}

		return sha, nil
	}

	return "", internalServerError("Database query error")
}

func fetchConfig(database *pgx.Tx, name string) (*Config, *requestError) {
	dbConfig := Config{Name: name}
	err := database.QueryRow("queryConfig", name).Scan(&dbConfig.MonoExecutable, &dbConfig.MonoEnvironmentVariables, &dbConfig.MonoOptions)
	if err == pgx.ErrNoRows {
		return nil, nil
	}
	if err != nil {
		return nil, internalServerError("Database query error")

	}
	return &dbConfig, nil
}

func ensureConfigExists(database *pgx.Tx, config Config) *requestError {
	dbConfig, reqErr := fetchConfig(database, config.Name)
	if reqErr != nil {
		return reqErr
	}

	if dbConfig != nil {
		if !config.isSameAs(dbConfig) {
			return badRequestError("Config does not match the one in the database")
		}
		return nil
	}

	envVars, err := json.Marshal(config.MonoEnvironmentVariables)
	if err != nil {
		return internalServerError("Could not marshal mono environment variables to JSON")
	}

	_, err = database.Exec("insertConfig", config.Name, config.MonoExecutable, string(envVars), config.MonoOptions)
	if err != nil {
		fmt.Printf("config insert error: %s\n", err.Error())
		return internalServerError("Could not insert config")
	}

	return nil
}

func ensureMachineExists(database *pgx.Tx, machine Machine) *requestError {
	var architecture string
	err := database.QueryRow("queryMachine", machine.Name).Scan(&architecture)
	if err == nil {
		if machine.Architecture != architecture {
			return badRequestError("Machine has a different architecture than the one in the database")
		}
		return nil
	}

	if err == pgx.ErrNoRows {
		_, err = database.Exec("insertMachine", machine.Name, machine.Architecture)
		if err != nil {
			return internalServerError("Could not insert machine")
		}

		return nil
	}

	return internalServerError("Database query error")
}

func fetchBenchmarks(database *pgx.Tx) (map[string]bool, *requestError) {
	rows, err := database.Query("queryBenchmarks")
	if err != nil {
		return nil, internalServerError("Could not get benchmarks")
	}
	defer rows.Close()

	names := make(map[string]bool)
	for rows.Next() {
		var name string
		if err = rows.Scan(&name); err != nil {
			return nil, internalServerError("Could not scan benchmark name")
		}
		names[name] = true
	}

	return names, nil
}

func (rs *RunSet) ensureBenchmarksAndMetricsExist(database *pgx.Tx) *requestError {
	benchmarks, reqErr := fetchBenchmarks(database)
	if reqErr != nil {
		return reqErr
	}

	for _, run := range rs.Runs {
		reqErr = run.ensureBenchmarksAndMetricsExist(database, benchmarks)
		if reqErr != nil {
			return reqErr
		}
	}

	for _, b := range rs.TimedOutBenchmarks {
		if !benchmarks[b] {
			return badRequestError("Benchmark does not exist: " + b)
		}
	}
	for _, b := range rs.CrashedBenchmarks {
		if !benchmarks[b] {
			return badRequestError("Benchmark does not exist: " + b)
		}
	}

	return nil
}

func fetchRunSet(database *pgx.Tx, id int32, withRuns bool) (*RunSet, *requestError) {
	var rs RunSet
	var secondaryCommits []string
	var pullRequestID *int32
	err := database.QueryRow("queryRunSet", id).Scan(&rs.StartedAt, &rs.FinishedAt,
		&rs.BuildURL, &rs.LogURLs,
		&rs.MainProduct.Commit, &secondaryCommits, &rs.Machine.Name, &rs.Config.Name,
		&rs.TimedOutBenchmarks, &rs.CrashedBenchmarks,
		&pullRequestID)
	if err == pgx.ErrNoRows {
		return nil, badRequestError("Run set does not exist")
	}
	if err != nil {
		fmt.Printf("run set query error: %s\n", err.Error())
		return nil, internalServerError("Could not get run set")
	}

	// FIXME: combine these two queries with the above one
	err = database.QueryRow("queryCommit", rs.MainProduct.Commit).Scan(&rs.MainProduct.Name)
	if err != nil {
		return nil, internalServerError("Could not get commit")
	}

	for _, c := range secondaryCommits {
		product := Product{Commit: c}
		err = database.QueryRow("queryCommit", c).Scan(&product.Name)
		if err != nil {
			return nil, internalServerError("Could not get commit")
		}

		rs.SecondaryProducts = append(rs.SecondaryProducts, product)
	}

	err = database.QueryRow("queryMachine", rs.Machine.Name).Scan(&rs.Machine.Architecture)
	if err != nil {
		return nil, internalServerError("Could not get machine")
	}

	config, reqErr := fetchConfig(database, rs.Config.Name)
	if reqErr != nil {
		return nil, nil
	}

	rs.Config = *config

	if withRuns {
		rows, err := database.Query("queryRunMetrics", id)
		if err != nil {
			return nil, internalServerError("Could not get run metrics")
		}
		defer rows.Close()

		runs := make(map[int32]*Run)

		for rows.Next() {
			var runID int32
			var benchmark string
			var metric string
			var resultNumber *float64
			var resultArray []float64

			if err = rows.Scan(&runID, &benchmark, &metric, &resultNumber, &resultArray); err != nil {
				return nil, internalServerError("Could not scan run metric: " + err.Error())
			}

			var resultValue interface{}
			if metricIsArray(metric) {
				resultValue = resultArray
			} else if resultNumber != nil {
				resultValue = *resultNumber
			} else {
				return nil, internalServerError("Run metric data is inconsistent")
			}

			run := runs[runID]
			if run == nil {
				run = &Run{Benchmark: benchmark, Results: make(map[string]interface{})}
				runs[runID] = run
			}

			run.Results[metric] = resultValue
		}

		for _, r := range runs {
			rs.Runs = append(rs.Runs, *r)
		}
	}

	return &rs, nil
}

func insertResults(database *pgx.Tx, runID int32, results Results, update bool) *requestError {
	idByMetric := make(map[string]int32)
	if update {
		rows, err := database.Query("queryRunMetricsForRun", runID)
		if err != nil {
			return internalServerError("Could not query run metrics")
		}
		for rows.Next() {
			var runMetricID int32
			var metric string
			err = rows.Scan(&runMetricID, &metric)
			if err != nil {
				return internalServerError("Could not scan run metric")
			}
			idByMetric[metric] = runMetricID
		}
	}

	for m, v := range results {
		var err error
		runMetricID, exists := idByMetric[m]
		if metricIsArray(m) {
			var arr []float64
			for _, x := range v.([]interface{}) {
				arr = append(arr, x.(float64))
			}
			if exists {
				_, err = database.Exec("updateRunMetricArray", runMetricID, arr)
			} else {
				_, err = database.Exec("insertRunMetricArray", runID, m, arr)
			}
		} else {
			if exists {
				_, err = database.Exec("updateRunMetricNumber", runMetricID, v)
			} else {
				_, err = database.Exec("insertRunMetricNumber", runID, m, v)
			}
		}
		if err != nil {
			fmt.Printf("run metric insert error: %s\n", err)
			return internalServerError("Could not insert run metric")
		}
	}
	return nil
}

func insertRuns(database *pgx.Tx, runSetID int32, runs []Run) ([]int32, *requestError) {
	var runIDs []int32
	for _, run := range runs {
		var runID int32
		err := database.QueryRow("insertRun", run.Benchmark, runSetID).Scan(&runID)
		if err != nil {
			fmt.Printf("run insert error: %s\n", err)
			return nil, internalServerError("Could not insert run")
		}
		runIDs = append(runIDs, runID)

		reqErr := insertResults(database, runID, run.Results, false)
		if reqErr != nil {
			return nil, reqErr
		}
	}
	return runIDs, nil
}

func updateRunSet(database *pgx.Tx, runSetID int32, rs *RunSet) *requestError {
	_, err := database.Exec("updateRunSet", runSetID, rs.LogURLs, rs.TimedOutBenchmarks, rs.CrashedBenchmarks)
	if err != nil {
		fmt.Printf("update run set error: %s\n", err)
		return internalServerError("Could not update run set")
	}
	return nil
}

func deleteRunSet(database *pgx.Tx, runSetID int32) (int64, int64, *requestError) {
	res, err := database.Exec("deleteRunMetricByRunSetId", runSetID)
	if err != nil {
		fmt.Printf("delete runmetric by runsetid: %s\n", err)
		return 0, 0, internalServerError("Could not delete runset")
	}
	numMetrics := res.RowsAffected()

	res, err = database.Exec("deleteRunByRunSetId", runSetID)
	if err != nil {
		fmt.Printf("delete run by runsetid: %s\n", err)
		return 0, 0, internalServerError("Could not delete runset")
	}
	numRuns := res.RowsAffected()

	_, err = database.Exec("deleteRunSet", runSetID)
	if err != nil {
		fmt.Printf("delete runset: %s\n", err)
		return 0, 0, internalServerError("Could not delete runset")
	}

	return numMetrics, numRuns, nil
}

func fetchRunSetSummaries(database *pgx.Tx, machine string, config string) ([]RunSetSummary, *requestError) {
	rows, err := database.Query("queryRunSetSummaries", machine, config)
	if err != nil {
		return nil, internalServerError("Could not fetch run set summaries")
	}
	var summaries []RunSetSummary
	for rows.Next() {
		var rss RunSetSummary
		if err = rows.Scan(&rss.ID, &rss.MainProduct.Commit); err != nil {
			return nil, internalServerError("Could not scan run set summary")
		}
		summaries = append(summaries, rss)
	}
	return summaries, nil
}

func initGitHub() {
	githubClient = github.NewClient(nil)
}

func prepareStatements(database *pgx.Conn) error {
	var err error

	_, err = database.Prepare("queryCommit", "select product from commit where hash = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("queryCommitWithProduct", "select commitDate from commit where hash = $1 and product = $2")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertCommit", "insert into commit (hash, commitDate, product, mergeBaseHash) values ($1, $2, $3, $4)")
	if err != nil {
		return err
	}

	_, err = database.Prepare("queryBenchmarks", "select name from benchmark")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertBenchmark", "insert into benchmark (name, disabled) values ($1, NULL)")
	if err != nil {
		return err
	}

	_, err = database.Prepare("queryConfig", "select monoExecutable, monoEnvironmentVariables, monoOptions from config where name = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertConfig", "insert into config (name, monoExecutable, monoEnvironmentVariables, monoOptions) values ($1, $2, $3, $4)")
	if err != nil {
		return err
	}

	_, err = database.Prepare("queryMachine", "select architecture from machine where name = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertMachine", "insert into machine (name, architecture, isDedicated) values ($1, $2, false)")
	if err != nil {
		return err
	}

	runSetColumns := "startedAt, finishedAt, buildURL, logURLs, commit, secondaryCommits, machine, config, timedOutBenchmarks, crashedBenchmarks, pullRequest"
	_, err = database.Prepare("queryRunSet", "select "+runSetColumns+" from runset where id = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertRunSet", "insert into runSet ("+runSetColumns+") values ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11) returning id")
	if err != nil {
		return err
	}

	_, err = database.Prepare("updateRunSet", "update runSet set logURLs = $2, timedOutBenchmarks = $3, crashedBenchmarks = $4 where id = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("deleteRunSet", "delete from RunSet where id = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertRun", "insert into run (benchmark, runSet) values ($1, $2) returning id")
	if err != nil {
		return err
	}

	_, err = database.Prepare("deleteRunByRunSetId", "delete from Run where runSet = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("queryRunMetrics", "select r.id, r.benchmark, rm.metric, rm.result, rm.resultArray from run r, runMetric rm where rm.run = r.id and r.runSet = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("queryRunMetricsForRun", "select rm.id, rm.metric from runMetric rm where rm.run = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertRunMetricNumber", "insert into runMetric (run, metric, result) values ($1, $2, $3)")
	if err != nil {
		return err
	}

	_, err = database.Prepare("updateRunMetricNumber", "update runMetric set result = $2 where id = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertRunMetricArray", "insert into runMetric (run, metric, resultArray) values ($1, $2, $3)")
	if err != nil {
		return err
	}

	_, err = database.Prepare("deleteRunMetricByRunSetId", "delete from RunMetric where run in (select id from Run where runSet = $1)")
	if err != nil {
		return err
	}

	_, err = database.Prepare("updateRunMetricArray", "update runMetric set resultArray = $2 where id = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("queryRunSetSummaries", "select id, commit from runSet where machine = $1 and config = $2")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertPullRequest", "insert into pullRequest (baselineRunSet, url) values ($1, $2) returning id")
	if err != nil {
		return err
	}

	return nil
}

func initDatabase() error {
	host, err := getCredentialString("benchmarkerPostgres", "host")
	if err != nil {
		return err
	}
	portFloat, err := getCredentialNumber("benchmarkerPostgres", "port")
	if err != nil {
		return err
	}
	port := uint16(portFloat)
	dbName, err := getCredentialString("benchmarkerPostgres", "database")
	if err != nil {
		return err
	}
	user, err := getCredentialString("benchmarkerPostgres", "user")
	if err != nil {
		return err
	}
	password, err := getCredentialString("benchmarkerPostgres", "password")
	if err != nil {
		return err
	}

	connConfig := pgx.ConnConfig{
		Host:     host,
		Port:     port,
		Database: dbName,
		User:     user,
		Password: password,
	}
	poolConfig := pgx.ConnPoolConfig{
		ConnConfig:     connConfig,
		MaxConnections: 3,
		AfterConnect:   prepareStatements,
	}
	connPool, err = pgx.NewConnPool(poolConfig)
	if err != nil {
		return err
	}

	return nil
}

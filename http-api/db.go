package main

import (
	"encoding/json"
	"errors"
	"fmt"
	"io/ioutil"
	"time"

	"github.com/google/go-github/github"
	"github.com/jackc/pgx"
)

var githubClient *github.Client

var database *pgx.Conn

func ensureProductExists(product Product) (string, *requestError) {
	var commitDate time.Time
	err := database.QueryRow("queryCommitWithProduct", product.Commit, product.Name).Scan(&commitDate)
	if err == nil {
		return product.Commit, nil
	}

	if err == pgx.ErrNoRows {
		fmt.Printf("Commit %s for product %s not found in DB.\n", product.Commit, product.Name)

		owner, repo := githubRepoForProduct(product.Name)
		if owner == "" || repo == "" {
			return "", badRequestError("Unknown product")
		}

		commit, _, err := githubClient.Git.GetCommit(owner, repo, product.Commit)
		if err != nil {
			return "", badRequestError("Could not get commit")
		}

		_, err = database.Exec("insertCommit", *commit.SHA, *commit.Committer.Date, product.Name)
		if err != nil {
			return "", internalServerError("Couldn't insert commit")
		}

		return *commit.SHA, nil
	}

	return "", internalServerError("Database query error")
}

func fetchConfig(name string) (*Config, *requestError) {
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

func ensureConfigExists(config Config) *requestError {
	dbConfig, reqErr := fetchConfig(config.Name)
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

func ensureMachineExists(machine Machine) *requestError {
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

func fetchBenchmarks() (map[string]bool, *requestError) {
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

func fetchRunSet(id int32) (*RunSet, *requestError) {
	var rs RunSet
	var secondaryCommits []string
	err := database.QueryRow("queryRunSet", id).Scan(&rs.StartedAt, &rs.FinishedAt,
		&rs.BuildURL, &rs.LogURLs,
		&rs.MainProduct.Commit, &secondaryCommits, &rs.Machine.Name, &rs.Config.Name,
		&rs.TimedOutBenchmarks, &rs.CrashedBenchmarks)
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

	config, reqErr := fetchConfig(rs.Config.Name)
	if reqErr != nil {
		return nil, nil
	}

	rs.Config = *config

	return &rs, nil
}

func insertRuns(runSetID int32, runs []Run) *requestError {
	for _, run := range runs {
		var runID int32
		err := database.QueryRow("insertRun", run.Benchmark, runSetID).Scan(&runID)
		if err != nil {
			fmt.Printf("run insert error: %s\n", err)
			return internalServerError("Could not insert run")
		}

		for m, v := range run.Results {
			if metricIsArray(m) {
				var arr []float64
				for _, x := range v.([]interface{}) {
					arr = append(arr, x.(float64))
				}
				_, err = database.Exec("insertRunMetricArray", runID, m, arr)
			} else {
				_, err = database.Exec("insertRunMetricNumber", runID, m, v)
			}
			if err != nil {
				fmt.Printf("run metric insert error: %s\n", err)
				return internalServerError("Could not insert run metric")
			}
		}
	}
	return nil
}

func updateRunSet(runSetID int32, rs *RunSet) *requestError {
	_, err := database.Exec("updateRunSet", runSetID, rs.LogURLs, rs.TimedOutBenchmarks, rs.CrashedBenchmarks)
	if err != nil {
		fmt.Printf("update run set error: %s\n", err)
		return internalServerError("Could not update run set")
	}
	return nil
}

func initGitHub() {
	githubClient = github.NewClient(nil)
}

func initDatabase(credentialsFile string) error {
	credentialsData, err := ioutil.ReadFile(credentialsFile)
	if err != nil {
		return err
	}

	var credentialsMap map[string]map[string]interface{}
	err = json.Unmarshal(credentialsData, &credentialsMap)
	if err != nil {
		return err
	}

	postgresMap, ok := credentialsMap["benchmarkerPostgres"]
	if !ok {
		return errors.New("No benchmarkerPostgres credentials in credentials file")
	}

	host := postgresMap["host"].(string)
	port := uint16(postgresMap["port"].(float64))
	dbName := postgresMap["database"].(string)
	user := postgresMap["user"].(string)
	password := postgresMap["password"].(string)

	config := pgx.ConnConfig{
		Host:     host,
		Port:     port,
		Database: dbName,
		User:     user,
		Password: password,
	}
	db, err := pgx.Connect(config)
	database = db

	if err != nil {
		return err
	}

	_, err = database.Prepare("queryCommit", "select product from commit where hash = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("queryCommitWithProduct", "select commitDate from commit where hash = $1 and product = $2")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertCommit", "insert into commit (hash, commitDate, product) values ($1, $2, $3)")
	if err != nil {
		return err
	}

	_, err = database.Prepare("queryBenchmarks", "select name from benchmark")
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

	runSetColumns := "startedAt, finishedAt, buildURL, logURLs, commit, secondaryCommits, machine, config, timedOutBenchmarks, crashedBenchmarks"
	_, err = database.Prepare("queryRunSet", "select "+runSetColumns+" from runset where id = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertRunSet", "insert into runSet ("+runSetColumns+") values ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10) returning id")
	if err != nil {
		return err
	}

	_, err = database.Prepare("updateRunSet", "update runSet set logURLs = $2, timedOutBenchmarks = $3, crashedBenchmarks = $4 where id = $1")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertRun", "insert into run (benchmark, runSet) values ($1, $2) returning id")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertRunMetricNumber", "insert into runMetric (run, metric, result) values ($1, $2, $3)")
	if err != nil {
		return err
	}

	_, err = database.Prepare("insertRunMetricArray", "insert into runMetric (run, metric, resultArray) values ($1, $2, $3)")
	if err != nil {
		return err
	}

	return nil
}

package main

import (
	"encoding/json"
	"flag"
	"fmt"
	"io/ioutil"
	"net/http"
	"os"
	"strconv"
	"strings"
)

type runsetPostResponse struct {
	RunSetID int32
	RunIDs   []int32
}

type requestError struct {
	Explanation string
	httpStatus  int
}

func (rerr *requestError) Error() string {
	return rerr.Explanation
}

func (rerr *requestError) httpError(w http.ResponseWriter) {
	errorString := "{\"Explanation\": \"unknown\"}"
	bytes, err := json.Marshal(rerr)
	if err == nil {
		errorString = string(bytes)
	}
	http.Error(w, errorString, rerr.httpStatus)
}

func badRequestError(explanation string) *requestError {
	return &requestError{Explanation: explanation, httpStatus: http.StatusBadRequest}
}

func internalServerError(explanation string) *requestError {
	return &requestError{Explanation: explanation, httpStatus: http.StatusInternalServerError}
}

func ensureBenchmarksAndMetricsExist(rs *RunSet) *requestError {
	benchmarks, reqErr := fetchBenchmarks()
	if reqErr != nil {
		return reqErr
	}

	for _, run := range rs.Runs {
		if !benchmarks[run.Benchmark] {
			return badRequestError("Benchmark does not exist: " + run.Benchmark)
		}
		for m, v := range run.Results {
			if !metricIsAllowed(m, v) {
				return badRequestError("Metric not supported or results of wrong type: " + m)
			}
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

func runSetPostHandler(w http.ResponseWriter, r *http.Request, body []byte) (bool, *requestError) {
	var params RunSet
	if err := json.Unmarshal(body, &params); err != nil {
		fmt.Printf("Unmarshal error: %s\n", err.Error())
		return false, badRequestError("Could not parse request body")
	}

	reqErr := ensureMachineExists(params.Machine)
	if reqErr != nil {
		return false, reqErr
	}

	mainCommit, reqErr := ensureProductExists(params.MainProduct)
	if reqErr != nil {
		return false, reqErr
	}

	var secondaryCommits []string
	for _, p := range params.SecondaryProducts {
		commit, reqErr := ensureProductExists(p)
		if reqErr != nil {
			return false, reqErr
		}
		secondaryCommits = append(secondaryCommits, commit)
	}

	reqErr = ensureBenchmarksAndMetricsExist(&params)
	if reqErr != nil {
		return false, reqErr
	}

	reqErr = ensureConfigExists(params.Config)
	if reqErr != nil {
		return false, reqErr
	}

	var runSetID int32
	err := database.QueryRow("insertRunSet",
		params.StartedAt, params.FinishedAt,
		params.BuildURL, params.LogURLs,
		mainCommit, secondaryCommits, params.Machine.Name, params.Config.Name,
		params.TimedOutBenchmarks, params.CrashedBenchmarks).Scan(&runSetID)
	if err != nil {
		fmt.Printf("run set insert error: %s\n", err)
		return false, internalServerError("Could not insert run set")
	}

	runIDs, reqErr := insertRuns(runSetID, params.Runs)
	if reqErr != nil {
		return false, reqErr
	}

	resp := runsetPostResponse{RunSetID: runSetID, RunIDs: runIDs}
	respBytes, err := json.Marshal(&resp)
	if err != nil {
		return false, internalServerError("Could not produce JSON for response")
	}

	w.WriteHeader(http.StatusCreated)
	w.Header().Set("Content-Type", "application/json; charset=UTF-8")
	w.Write(respBytes)

	return true, nil
}

func parseIDFromPath(path string, numComponents int) (int32, *requestError) {
	pathComponents := strings.Split(path, "/")
	if len(pathComponents) != numComponents {
		return -1, badRequestError("Incorrect path")
	}
	id64, err := strconv.ParseInt(pathComponents[numComponents-1], 10, 32)
	if err != nil {
		return -1, badRequestError("Could not parse run set id")
	}
	return int32(id64), nil
}

func specificRunSetGetHandler(w http.ResponseWriter, r *http.Request, body []byte) (bool, *requestError) {
	runSetID, reqErr := parseIDFromPath(r.URL.Path, 4)
	if reqErr != nil {
		return false, reqErr
	}

	rs, reqErr := fetchRunSet(runSetID, true)
	if reqErr != nil {
		return false, reqErr
	}

	respBytes, err := json.Marshal(&rs)
	if err != nil {
		return false, internalServerError("Could not product JSON for response")
	}

	w.WriteHeader(http.StatusOK)
	w.Header().Set("Content-Type", "application/json; charset=UTF-8")
	w.Write(respBytes)

	return false, nil
}

func specificRunSetPostHandler(w http.ResponseWriter, r *http.Request, body []byte) (bool, *requestError) {
	runSetID, reqErr := parseIDFromPath(r.URL.Path, 4)
	if reqErr != nil {
		return false, reqErr
	}

	var params RunSet
	if err := json.Unmarshal(body, &params); err != nil {
		fmt.Printf("Unmarshal error: %s\n", err.Error())
		return false, badRequestError("Could not parse request body")
	}

	reqErr = ensureBenchmarksAndMetricsExist(&params)
	if reqErr != nil {
		return false, reqErr
	}

	rs, reqErr := fetchRunSet(runSetID, false)
	if reqErr != nil {
		return false, reqErr
	}

	if params.MainProduct != rs.MainProduct ||
		!productSetsEqual(params.SecondaryProducts, rs.SecondaryProducts) ||
		params.Machine != rs.Machine ||
		!params.Config.isSameAs(&rs.Config) {
		return false, badRequestError("Parameters do not match database")
	}

	rs.amendWithDataFrom(&params)

	runIDs, reqErr := insertRuns(runSetID, params.Runs)
	if reqErr != nil {
		return false, reqErr
	}

	reqErr = updateRunSet(runSetID, rs)
	if reqErr != nil {
		return false, reqErr
	}

	resp := runsetPostResponse{RunSetID: runSetID, RunIDs: runIDs}
	respBytes, err := json.Marshal(&resp)
	if err != nil {
		return false, internalServerError("Could not produce JSON for response")
	}

	w.WriteHeader(http.StatusCreated)
	w.Header().Set("Content-Type", "application/json; charset=UTF-8")
	w.Write([]byte(respBytes))

	return true, nil
}

type handlerFunc func(w http.ResponseWriter, r *http.Request, body []byte) (bool, *requestError)

func newTransactionHandler(authToken string, handlers map[string]handlerFunc) func(w http.ResponseWriter, r *http.Request) {
	return func(w http.ResponseWriter, r *http.Request) {
		var reqErr *requestError
		handler, ok := handlers[r.Method]
		if !ok {
			reqErr = &requestError{Explanation: "Method not allowed", httpStatus: http.StatusMethodNotAllowed}
		} else if r.URL.Query().Get("authToken") != authToken {
			reqErr = &requestError{Explanation: "Auth token invalid", httpStatus: http.StatusUnauthorized}
		} else {
			body, err := ioutil.ReadAll(r.Body)
			r.Body.Close()
			if err != nil {
				reqErr = internalServerError("Could not read request body")
			} else {
				transaction, err := database.Begin()
				if err != nil {
					reqErr = internalServerError("Could not begin transaction")
				} else {
					var commit bool
					commit, reqErr = handler(w, r, body)
					if commit {
						transaction.Commit()
					} else {
						transaction.Rollback()
					}
				}
			}
		}
		if reqErr != nil {
			reqErr.httpError(w)
		}
	}
}

func notFoundHandler(w http.ResponseWriter, r *http.Request) {
	reqErr := &requestError{Explanation: "No such endpoint", httpStatus: http.StatusNotFound}
	reqErr.httpError(w)
}

func main() {
	portFlag := flag.Int("port", 0, "port on which to listen")
	credentialsFlag := flag.String("credentials", "benchmarkerCredentials", "path of the credentials file")
	keyFlag := flag.String("ssl-key", "", "path of the SSL key file")
	certFlag := flag.String("ssl-certificate", "", "path of the SSL certificate file")
	flag.Parse()

	ssl := *certFlag != "" || *keyFlag != ""
	port := *portFlag
	if port == 0 {
		if ssl {
			port = 10443
		} else {
			port = 8081
		}
	}

	if err := readCredentials(*credentialsFlag); err != nil {
		fmt.Fprintf(os.Stderr, "Error: Cannot read credentials from file %s: %s\n", *credentialsFlag, err.Error())
		os.Exit(1)
	}

	initGitHub()

	if err := initDatabase(); err != nil {
		fmt.Fprintf(os.Stderr, "Error: Cannot init DB: %s\n", err.Error())
		os.Exit(1)
	}

	authToken, err := getCredentialString("httpAPITokens", "default")
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error: Cannot get auth token: %s\n", err.Error())
		os.Exit(1)
	}

	http.HandleFunc("/api/runset", newTransactionHandler(authToken, map[string]handlerFunc{"POST": runSetPostHandler}))
	http.HandleFunc("/api/runset/", newTransactionHandler(authToken, map[string]handlerFunc{"GET": specificRunSetGetHandler, "POST": specificRunSetPostHandler}))
	http.HandleFunc("/", notFoundHandler)

	addr := fmt.Sprintf(":%d", port)
	fmt.Printf("listening at %s\n", addr)

	if ssl {
		// Instructions for generating a certificate: http://www.zytrax.com/tech/survival/ssl.html#self
		err = http.ListenAndServeTLS(addr, *certFlag, *keyFlag, nil)
	} else {
		err = http.ListenAndServe(addr, nil)
	}

	if err != nil {
		fmt.Fprintf(os.Stderr, "Error: Listen failed: %s\n", err.Error())
		os.Exit(1)
	}
}

package main

import (
	"time"
)

type Config struct {
	Name                     string
	MonoExecutable           string
	MonoEnvironmentVariables map[string]string
	MonoOptions              []string
}

type Results map[string]interface{}

type Run struct {
	Benchmark string
	Results   Results
}

type Machine struct {
	Name         string
	Architecture string
}

type Product struct {
	Name          string
	Commit        string
	MergeBaseHash *string
}

type PullRequest struct {
	BaselineRunSetID int32
	URL              string
}

type RunSet struct {
	MainProduct        Product
	SecondaryProducts  []Product
	Machine            Machine
	Config             Config
	PullRequest        *PullRequest
	StartedAt          time.Time
	FinishedAt         time.Time
	BuildURL           string
	LogURLs            map[string]string
	TimedOutBenchmarks []string
	CrashedBenchmarks  []string
	Runs               []Run
}

type ProductSummary struct {
	Commit string
}

type RunSetSummary struct {
	ID          int32
	MainProduct ProductSummary
}

func stringSlicesSetEqual(a []string, b []string) bool {
	if len(a) != len(b) {
		return false
	}
	for _, v := range a {
		found := false
		for _, o := range b {
			if o == v {
				found = true
				break
			}
		}
		if !found {
			return false
		}
	}
	return true
}

func productSetsEqual(a []Product, b []Product) bool {
	var as []string
	for _, p := range a {
		as = append(as, p.Commit)
	}
	var bs []string
	for _, p := range b {
		bs = append(bs, p.Commit)
	}
	return stringSlicesSetEqual(as, bs)
}

func (config *Config) isSameAs(other *Config) bool {
	if config.Name != other.Name {
		return false
	}
	if config.MonoExecutable != other.MonoExecutable {
		return false
	}
	if len(config.MonoEnvironmentVariables) != len(other.MonoEnvironmentVariables) {
		return false
	}
	for k, v := range config.MonoEnvironmentVariables {
		o, ok := other.MonoEnvironmentVariables[k]
		if !ok || v != o {
			return false
		}
	}
	if !stringSlicesSetEqual(config.MonoOptions, other.MonoOptions) {
		return false
	}
	return true
}

func metricIsArray(metric string) bool {
	return metric == "cachegrind" || metric == "pause-times" || metric == "pause-starts"
}

func metricIsAllowed(metric string, value interface{}) bool {
	if metricIsArray(metric) {
		arr, ok := value.([]interface{})
		if !ok {
			return false
		}
		for _, v := range arr {
			_, ok = v.(float64)
			if !ok {
				return false
			}
		}
		return true
	}

	if metric == "time" ||
		metric == "instructions" ||
		metric == "memory-integral" ||
		metric == "cache-miss" {
		_, ok := value.(float64)
		return ok
	}

	return false
}

func githubRepoForProduct(name string) (string, string) {
	if name == "mono" {
		return "mono", "mono"
	}
	if name == "benchmarker" {
		return "xamarin", "benchmarker"
	}
	return "", ""
}

func amendStringSlice(l []string, add []string) []string {
	for _, b := range add {
		found := false
		for _, o := range l {
			if b == o {
				found = true
				break
			}
		}
		if found {
			continue
		}
		l = append(l, b)
	}
	return l
}

func (rs *RunSet) amendWithDataFrom(other *RunSet) {
	// FIXME: don't allow overwriting existing ones?
	for k, v := range other.LogURLs {
		rs.LogURLs[k] = v
	}
	rs.TimedOutBenchmarks = amendStringSlice(rs.TimedOutBenchmarks, other.TimedOutBenchmarks)
	rs.CrashedBenchmarks = amendStringSlice(rs.CrashedBenchmarks, other.CrashedBenchmarks)
	// FIXME: technically we don't need this
	for _, r := range other.Runs {
		rs.Runs = append(rs.Runs, r)
	}
}

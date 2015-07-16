# Mono Benchmarker

## Building tools

    cd tools
    nuget restore tools.sln
    xbuild

## Configs

Each Mono configuration requires a `.conf` file.  The files in the `configs` directory are examples. The JSON structure is as follows:

  - `Name`: name of the config (must be unique across all configs and benchmarks)
  - `Count`: number of time to run the benchmark (optional, default: 5)
  - `Mono`: path to the mono executable (optional, default to system one)
  - `MonoOptions`: command line parameters to pass to the mono runtime (optional)
  - `MonoEnvironmentVariables`: environment variables to set to run the benchmark (optional)
  - `ResultsDirectory`: path to the results directory, relative to the benchmarker repository root directory (optional, default to `results/`)

## Benchmarks

Each benchmark requires a `.benchmark` file. The files in the `benchmarks` directory are examples. The JSON structure is as follows:

  - `Name`: name of the benchmark (must be unique across all configs and benchmarks)
  - `TestDirectory`: path to the working directory to run the benchmark, relative to the benchmarker repository root directory
  - `CommandLine`: command line to run the benchnark, does not contain the runtime (mono) executable
  - `Timeout`: benchmark specific timeout, override the command line one

## Building the front-end

### Requirements

    brew install npm
    npm -g install webpack

### Building

    make -C front-end

## Comparing directly

To compare two or more revisions and/or configurations directly, use `tools/compare.exe`:

    ./compare.exe [options] [--] <tests-dir> <benchmarks-dir> <machines-dir> <config-file>

Where:

  - `tests-dir`: path to tests directory
  - `benchmarks-dir`: path to benchmarks directory
  - `machines-dir`: path to machines directory
  - `config-file`: path to a configuration file to run, there is one or more.

Stores the graph to `graph.svg` in the current directory by default.

## JSON results format

The new JSON results format is as follows:

  - `DateTime`: date and time at which this benchmark was run
  - `Benchmark`: copy data of the `benchmarks/*.benchmark` corresponding file
    - `Name`: name of the benchmark
    - `TestDirectory`: working directory to use to run the benchmark, relative to tests/
    - `CommandLine`: command line parameters to pass to the benchmark
    - `Timeout`: timeout specific to this benchmark, in seconds
  - `Config`: copy data of the `configs/*.conf` corresponding file
    - `Name`: name of the config
    - `Count`: number of time to run the benchmark
    - `Mono`: path to the mono executable
    - `MonoOptions`: command line parameters to pass to the mono runtime
    - `MonoEnvironmentVariables`: environment variables to set to run the benchmark
    - `UnsavedMonoEnvironmentVariables`: like `MonoEnvironmentVariables`, but not saved to the database
    - `ResultsDirectory`: path to the results directory, relative to the benchmarker repository root directory
  - `Version`: standard output when run with `--version` runtime command line parameter
  - `Timedout`: true if any of the run of the benchmark has timed out
  - `Runs`: collections of the runs for the benchnark, size is equal to Config.Count
    - `WallClockTime`: wall clock time taken to run the benchmark
    - `Output`: standard output of the benchmark
    - `Error`: standard error of the benchmark

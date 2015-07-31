# Mono Benchmarker

[![Build Status](https://travis-ci.org/xamarin/benchmarker.svg?branch=master)](https://travis-ci.org/xamarin/benchmarker)

## Running benchmarks locally

### Requirements

    brew install lynx jq

### Building

    cd tools
    nuget restore tools.sln
    xbuild

### Running

There are two ways to run the benchmarking suite locally, one
higher-level than the other.  If you want to benchmark finished Mono
packages from a build bot, you should use `run.sh`.  If you want to
benchmark a Mono that you've built on your own machine, use the
lower-level `compare.exe`.

#### From a package

`run.sh` requires at least three pieces of information, two of which
serve as documentation and for reproducibility:

- The file or URL of the Mono package you want to benchmark
- The URL of the build page for that package
- The `mono` Git repository commit of that package

Example, on OS X:

    ./run.sh --commit 2d4f6b344205b410f21a3470d4152d78a28964d1 \
	         --build-url 'https://wrench.internalx.com/Wrench/ViewLane.aspx?lane_id=1972&host_id=148&revision_id=580021' \
			 --pkg-url 'http://storage.bos.internalx.com/mono-mac-4.2.0-pre2-branch/2d/2d4f6b344205b410f21a3470d4152d78a28964d1/MonoFramework-MRE-4.2.0.22.macos10.xamarin.x86.pkg'

It will ask for your password when it tries to install the package.
The package is installed in a temporary disk image, so your system
will not be affected.

On Linux, `run.sh` requires that you specify `.deb` package URLs.
`--help` will tell you how.

#### From a local build

To run the suite on a locally built Mono, use `tools/compare.exe`:

    mono tools/compare.exe --root <MONO-INSTALL-ROOT>

Your Mono executable must be in `MONO-INSTALL-ROOT/bin/mono-sgen`.
The benchmark runner will try to figure out the Git commit of your
Mono.  If that fails, use the `--commit` and `--git-repo` options to
help it.

Once all benchmarks have been run and the results uploaded it will
print a URL that shows all the results.

#### Setting options

To give options to Mono or set environment variables you need to use a
configuration files.  Those files are kept in the `config` directory.
The default configuration is `default-sgen.conf`.  Make a copy of it
and rename it, to avoid confusion and for documentation.

## Configs

Each Mono configuration requires a `.conf` file.  The files in the `configs` directory are examples. The JSON structure is as follows:

  - `Name`: name of the config (must be unique across all configs and benchmarks)
  - `Count`: number of time to run the benchmark (optional, default: 10)
  - `Mono`: path to the mono executable (optional, default to system one)
  - `MonoOptions`: command line parameters to pass to the mono runtime (optional)
  - `MonoEnvironmentVariables`: environment variables to set to run the benchmark (optional)
  - `UnsavedMonoEnvironmentVariables`: environment variables to set to run the benchmarks, but not saved in the database (optional)

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

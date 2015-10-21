# The Benchmarker Front-end

## Motivation

The goal of the benchmarker front-end is to provide a convenient way
to view and compare the results of running benchmarks. We use the
following terminology to talk about benchmark results:

* A **run** is a single execution of a single benchmark.

* A **machine** is the identity of a machine on which benchmarks can
  be run.

* A **config** is the set of command-line arguments and environment
  variables with which benchmarks can be run.

* A **run set** is a set of results from running all benchmarks at a
  particular commit, on a particular machine, with a particular
  config.

Notably, benchmark results are not generally comparable between
machines or configs, unless trying to identify platform- or
architecture-specific regressions.

Common tasks include:

* Automatically recording benchmark results for pushes to interesting
  repositories (e.g., Mono) in order to locate regressions (and
  improvements!).

* Comparing run sets from different commits in order to identify the
  causes of regressions.

## Architecture

The front-end is built using [TypeScript] and [React].  Charts are
rendered using [amCharts].  The code is bundled with [webpack] for
distribution.  We use [tslint] for additional checks.

Each page has a basic skeleton HTML document `page.html`, a
page-specific stylesheet `src/page.css`, and a controller/renderer
script `src/page.tsx`.

A page’s module has two classes: a `Controller` that derives from the
`Controller` class in `common.tsx`, and a `Page` that derives from
`React.Component`. The `Controller` is the entry point of the page,
responsible for loading data at startup and invoking React to render
the `Page`, while the `Page` is the top-level layout and state of the
view.

[TypeScript]: http://www.typescriptlang.org/
[React]: http://facebook.github.io/react/
[amCharts]: http://www.amcharts.com/
[webpack]: http://webpack.github.io/
[tslint]: https://github.com/palantir/tslint

## Development

Run

    make server

and point your browser to the URL it reports.

To run the `tslint` task in Visual Studio Code, in `front-end`, do

    npm install -g gulp
    npm install gulp gulp-tslint

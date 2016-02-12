///<reference path="../typings/react/react.d.ts"/>
///<reference path="../typings/react-dom/react-dom.d.ts"/>

/* @flow */

"use strict";

import * as xp_common from './common.tsx';
import * as xp_utils from './utils.ts';
import * as xp_charts from './charts.tsx';
import * as Database from './database.ts';
import React = require ('react');
import ReactDOM = require ('react-dom');

interface SelectionNames {
	machineName: string;
	configName: string;
	metric: string;
}

class Controller {
	private initialRunSetIds: Array<number>;
	private initialSelectionNames: Array<SelectionNames>;
	private initialZoom: boolean;
	private runSetCounts: Array<Database.RunSetCount>;
	private featuredTimelines: Array<Database.DBObject>;

	constructor (machineName: string, configName: string, metric: string, selection: Array<number>) {
		this.initialRunSetIds = selection;
		if (machineName === undefined && configName === undefined && metric === undefined) {
			this.initialSelectionNames = [
				{ machineName: 'benchmarker', configName: 'auto-sgen-noturbo', metric: 'time' },
				{ machineName: 'benchmarker', configName: 'auto-sgen-noturbo-binary', metric: 'time' },
			];
			this.initialZoom = true;
		} else {
			this.initialSelectionNames = [ { machineName: machineName, configName: configName, metric: metric } ];
			this.initialZoom = false;
		}
	}

	public loadAsync () : void {
		Database.fetchRunSetCounts ((runSetCounts: Array<Database.RunSetCount>) => {
			this.runSetCounts = runSetCounts;
			this.checkAllDataLoaded ();
		}, (error: Object) => {
			alert ("error loading run set counts: " + error.toString ());
		});

		Database.fetchFeaturedTimelines ((featuredTimelines: Array<Database.DBObject>) => {
			this.featuredTimelines = featuredTimelines;
			this.checkAllDataLoaded ();
		}, (error: Object) => {
			alert ("error loading featured run sets: " + error.toString ());
		});
	}

	private checkAllDataLoaded () : void {
		if (this.runSetCounts === undefined)
			return;
		if (this.featuredTimelines === undefined)
			return;
		this.allDataLoaded ();
	}

	private allDataLoaded () : void {
		let selection: Array<xp_common.MachineConfigSelection> = [];
		this.initialSelectionNames.forEach ((isn: SelectionNames) => {
			let s = Database.findRunSetCount (this.runSetCounts, isn.machineName, isn.configName, isn.metric);
			selection.push (s);
		});

		ReactDOM.render (<Page
					initialRunSetIds={this.initialRunSetIds}
					initialSelection={selection}
					initialZoom={this.initialZoom}
					runSetCounts={this.runSetCounts}
					featuredTimelines={this.featuredTimelines}
					onChange={this.updateForSelection.bind (this)} />,
			document.getElementById ('timelinePage')
		);

		this.updateForSelection (selection, this.initialRunSetIds);
	}

	private updateForSelection (
		selection: Array<xp_common.MachineConfigSelection>,
		runSetSelection: Array<number>
	) : void {
		if (selection.length < 1) {
			return;
		}
		// FIXME: put all of them in the location
		var machine = selection [0].machine;
		var config = selection [0].config;
		var metric = selection [0].metric;

		xp_common.setLocationForDict ({
			machine: machine.get ('name'),
			config: config.get ('name'),
			metric: metric,
			selection: runSetSelection.length === 0 ? undefined : runSetSelection.join ('+'),
		});
	}
}

interface PageProps {
	initialSelection: Array<xp_common.MachineConfigSelection>;
	initialRunSetIds: Array<number>;
	initialZoom: boolean;
	onChange: (selection: Array<xp_common.MachineConfigSelection>, runSetIds: Array<number>) => void;
	runSetCounts: Array<Database.RunSetCount>;
	featuredTimelines: Array<Database.DBObject>;
}

interface PageState {
	selection: Array<xp_common.MachineConfigSelection>;
	zoom: boolean;
	runSetIndices: Array<number>;
	sortedResults: Array<Database.Summary>;
	benchmarkNames: Array<string>;
}

class Page extends React.Component<PageProps, PageState> {
	constructor (props: PageProps) {
		super (props);
		this.state = {
			selection: this.props.initialSelection,
			zoom: this.props.initialZoom,
			runSetIndices: [],
			sortedResults: [],
			benchmarkNames: [],
		};
	}

	public componentWillMount () : void {
		this.fetchSummaries (this.state.selection);
	}

	private selectedRunSetIds () : Array<number> {
		if (this.state.runSetIndices !== undefined)
			return this.state.runSetIndices.map
				((index: number) => this.state.sortedResults [index].runSet.get ('id'));
		return [];
	}

	private runSetSelected (metric: string, runSet: Database.DBObject) : void {
		var index = xp_utils.findIndex (this.state.sortedResults, (r: Database.Summary) => r.runSet === runSet);
		if (this.state.runSetIndices.indexOf (index) < 0)
			this.setState ({ runSetIndices: this.state.runSetIndices.concat ([index]), zoom: false } as any);
		var machine = runSet.get ('machine');
		var config = runSet.get ('config');
		var metric = metric;
		var selection = this.selectedRunSetIds ();
		xp_common.setLocationForDict ({
			machine: machine,
			config: config,
			metric: metric,
			selection: selection === undefined ? undefined : selection.join ('+'),
		});
	}

	private allBenchmarksLoaded (names: Array<string>) : void {
		this.setState ({benchmarkNames: names} as any);
	}

	private fetchSummaries (selection: Array<xp_common.MachineConfigSelection>) : void {
		let results: Array<Database.Summary> = [];
		let numResults = 0;
		selection.forEach ((s: xp_common.MachineConfigSelection, i: number) => {
			Database.fetchSummaries (s.machine, s.config, s.metric,
				(objs: Array<Database.Summary>) => {
					if (this.state.selection !== selection) {
						return;
					}

					results = results.concat (objs);
					++numResults;
					if (numResults < selection.length) {
						return;
					}

					results.sort ((a: Database.Summary, b: Database.Summary) => {
						var aDate = a.runSet.commit.get ('commitDate');
						var bDate = b.runSet.commit.get ('commitDate');
						if (aDate.getTime () !== bDate.getTime ())
							return aDate - bDate;
						return a.runSet.get ('startedAt') - b.runSet.get ('startedAt');
					});

					var indices = this.state.runSetIndices;
					if (this.props.initialRunSetIds !== undefined) {
						indices = this.props.initialRunSetIds.map (
							(id: number) => xp_utils.findIndex (
								results, (r: Database.Summary) => r.runSet.get ('id') === id));
					}

					this.setState ({ sortedResults: results, runSetIndices: indices } as any);
				}, (error: Object) => {
					alert ("error loading summaries: " + error.toString ());
				});
		});
	}

	private selectionChanged (selection: Array<xp_common.MachineConfigSelection>) : void {
		this.setState ({selection: selection, runSetIndices: [], sortedResults: [], benchmarkNames: [], zoom: false});
		this.fetchSummaries (selection);
		this.props.onChange (selection, []);
	}

	public render () : JSX.Element {
		var chart;
		var benchmarkChartList;
		let firstSelection: xp_common.MachineConfigSelection = { machine: undefined, config: undefined, metric: undefined };

		if (this.state.selection.length !== 0) {
			firstSelection = this.state.selection [0];

			var zoomInterval;
			if (this.state.zoom)
				zoomInterval = { start: 6, end: this.state.sortedResults.length };
			chart = <AllBenchmarksChart
				graphName={'allBenchmarksChart'}
				metric={firstSelection.metric}
				sortedResults={this.state.sortedResults}
				zoomInterval={zoomInterval}
				runSetSelected={(rs: Database.DBObject) => this.runSetSelected (firstSelection.metric, rs)}
				allBenchmarksLoaded={(names: Array<string>) => this.allBenchmarksLoaded (names)}
				selectedIndices={this.state.runSetIndices}
				/>;
			benchmarkChartList = <BenchmarkChartList
				benchmarkNames={this.state.benchmarkNames}
				metric={firstSelection.metric}
				sortedResults={this.state.sortedResults}
				runSetSelected={(rs: Database.DBObject) => this.runSetSelected (firstSelection.metric, rs)}
				selectedIndices={this.state.runSetIndices}
				/>;
		} else {
			chart = <div className="DiagnosticBlock">Please select a machine and config.</div>;
		}

		var runSetIndices = this.state.runSetIndices;
		var runSets = runSetIndices.map ((i: number) => this.state.sortedResults [i].runSet);

		var comparisonChart;
		if (runSets.length > 1) {
			comparisonChart = <xp_charts.ComparisonAMChart
				runSetLabels={undefined}
				graphName="comparisonChart"
				runSets={runSets}
				metric={firstSelection.metric}
				selectedIndices={runSetIndices}/>;
		}

		let runSetSummaries: JSX.Element;
		if (runSetIndices.length > 0) {
			var divs = runSetIndices.map ((i: number) => {
				var rs = this.state.sortedResults [i].runSet;
				var prev = i > 0 ? this.state.sortedResults [i - 1].runSet : undefined;
				var elem = <xp_common.RunSetSummary key={"runSet" + i.toString ()} runSet={rs} previousRunSet={prev} />;
				return elem;
			});
			runSetSummaries = <div className="RunSetSummaries">{divs}</div>;
		}

		var pageExplanation = <div className="TextBlock">
			<p>Select a machine and config to view a timeline of all run sets
			from that machine&ndash;config pair. The data point indicates the
			normalized average metric&mdash;for example, wall clock
			time&mdash;for all benchmarks in that run set. The shaded area
			indicates the range (minimum &amp; maximum) of the metric in that
			run set. Red data points indicate run sets with one or more crashed
			benchmarks.</p>
			<p>Clicking a data point will add it to a comparison chart showing
			relative results for all benchmarks.</p>
		</div>;

		var selectionExplanation = <div className="TextBlock">
			<p>Here is a comparison of the metrics for the selected run sets,
			if any. Switch to the &ldquo;Compare&rdquo; tab to view this
			comparison in more detail.</p>
		</div>;

		// FIXME: we need the descriptions for all machines and configs!

		return <div className="TimelinePage">
			<xp_common.Navigation
				currentPage="timeline"
				comparisonRunSetIds={runSets.map ((rs: Database.DBRunSet) => rs.get ('id'))} />
			<article>
				<h1>Timeline</h1>
				{pageExplanation}
				<h2>Overview</h2>
				<div className="outer">
					<div className="inner">
						<xp_common.CombinedConfigSelector
							includeMetric={true}
							runSetCounts={this.props.runSetCounts}
							featuredTimelines={this.props.featuredTimelines}
							selection={this.state.selection}
							onChange={this.selectionChanged.bind (this)}
							showControls={false} />
						<xp_common.MachineDescription
							machine={firstSelection.machine}
							omitHeader={true} />
						<xp_common.ConfigDescription
							config={firstSelection.config}
							omitHeader={true} />
					</div>
				</div>
				{chart}
				<div style={{ clear: 'both' }}></div>
				<h2>Selected Run Sets</h2>
				{selectionExplanation}
				{runSetSummaries}
				<div style={{ clear: 'both' }}></div>
				{comparisonChart}
				<h2>Per-benchmark Timelines</h2>
				{benchmarkChartList}
			</article>
		</div>;
	}
}

function runSetIsBroken (runSet: Database.DBObject, averages: Database.BenchmarkValues) : boolean {
	var timedOutBenchmarks = runSet.get ('timedOutBenchmarks') || [];
	var crashedBenchmarks = runSet.get ('crashedBenchmarks') || [];
	var timedOutOrCrashedBenchmarks = timedOutBenchmarks.concat (crashedBenchmarks);
	for (var i = 0; i < timedOutOrCrashedBenchmarks.length; ++i) {
		var benchmark = timedOutOrCrashedBenchmarks [i];
		if (!(benchmark in averages))
			return true;
	}
	return false;
}

interface AxisLabels {
	name: string;
	lowest: string;
	highest: string;
}

function axisNameForMetric (metric: string, relative: boolean) : AxisLabels {
	switch (metric) {
		case 'time':
			return {
				name: relative ? "Relative wall clock time" : "Wall clock time",
				lowest: "Fastest",
				highest: "Slowest",
			};
		case 'memory-integral':
			return {
				name: relative ? "Relative memory usage" : "MB * Giga Instructions",
				lowest: "Least memory",
				highest: "Most memory",
			};
		case 'instructions':
			return {
				name: relative ? "Relative # of instructions" : "Number of instructions",
				lowest: "Fewest instructions",
				highest: "Most instructions",
			};
		case 'cache-miss':
			return {
				name: relative ? "Relative cache miss rate" : "Cache miss rate",
				lowest: "Fewest cache misses",
				highest: "Most cache misses",
			};
		case 'branch-mispred':
			return {
				name: relative ? "Relative branch misprediction rate" : "Branch misprediction rate",
				lowest: "Fewest branch mispredictions",
				highest: "Most branch mispredictions",
			};
		default:
			return {
				name: "Unknown metric",
				lowest: "Lowest value",
				highest: "Highest value",
			};
	}
}

interface AllBenchmarksChartProps extends xp_charts.TimelineChartProps {
	metric: string;
	sortedResults: Array<Database.Summary>;
	selectedIndices: Array<number>;
	allBenchmarksLoaded (benchmarkNamesByIndices: Array<string>) : void;
};

class AllBenchmarksChart extends xp_charts.TimelineChart<AllBenchmarksChartProps> {
	public valueAxisTitle () : string {
		return axisNameForMetric (this.props.metric, true).name;
	}

	public computeTable (nextProps: AllBenchmarksChartProps) : Array<Object> {
		var results = nextProps.sortedResults;
		var i = 0, j = 0;

		/* A table of run data. The rows are indexed by benchmark index, the
		 * columns by sorted run set index.
		 */
		var runMetricsTable: Array<Array<number>> = [];

		/* Get a row index from a benchmark ID. */
		var benchmarkIndicesByName = {};
		var benchmarkNamesByIndices = [];
		/* Compute the mean elapsed time for each. */
		for (i = 0; i < results.length; ++i) {
			var row = results [i];
			var averages = row.averages;
			for (var name of Object.keys (averages)) {
				var index = benchmarkIndicesByName [name];
				if (index === undefined) {
					index = Object.keys (benchmarkIndicesByName).length;
					runMetricsTable.push ([]);
					benchmarkIndicesByName [name] = index;
					benchmarkNamesByIndices [index] = name;
				}

				var avg = averages [name];
				if (avg === undefined)
					continue;
				runMetricsTable [index] [i] = avg;
			}
		}

		/* Compute the average time for a benchmark, and normalize times by
		 * it, i.e., in a given run set, a given benchmark took some
		 * proportion of the average time for that benchmark.
		 */
		for (i = 0; i < runMetricsTable.length; ++i) {
			var filtered = runMetricsTable [i].filter ((x: number) => !isNaN (x));
			var normal = filtered.reduce ((sumSoFar: number, time: number) => sumSoFar + time, 0) / filtered.length;
			runMetricsTable [i] = runMetricsTable [i].map ((time: number) => time / normal);
		}

		var table = [];

		for (j = 0; j < results.length; ++j) {
			var runSet = results [j].runSet;
			var prodForRunSet = 1.0;
			var count = 0;
			var min = undefined;
			var minName = undefined;
			var max = undefined;
			var maxName = undefined;
			for (i = 0; i < runMetricsTable.length; ++i) {
				var val = runMetricsTable [i] [j];
				if (isNaN (val))
					continue;
				prodForRunSet *= val;
				if (min === undefined || val < min) {
					min = val;
					minName = benchmarkNamesByIndices [i];
				}
				if (max === undefined || val > max) {
					max = val;
					maxName = benchmarkNamesByIndices [i];
				}
				++count;
			}
			if (count === 0)
				continue;

			var tooltip = xp_charts.tooltipForRunSet (runSet, true);
			var broken = runSetIsBroken (runSet, results [j].averages);
			var runSetIndex = xp_utils.findIndex (results, (r: Database.Summary) => r.runSet === runSet);
			var selected = nextProps.selectedIndices.indexOf (runSetIndex) >= 0;
			const { lowest: lowestLabel, highest: highestLabel } = axisNameForMetric (this.props.metric, true);
			table.push ({
				dataItem: runSet,
				low: min,
				lowName: minName ? (lowestLabel + ": " + minName) : undefined,
				high: max,
				highName: maxName ? (highestLabel + ": " + maxName) : undefined,
				geomean: Math.pow (prodForRunSet, 1.0 / count),
				tooltip: tooltip,
				lineColor: selected
					? broken ? xp_common.xamarinColors.red [4] : xp_common.xamarinColors.blue [4]
					: broken ? xp_common.xamarinColors.red [2] : xp_common.xamarinColors.blue [2],
				bulletSize: selected ? 12 : 4,
			});
		}

		if (nextProps.allBenchmarksLoaded !== undefined)
			nextProps.allBenchmarksLoaded (benchmarkNamesByIndices);

		return table;
	}
}

function formatDuration (t: number) : string {
	return (t / 1000).toPrecision (4) + "s";
}

interface BenchmarkChartProps extends xp_charts.TimelineChartProps {
	metric: string;
	sortedResults: Array<Database.Summary>;
	benchmark: string;
	selectedIndices: Array<number>;
};

class BenchmarkChart extends xp_charts.TimelineChart<BenchmarkChartProps> {
	public valueAxisTitle () : string {
		return axisNameForMetric (this.props.metric, false).name;
	}

	public computeTable (nextProps: BenchmarkChartProps) : Array<Object> {
		var results = nextProps.sortedResults;
		var j = 0;

		var table = [];

		for (j = 0; j < results.length; ++j) {
			var runSet = results [j].runSet;
			var average = results [j].averages [nextProps.benchmark];
			var variance = results [j].variances [nextProps.benchmark];
			if (average === undefined)
				continue;

			var tooltip = xp_charts.tooltipForRunSet (runSet, false);

			var low = undefined;
			var high = undefined;
			var averageTooltip;
			if (variance !== undefined) {
				var stdDev = Math.sqrt (variance);
				low = average - stdDev;
				high = average + stdDev;
				// FIXME: it's not always duration, depending on the metric
				averageTooltip = "Average +/- standard deviation: " + formatDuration (low) + "–" + formatDuration (high);
			} else {
				averageTooltip = "Average: " + formatDuration (average);
			}
			var broken = runSetIsBroken (runSet, results [j].averages);
			var runSetIndex = xp_utils.findIndex (results, (r: Database.Summary) => r.runSet === runSet);
			var selected = nextProps.selectedIndices.indexOf (runSetIndex) >= 0;
			table.push ({
				dataItem: runSet,
				geomean: average,
				low: low,
				high: high,
				tooltip: tooltip + "\n" + averageTooltip,
				lineColor: selected
					? broken ? xp_common.xamarinColors.red [4] : xp_common.xamarinColors.blue [4]
					: broken ? xp_common.xamarinColors.red [2] : xp_common.xamarinColors.blue [2],
				bulletSize: selected ? 12 : 4,
			});
		}

		return table;
	}
}

type BenchmarkChartListProps = {
	metric: string;
	benchmarkNames: Array<string>;
	sortedResults: Array<Database.Summary>;
	runSetSelected: (runSet: Database.DBObject) => void;
	selectedIndices: Array<number>;
};

type BenchmarkChartListState = {
	expanded: { [name: string]: boolean };
};

class BenchmarkChartList extends React.Component<BenchmarkChartListProps, BenchmarkChartListState> {
	constructor (props: BenchmarkChartListProps) {
		super (props);
		this.state = { expanded: {} };
	}

	public render () : JSX.Element {
		var benchmarks = this.props.benchmarkNames.slice ();
		benchmarks.sort ();
		function chartKey (name: string) : string {
			return 'benchmarkChart_' + name;
		}
		function chartOrExpander (name: string) : JSX.Element {
			if (this.state.expanded [name]) {
				return <BenchmarkChart
					zoomInterval={undefined}
					graphName={chartKey (name)}
					sortedResults={this.props.sortedResults}
					metric={this.props.metric}
					benchmark={name}
					runSetSelected={this.props.runSetSelected}
					selectedIndices={this.props.selectedIndices}
					/>;
			} else {
				return <div>
					<button onClick={(e: React.MouseEvent) => this.expand (name)}>View Timeline</button>
				</div>;
			}
		}
		var chartRows = benchmarks.map ((name: string) => {
			return <tr key={chartKey (name)} className="BenchmarkChartList">
				<td><code>{name}</code></td>
				<td>{chartOrExpander.call (this, name)}</td>
			</tr>;
		});

		return <div className="BenchmarkChartList">
			<table>
				<tbody>
					<tr><th>Benchmark</th><th className="grow">Timeline</th></tr>
					<tr><td></td><td>
						<button onClick={(e: React.MouseEvent) => this.expandAll ()}>View All Timelines</button>
					</td></tr>
					{chartRows}
				</tbody>
			</table>
		</div>;
	}

	private expand (name: string) : void {
		var expanded: { [name: string]: boolean } = this.state.expanded;
		expanded [name] = true;
		this.setState ({ expanded: expanded });
	}

	private expandAll () : void {
		var benchmarks = this.props.benchmarkNames.slice ();
		var expanded: { [name: string]: boolean } = this.state.expanded;
		for (var i = 0; i < benchmarks.length; ++i)
			expanded [benchmarks [i]] = true;
		this.setState ({ expanded: expanded });
	}
}

function start (params: Object) : void {
	var machine = params ['machine'];
	var config = params ['config'];
	var metric = params ['metric'];
	var selection = [];
	if (params ['selection'] !== undefined)
		selection = xp_common.splitLocationHashValues (params ['selection']).map (
			(id: string) => parseInt (id));
	var controller = new Controller (machine, config, metric, selection);
	controller.loadAsync ();
}

xp_common.parseLocationHashForDict (['machine', 'config', 'metric', 'selection'], start);

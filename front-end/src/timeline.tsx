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

class Controller {
	private initialSelectionNames: { machineName: string, configName: string, metric: string };
	private initialZoom: boolean;
	private runSetCounts: Array<Database.RunSetCount>;
	private featuredTimelines: Array<Database.DBObject>;

	constructor (machineName: string, configName: string, metric: string) {
		if (machineName === undefined && configName === undefined && metric === undefined) {
			machineName = 'benchmarker';
			configName = 'auto-sgen-noturbo';
			metric = 'time';
			this.initialZoom = true;
		} else {
			this.initialZoom = false;
		}
		this.initialSelectionNames = { machineName: machineName, configName: configName, metric: metric };
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
		var selection: xp_common.MachineConfigSelection;
		if (this.initialSelectionNames.machineName !== undefined &&
				this.initialSelectionNames.configName !== undefined &&
				this.initialSelectionNames.metric !== undefined &&
				this.runSetCounts !== undefined) {
			selection = Database.findRunSetCount (this.runSetCounts,
				this.initialSelectionNames.machineName,
				this.initialSelectionNames.configName,
				this.initialSelectionNames.metric);
		}
		if (selection === undefined)
			selection = { machine: undefined, config: undefined, metric: undefined };

		ReactDOM.render (<Page
					initialSelection={selection}
					initialZoom={this.initialZoom}
					runSetCounts={this.runSetCounts}
					featuredTimelines={this.featuredTimelines}
					onChange={(s: xp_common.MachineConfigSelection) => this.updateForSelection (s)} />,
			document.getElementById ('timelinePage')
		);

		this.updateForSelection (selection);
	}

	private updateForSelection (selection: xp_common.MachineConfigSelection) : void {
		var machine = selection.machine;
		var config = selection.config;
		var metric = selection.metric;
		if (machine === undefined || config === undefined || metric === undefined)
			return;
		xp_common.setLocationForDict ({ machine: machine.get ('name'), config: config.get ('name'), metric: metric });
	}
}

type PageProps = {
	initialSelection: xp_common.MachineConfigSelection;
	initialZoom: boolean;
	onChange: (selection: xp_common.MachineConfigSelection) => void;
	runSetCounts: Array<Database.RunSetCount>;
	featuredTimelines: Array<Database.DBObject>;
};

type PageState = {
	machine: Database.DBObject;
	config: Database.DBObject;
	metric: string;
	zoom: boolean;
	runSetIndexes: Array<number>,
	sortedResults: Array<Database.Summary>,
	benchmarkNames: Array<string>
};

class Page extends React.Component<PageProps, PageState> {
	constructor (props: PageProps) {
		super (props);
		this.state = {
			machine: this.props.initialSelection.machine,
			config: this.props.initialSelection.config,
			metric: this.props.initialSelection.metric,
			zoom: this.props.initialZoom,
			runSetIndexes: [],
			sortedResults: [],
			benchmarkNames: []
		};
	}

	public componentWillMount () : void {
		this.fetchSummaries (this.state);
	}

	private runSetSelected (runSet: Database.DBObject) : void {
		var index = xp_utils.findIndex (this.state.sortedResults, (r: Database.Summary) => r.runSet === runSet);
		if (this.state.runSetIndexes.indexOf (index) < 0)
			this.setState ({runSetIndexes: this.state.runSetIndexes.concat ([index]), zoom: false} as any);
	}

	private allBenchmarksLoaded (names: Array<string>) : void {
		this.setState ({benchmarkNames: names} as any);
	}

	private fetchSummaries (selection: xp_common.MachineConfigSelection) : void {
		var machine = selection.machine;
		var config = selection.config;
		var metric = selection.metric;

		if (machine === undefined || config === undefined || metric === undefined)
			return;

		Database.fetchSummaries (machine, config, metric,
			(objs: Array<Database.Summary>) => {
				objs.sort ((a: Database.Summary, b: Database.Summary) => {
					var aDate = a.runSet.commit.get ('commitDate');
					var bDate = b.runSet.commit.get ('commitDate');
					if (aDate.getTime () !== bDate.getTime ())
						return aDate - bDate;
					return a.runSet.get ('startedAt') - b.runSet.get ('startedAt');
				});

				this.setState ({sortedResults: objs} as any);
			}, (error: Object) => {
				alert ("error loading summaries: " + error.toString ());
			});
	}

	private selectionChanged (selection: xp_common.MachineConfigSelection) : void {
		var machine = selection.machine;
		var config = selection.config;
		var metric = selection.metric;

		this.setState ({machine: machine, config: config, metric: metric, runSetIndexes: [], sortedResults: [], benchmarkNames: [], zoom: false});
		this.fetchSummaries (selection);
		this.props.onChange (selection);
	}

	public render () : JSX.Element {
		var chart;
		var benchmarkChartList;
		var selected = this.state.machine !== undefined && this.state.config !== undefined && this.state.metric !== undefined;

		if (selected) {
			var zoomInterval;
			if (this.state.zoom)
				zoomInterval = { start: 6, end: this.state.sortedResults.length };
			chart = <AllBenchmarksChart
				graphName={'allBenchmarksChart'}
				machine={this.state.machine}
				config={this.state.config}
				metric={this.state.metric}
				sortedResults={this.state.sortedResults}
				zoomInterval={zoomInterval}
				runSetSelected={(rs: Database.DBObject) => this.runSetSelected (rs)}
				allBenchmarksLoaded={(names: Array<string>) => this.allBenchmarksLoaded (names)}
				/>;
			benchmarkChartList = <BenchmarkChartList
				benchmarkNames={this.state.benchmarkNames}
				machine={this.state.machine}
				config={this.state.config}
				metric={this.state.metric}
				sortedResults={this.state.sortedResults}
				runSetSelected={(rs: Database.DBObject) => this.runSetSelected (rs)}
				/>;
		} else {
			chart = <div className="DiagnosticBlock">Please select a machine and config.</div>;
		}

		var runSetIndexes = this.state.runSetIndexes;
		var runSets = runSetIndexes.map ((i: number) => this.state.sortedResults [i].runSet);

		var comparisonChart;
		if (runSets.length > 1) {
			comparisonChart = <xp_charts.ComparisonAMChart
				runSetLabels={undefined}
				graphName="comparisonChart"
				runSets={runSets}
				metric={this.state.metric} />;
		}

		var runSetSummaries;
		if (runSetIndexes.length > 0) {
			var divs = runSetIndexes.map ((i: number) => {
				var rs = this.state.sortedResults [i].runSet;
				var prev = i > 0 ? this.state.sortedResults [i - 1].runSet : undefined;
				var elem = <RunSetSummary key={"runSet" + i.toString ()} runSet={rs} previousRunSet={prev} />;
				return elem;
			});
			runSetSummaries = <div className="RunSetSummaries">{divs}</div>;
		}

		return <div className="TimelinePage">
			<xp_common.Navigation
				currentPage="timeline"
				comparisonRunSetIds={runSets.map ((rs: Database.DBRunSet) => rs.get ('id'))} />
			<article>
				<div className="outer">
					<div className="inner">
						<xp_common.CombinedConfigSelector
							includeMetric={true}
							runSetCounts={this.props.runSetCounts}
							featuredTimelines={this.props.featuredTimelines}
							machine={this.state.machine}
							config={this.state.config}
							metric={this.state.metric}
							onChange={(s: xp_common.MachineConfigSelection) => this.selectionChanged (s)}
							showControls={false} />
						<xp_common.MachineDescription
							machine={this.state.machine}
							omitHeader={true} />
						<xp_common.ConfigDescription
							config={this.state.config}
							omitHeader={true} />
					</div>
				</div>
				{chart}
				<div style={{ clear: 'both' }}></div>
				{runSetSummaries}
				<div style={{ clear: 'both' }}></div>
				{comparisonChart}
				{benchmarkChartList}
			</article>
		</div>;
	}
}

interface RunSetSummaryProps extends React.Props<RunSetSummary> {
	runSet: Database.DBRunSet;
	previousRunSet: Database.DBRunSet;
}

class RunSetSummary extends React.Component<RunSetSummaryProps, void> {
	public render () : JSX.Element {
		var runSet = this.props.runSet;
		var commitHash = runSet.commit.get ('hash');
		var commitLink = xp_common.githubCommitLink (runSet.commit.get ('product'), commitHash);

		var prev = this.props.previousRunSet;
		var prevItems;
		if (prev !== undefined) {
			var prevHash = prev.commit.get ('hash');
			var prevLink = xp_common.githubCommitLink (prev.commit.get ('product'), prevHash);
			var compareLink = xp_common.githubCompareLink (prevHash, commitHash);
			prevItems = [<dt key="previousName">Previous</dt>,
				<dd key="previousValue"><a href={prevLink}>{prevHash.substring (0, 10)}</a><br /><a href={compareLink}>Compare</a></dd>];
		}

		var runSetLink = "runset.html#id=" + runSet.get ('id');
		return <div className="RunSetSummary">
			<div className="Description">
			<dl>
			<dt>Commit</dt>
			<dd><a href={commitLink}>{commitHash.substring (0, 10)}</a><br /><a href={runSetLink}>Details</a></dd>
			{prevItems}
			</dl>
			</div>
			</div>;
	}
}

function joinBenchmarkNames (benchmarks: Array<string>, prefix: string) : string {
	if (benchmarks === undefined || benchmarks.length === 0)
		return "";
	return prefix + benchmarks.join (", ");
}

function tooltipForRunSet (runSet: Database.DBRunSet, includeBroken: boolean) : string {
	var commit = runSet.commit;
	var commitDateString = commit.get ('commitDate').toDateString ();
	var branch = "";
	if (commit.get ('branch') !== undefined)
		branch = " (" + commit.get ('branch') + ")";
	var startedAtString = runSet.get ('startedAt').toDateString ();
	var hashString = commit.get ('hash').substring (0, 10);

	var tooltip = hashString + branch + "\nCommitted on " + commitDateString + "\nRan on " + startedAtString;
	if (includeBroken) {
		var timedOutBenchmarks = joinBenchmarkNames (runSet.get ('timedOutBenchmarks'), "\nTimed out: ");
		var crashedBenchmarks = joinBenchmarkNames (runSet.get ('crashedBenchmarks'), "\nCrashed: ");
		tooltip = tooltip + timedOutBenchmarks + crashedBenchmarks;
	}
	return tooltip;
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

interface TimelineChartProps {
	graphName: string;
	machine: Database.DBObject;
	config: Database.DBObject;
	metric: string;
	sortedResults: Array<Database.Summary>;
	zoomInterval: {start: number, end: number};
	runSetSelected: (runSet: Database.DBObject) => void;
};

abstract class TimelineChart<Props extends TimelineChartProps> extends React.Component<Props, void> {
	// FIXME: make private and have `computeTable` return the new table
	public table: Array<Object>;

	public valueAxisTitle () : string {
		return "";
	}

	public componentWillMount () : void {
		this.invalidateState (this.props);
	}

	public componentWillReceiveProps (nextProps: Props) : void {
		if (this.props.machine === nextProps.machine &&
				this.props.config === nextProps.config &&
				this.props.metric === nextProps.metric &&
				this.props.sortedResults === nextProps.sortedResults) {
			return;
		}
		this.invalidateState (nextProps);
	}

	public render () : JSX.Element {
		if (this.table === undefined)
			return <div className="diagnostic">Loading&hellip;</div>;

		return <xp_charts.TimelineAMChart
			graphName={this.props.graphName}
			height={300}
			data={this.table}
			zoomInterval={this.props.zoomInterval}
			title={this.valueAxisTitle ()}
			selectListener={(rs: Database.DBRunSet) => this.props.runSetSelected (rs)} />;
	}

	public abstract computeTable (nextProps: Props) : void;

	private invalidateState (nextProps: Props) : void {
		this.table = undefined;
		this.computeTable (nextProps);
	}
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
				highest: "Slowest"
			};
		case 'memory-integral':
			return {
				name: relative ? "Relative memory usage" : "MB * Giga Instructions",
				lowest: "Least memory",
				highest: "Most memory"
			};
		case 'instructions':
			return {
				name: relative ? "Relative # of instructions" : "Number of instructions",
				lowest: "Fewest instructions",
				highest: "Most instructions"
			};
		default:
			return undefined;
	}
}

interface AllBenchmarksChartProps extends TimelineChartProps {
	allBenchmarksLoaded (benchmarkNamesByIndices: Array<string>) : void;
};

class AllBenchmarksChart extends TimelineChart<AllBenchmarksChartProps> {
	public valueAxisTitle () : string {
		return axisNameForMetric (this.props.metric, true).name;
	}

	public computeTable (nextProps: AllBenchmarksChartProps) : void {
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
			if (count === 0) {
				console.log ("No data for run set " + runSet.get ('id'));
				continue;
			}
			var tooltip = tooltipForRunSet (runSet, true);
			var broken = runSetIsBroken (runSet, results [j].averages);
			const { lowest: lowestLabel, highest: highestLabel } = axisNameForMetric (this.props.metric, true);
			table.push ({
				dataItem: runSet,
				low: min,
				lowName: minName ? (lowestLabel + ": " + minName) : undefined,
				high: max,
				highName: maxName ? (highestLabel + ": " + maxName) : undefined,
				geomean: Math.pow (prodForRunSet, 1.0 / count),
				tooltip: tooltip,
				lineColor: (broken ? xp_common.xamarinColors.red [2] : xp_common.xamarinColors.blue [2])
			});
		}

		this.table = table;

		if (nextProps.allBenchmarksLoaded !== undefined)
			nextProps.allBenchmarksLoaded (benchmarkNamesByIndices);
	}
}

function formatDuration (t: number) : string {
	return (t / 1000).toPrecision (4) + "s";
}

interface BenchmarkChartProps extends TimelineChartProps {
	benchmark: string;
};

class BenchmarkChart extends TimelineChart<BenchmarkChartProps> {
	public valueAxisTitle () : string {
		return axisNameForMetric (this.props.metric, false).name;
	}

	public computeTable (nextProps: BenchmarkChartProps) : void {
		var results = nextProps.sortedResults;
		var j = 0;

		var table = [];

		for (j = 0; j < results.length; ++j) {
			var runSet = results [j].runSet;
			var average = results [j].averages [nextProps.benchmark];
			var variance = results [j].variances [nextProps.benchmark];
			if (average === undefined)
				continue;

			var tooltip = tooltipForRunSet (runSet, false);

			var low = undefined;
			var high = undefined;
			var averageTooltip;
			if (variance !== undefined) {
				var stdDev = Math.sqrt (variance);
				low = average - stdDev;
				high = average + stdDev;
				averageTooltip = "Average +/- standard deviation: " + formatDuration (low) + "–" + formatDuration (high);
			} else {
				averageTooltip = "Average: " + formatDuration (average);
			}
			table.push ({
				dataItem: runSet,
				geomean: average,
				low: low,
				high: high,
				tooltip: tooltip + "\n" + averageTooltip
			});
		}

		this.table = table;
	}
}

type BenchmarkChartListProps = {
	machine: Database.DBObject;
	config: Database.DBObject;
	metric: string;
	benchmarkNames: Array<string>;
	sortedResults: Array<Database.Summary>;
	runSetSelected: (runSet: Database.DBObject) => void;
};

type BenchmarkChartListState = {
	isExpanded: boolean;
};

class BenchmarkChartList extends React.Component<BenchmarkChartListProps, BenchmarkChartListState> {
	constructor (props: BenchmarkChartListProps) {
		super (props);
		this.state = { isExpanded: false };
	}

	public render () : JSX.Element {
		if (!this.state.isExpanded) {
			return <div className="BenchmarkChartList">
				<button onClick={(e: React.MouseEvent) => this.expand ()}>Show Benchmarks</button>
			</div>;
		}

		var benchmarks = this.props.benchmarkNames.slice ();
		benchmarks.sort ();
		var charts = benchmarks.map ((name: string) => {
			var key = 'benchmarkChart_' + name;
			return <div key={key} className="BenchmarkChartList">
				<h3>{name}</h3>
				<BenchmarkChart
					zoomInterval={undefined}
					graphName={key}
					sortedResults={this.props.sortedResults}
					machine={this.props.machine}
					config={this.props.config}
					metric={this.props.metric}
					benchmark={name}
					runSetSelected={this.props.runSetSelected}
					/>
				</div>;
		});

		return <div>{charts}</div>;
	}

	private expand () : void {
		this.setState ({ isExpanded: true });
	}
}

function start (params: Object) : void {
	var machine = params ['machine'];
	var config = params ['config'];
	var metric = params ['metric'];
	var controller = new Controller (machine, config, metric);
	controller.loadAsync ();
}

xp_common.parseLocationHashForDict (['machine', 'config', 'metric'], start);

/* @flow */

"use strict";

import * as xp_common from './common.js';
import * as xp_utils from './utils.js';
import * as xp_charts from './charts.js';
import {Parse} from 'parse';
import React from 'react';

class Controller extends xp_common.Controller {

	initialMachineId: string | void;
	initialConfigId: string | void;

	constructor (machineId, configId) {
		super ();
		this.initialMachineId = machineId;
		this.initialConfigId = configId;
	}

	allDataLoaded () {
		var initialSelection = {};
		if (this.initialMachineId !== undefined)
			initialSelection.machine = this.machineForId (this.initialMachineId);
		if (this.initialConfigId !== undefined)
			initialSelection.config = this.configForId (this.initialConfigId);

		React.render (
			React.createElement (
				Page,
				{
					controller: this,
					initialSelection: initialSelection,
					onChange: this.updateForSelection.bind (this)
				}
			),
			document.getElementById ('timelinePage')
		);

		this.updateForSelection (initialSelection);
	}

	updateForSelection (selection) {
		var machine = selection.machine;
		var config = selection.config;
		if (machine === undefined || config === undefined)
			return;
		window.location.hash = machine.id + '+' + config.id;
	}

}

class Page extends React.Component {

	constructor (props) {
		super (props);
		this.state = {
			machine: this.props.initialSelection.machine,
			config: this.props.initialSelection.config,
			runSets: []
		};
	}

	setState (newState) {
		super.setState (newState);
		this.props.onChange (newState);
	}

	runSetSelected (runSet) {
		window.open ('runset.html#' + runSet.id);
		this.setState ({runSets: this.state.runSets.concat ([runSet])});
	}

	render () {
		var chart;
		var benchmarkChartList;
		var selected = this.state.machine !== undefined && this.state.config !== undefined;

		if (selected) {
			chart = <AllBenchmarksChart
				graphName={'allBenchmarksChart'}
				controller={this.props.controller}
				machine={this.state.machine}
				config={this.state.config}
				runSetSelected={this.runSetSelected.bind (this)}
				/>;
			benchmarkChartList = <BenchmarkChartList
				controller={this.props.controller}
				machine={this.state.machine}
				config={this.state.config}
				runSetSelected={this.runSetSelected.bind (this)}
				/>;
		} else {
			chart = <div className="DiagnosticBlock">Please select a machine and config.</div>;
		}

		var comparisonChart;
		if (this.state.runSets.length > 1)
			comparisonChart = <xp_charts.ComparisonAMChart graphName="comparisonChart" controller={this.props.controller} runSets={this.state.runSets} />;

		return <div className="TimelinePage">
			<xp_common.Navigation currentPage="timeline" />
			<article>
				<div className="panel">
					<xp_common.CombinedConfigSelector
						controller={this.props.controller}
						machine={this.state.machine}
						config={this.state.config}
						onChange={this.setState.bind (this)}
						showControls={false} />
					<xp_common.MachineDescription
						machine={this.state.machine}
						omitHeader={true} />
					<xp_common.ConfigDescription
						config={this.state.config}
						omitHeader={true} />
				</div>
				{chart}
				<div style={{ clear: 'both' }}></div>
				{comparisonChart}
				{benchmarkChartList}
			</article>
		</div>;
	}
}

function tooltipForRunSet (controller: Controller, runSet: Parse.Object) {
	var commit = runSet.get ('commit');
	var commitDateString = commit.get ('commitDate').toDateString ();
	var branch = "";
	if (commit.get ('branch') !== undefined)
		branch = " (" + commit.get ('branch') + ")";
	var startedAtString = runSet.get ('startedAt').toDateString ();
	var hashString = commit.get ('hash').substring (0, 10);

	var timedOutBenchmarks = xp_common.joinBenchmarkNames (controller, runSet.get ('timedOutBenchmarks'), "\nTimed out: ");
	var crashedBenchmarks = xp_common.joinBenchmarkNames (controller, runSet.get ('crashedBenchmarks'), "\nCrashed: ");

	return hashString + branch + "\nCommitted on " + commitDateString + "\nRan on " + startedAtString + timedOutBenchmarks + crashedBenchmarks;
}

function runSetIsBroken (controller: Controller, runSet: Parse.Object) {
	var timedOutBenchmarks = runSet.get ('timedOutBenchmarks') || [];
	var crashedBenchmarks = runSet.get ('crashedBenchmarks') || [];
	var timedOutOrCrashedBenchmarks = timedOutBenchmarks.concat (crashedBenchmarks);
	for (var i = 0; i < timedOutOrCrashedBenchmarks.length; ++i) {
		var benchmark = timedOutOrCrashedBenchmarks [i];
		var name = controller.benchmarkNameForId (benchmark.id);
		if (!(name in runSet.get ('elapsedTimeAverages')))
			return true;
	}
	return false;
}

type TimelineChartProps = {
	graphName: string;
	controller: Controller;
	machine: Parse.Object;
	config: Parse.Object;
	benchmark: string;
	runSetSelected: (runSet: Parse.Object) => void;
};

class TimelineChart extends React.Component<TimelineChartProps, TimelineChartProps, void> {

	sortedRunSets : Array<Parse.Object>;
	table : void | Array<Object>;

	constructor (props) {
		super (props);
	}

	componentWillMount () {
		this.invalidateState (this.props.machine, this.props.config);
	}

	componentWillReceiveProps (nextProps) {
		if (this.props.machine === nextProps.machine && this.props.config === nextProps.config)
			return;
		this.invalidateState (nextProps.machine, nextProps.config);
	}

	render () {
		if (this.table === undefined)
			return <div className="diagnostic">Loading&hellip;</div>;

		return <xp_charts.TimelineAMChart
			graphName={this.props.graphName}
			height={300}
			data={this.table}
			selectListener={this.props.runSetSelected.bind (this)} />;
	}

	computeTable () {
	}

	invalidateState (machine, config) {
		this.table = undefined;

		var runSets = this.props.controller.runSetsForMachineAndConfig (machine, config);

		runSets.sort ((a, b) => {
			var aDate = a.get ('commit').get ('commitDate');
			var bDate = b.get ('commit').get ('commitDate');
			if (aDate.getTime () !== bDate.getTime ())
				return aDate - bDate;
			return a.get ('startedAt') - b.get ('startedAt');
		});

		this.sortedRunSets = runSets;

		this.computeTable ();

		this.forceUpdate ();
	}
}

class AllBenchmarksChart extends TimelineChart {
	computeTable () {
		var runSets = this.sortedRunSets;
		var i = 0, j = 0;
		var allBenchmarks = this.props.controller.allEnabledBenchmarks ();

		/* A table of run data. The rows are indexed by benchmark index, the
		 * columns by sorted run set index.
		 */
		var runMetricsTable : Array<Array<number>> = [];

		/* Get a row index from a benchmark ID. */
		var benchmarkIndicesById = {};
		for (i = 0; i < allBenchmarks.length; ++i) {
			runMetricsTable.push ([]);
			benchmarkIndicesById [allBenchmarks [i].id] = i;
		}

		/* Get a column index from a run set ID. */
		var runSetIndicesById = {};
		for (i = 0; i < runSets.length; ++i)
			runSetIndicesById [runSets [i].id] = i;

		/* Compute the mean elapsed time for each. */
		for (i = 0; i < allBenchmarks.length; ++i) {
			for (j = 0; j < runSets.length; ++j) {
				var benchmark = allBenchmarks [i];
				var averages = runSets [j].get ('elapsedTimeAverages');
				var avg = averages [benchmark.get ('name')];
				if (avg === undefined)
					continue;
				runMetricsTable [i] [j] = avg;
			}
		}

		/* Compute the average time for a benchmark, and normalize times by
		 * it, i.e., in a given run set, a given benchmark took some
		 * proportion of the average time for that benchmark.
		 */
		for (i = 0; i < allBenchmarks.length; ++i) {
			var filtered = runMetricsTable [i].filter (x => !isNaN (x));
			var normal = filtered.reduce ((sumSoFar, time) => sumSoFar + time, 0) / filtered.length;
			runMetricsTable [i] = runMetricsTable [i].map (time => time / normal);
		}

		var table = [];

		for (j = 0; j < runSets.length; ++j) {
			var prodForRunSet = 1.0;
			var count = 0;
			var min = undefined;
			var minName = undefined;
			var max = undefined;
			var maxName = undefined;
			for (i = 0; i < allBenchmarks.length; ++i) {
				var val = runMetricsTable [i] [j];
				if (isNaN (val))
					continue;
				prodForRunSet *= val;
				if (min === undefined || val < min) {
					min = val;
					minName = allBenchmarks [i].get ('name');
				}
				if (max === undefined || val > max) {
					max = val;
					maxName = allBenchmarks [i].get ('name');
				}
				++count;
			}
			if (count === 0) {
				console.log ("No data for run set " + runSets [j].id);
				continue;
			}
			var tooltip = tooltipForRunSet (this.props.controller, runSets [j]);
			var broken = runSetIsBroken (this.props.controller, runSets [j]);
			table.push ({
				runSet: runSets [j],
				low: min,
				lowName: minName,
				high: max,
				highName: maxName,
				geomean: Math.pow (prodForRunSet, 1.0 / count),
				tooltip: tooltip,
				lineColor: (broken ? xp_common.xamarinColors.red [2] : xp_common.xamarinColors.blue [2])
			});
		}

		this.table = table;
	}
}

class BenchmarkChart extends TimelineChart {
	computeTable () {
		var runSets = this.sortedRunSets;
		var j = 0;

		var table = [];

		for (j = 0; j < runSets.length; ++j) {
			var runSet = runSets [j];
			var average = runSet.get ('elapsedTimeAverages') [this.props.benchmark];
			var variance = runSet.get ('elapsedTimeVariances') [this.props.benchmark];
			if (average === undefined)
				continue;
			var low = undefined;
			var high = undefined;
			if (variance !== undefined) {
				var stdDev = Math.sqrt (variance);
				low = average - stdDev;
				high = average + stdDev;
			}
			var tooltip = tooltipForRunSet (this.props.controller, runSet);
			table.push ({
				runSet: runSet,
				geomean: average,
				low: low,
				high: high,
				tooltip: tooltip
			});
		}

		this.table = table;
	}
}

class BenchmarkChartList extends React.Component {
	constructor (props) {
		super (props);
		this.state = { isExpanded: false };
	}

	render () {
		if (!this.state.isExpanded) {
			return <div className="BenchmarkChartList">
				<button onClick={this.expand.bind (this)}>Show Benchmarks</button>
			</div>;
		}

		var benchmarks = this.props.controller.allEnabledBenchmarks ();
		benchmarks = xp_utils.sortArrayLexicographicallyBy (benchmarks, b => b.get ('name'));
		var charts = benchmarks.map (b => {
			var name = b.get ('name');
			var key = 'benchmarkChart_' + name;
			return <div key={key} className="BenchmarkChartList">
				<h3>{name}</h3>
				<BenchmarkChart
				graphName={key}
				controller={this.props.controller}
				machine={this.props.machine}
				config={this.props.config}
				benchmark={name}
				runSetSelected={this.props.runSetSelected}
				/>
				</div>;
		});

		return <div>{charts}</div>;
	}

	expand () {
		this.setState ({ isExpanded: true });
	}
}

function started () {
	var machineId = undefined;
	var configId = undefined;
	if (window.location.hash) {
		var ids = window.location.hash.substring (1).split ('+');
		if (ids.length === 2) {
			machineId = ids [0];
			configId = ids [1];
		}
	}
	var controller = new Controller (machineId, configId);
	controller.loadAsync ();
}

xp_common.start (started);

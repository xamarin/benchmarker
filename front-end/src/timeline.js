/* @flow */

/* global google */

"use strict";

import * as xp_common from './common.js';
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
		console.log (runSet);
		window.open ('runset.html#' + runSet.id);
		this.setState ({runSets: this.state.runSets.concat ([runSet])});
	}

	render () {

		var chart;

		if (this.state.machine === undefined || this.state.config === undefined)
			chart = <div className="diagnostic">Please select a machine and config.</div>;
		else
			chart = <Chart
		controller={this.props.controller}
		machine={this.state.machine}
		config={this.state.config}
		runSetSelected={this.runSetSelected.bind (this)}
			/>;

		var comparisonChart;
		if (this.state.runSets.length > 1)
			comparisonChart = <xp_common.ComparisonChart controller={this.props.controller} runSets={this.state.runSets} />;

		return <div>
			<xp_common.ConfigSelector
				controller={this.props.controller}
				machine={this.state.machine}
				config={this.state.config}
				onChange={this.setState.bind (this)} />
			{chart}
		{comparisonChart}
		</div>;

	}

}

function tooltipForRunSet (controller: Controller, runSet: ParseObject) {
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

class Chart extends xp_common.GoogleChartsStateComponent {

	constructor (props) {
		super (props);
		this.invalidateState (props.machine, props.config);
	}

	invalidateState (machine, config) {

		this.table = undefined;

		var runSetQuery = new Parse.Query (xp_common.RunSet);
		runSetQuery
			.equalTo ('machine', machine)
			.equalTo ('config', config);
		xp_common.pageParseQuery (
			() => {
				var runQuery = new Parse.Query (xp_common.Run);
				runQuery.matchesQuery ('runSet', runSetQuery);
				return runQuery;
			},
			results => {
				if (machine !== this.props.machine || config !== this.props.config)
					return;

				this.allRuns = results;
				this.runsLoaded ();
			},
			function (error) {
				alert ("error loading runs: " + error);
			});
	}

	componentWillReceiveProps (nextProps) {
		if (this.props.machine === nextProps.machine && this.props.config === nextProps.config)
			return;
		this.invalidateState (nextProps.machine, nextProps.config);
	}

	render () {

		if (this.table === undefined)
			return <div className="diagnostic">Loading&hellip;</div>;

		var options = {
			vAxis: {
				gridlines: {
					color: 'transparent'
				},
				baseline: 1.0
			},
			hAxis: {
				gridlines: {
					color: 'transparent'
				},
				textPosition: 'none'
			},
			intervals: {
				style: 'area',
			}
		};

		return <xp_common.GoogleChart
		graphName='timelineChart'
		chartClass={google.visualization.LineChart}
		height={300}
		table={this.table}
		options={options}
		selectListener={this.selectListener.bind (this)} />;

	}

	selectListener (chart) {
		var item = chart.getSelection () [0];
		if (item === undefined)
			return;
		console.log ("selected");
		console.log (item);
		var runSet = this.sortedRunSets [item.row];
		this.props.runSetSelected (runSet);
	}

	googleChartsLoaded () {
		this.runsLoaded ();
	}

	runsLoaded () {
		var i = 0, j = 0;
		var machine = this.props.machine;
		var config = this.props.config;
		var allRuns = this.allRuns;

		if (this.allRuns === undefined)
			return;

		if (!xp_common.canUseGoogleCharts ())
			return;

		var allBenchmarks = this.props.controller.allBenchmarks;
		var runSets = this.props.controller.runSetsForMachineAndConfig (machine, config);
		runSets.sort ((a, b) => {
			var aDate = a.get ('commit').get ('commitDate');
			var bDate = b.get ('commit').get ('commitDate');
			if (aDate.getTime () !== bDate.getTime ())
				return aDate - bDate;
			return a.get ('startedAt') - b.get ('startedAt');
		});

		this.sortedRunSets = runSets;

		/* A table of run data. The rows are indexed by benchmark index, the
		 * columns by sorted run set index.
		 */
		var runTable : Array<Array<Array<ParseObject>>> = [];
		var runMetricsTable : Array<Array<number>> = [];

		/* Get a row index from a benchmark ID. */
		var benchmarkIndicesById = {};
		for (i = 0; i < allBenchmarks.length; ++i) {
			runTable.push ([]);
			runMetricsTable.push ([]);
			benchmarkIndicesById [allBenchmarks [i].id] = i;
		}

		/* Get a column index from a run set ID. */
		var runSetIndicesById = {};
		for (i = 0; i < runSets.length; ++i) {
			for (j = 0; j < allBenchmarks.length; ++j)
				runTable [j].push ([]);
			runSetIndicesById [runSets [i].id] = i;
		}

		/* Partition allRuns by benchmark and run set. */
		for (i = 0; i < allRuns.length; ++i) {
			var run = allRuns [i];
			var runIndex = runSetIndicesById [run.get ('runSet').id];
			var benchmarkIndex = benchmarkIndicesById [run.get ('benchmark').id];
			runTable [benchmarkIndex] [runIndex].push (run);
		}

		/* Compute the mean elapsed time for each. */
		for (i = 0; i < allBenchmarks.length; ++i) {
			for (j = 0; j < runSets.length; ++j) {
				var runs = runTable [i] [j];
				var sum = runs
					.map (r => r.get ('elapsedMilliseconds'))
					.reduce ((sumSoFar, time) => sumSoFar + time, 0);
				runMetricsTable [i] [j] = sum / runs.length;
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

		var table = new google.visualization.DataTable ();

		table.addColumn ({type: 'number', label: "Run Set Index"});
		table.addColumn ({type: 'number', label: "Elapsed Time"});
		table.addColumn ({type: 'number', role: 'interval'});
		table.addColumn ({type: 'number', role: 'interval'});
		table.addColumn ({type: 'string', role: 'tooltip'});

		for (j = 0; j < runSets.length; ++j) {
			var sumForRunSet = 0;
			var count = 0;
			var min = undefined;
			var max = undefined;
			for (i = 0; i < allBenchmarks.length; ++i) {
				var val = runMetricsTable [i] [j];
				if (isNaN (val))
					continue;
				sumForRunSet += val;
				if (min === undefined || val < min)
					min = val;
				if (max === undefined || val > max)
					max = val;
				++count;
			}
			var tooltip = tooltipForRunSet (this.props.controller, runSets [j]);
			table.addRow ([
				j,
				sumForRunSet / count,
				min,
				max,
				tooltip
			]);
		}

		this.table = table;
		this.forceUpdate ();
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
}

xp_common.start (started);

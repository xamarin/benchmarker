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

		return <div className="Timeline">
			<table>
				<tr>
					<td>
						<xp_common.CombinedConfigSelector
							controller={this.props.controller}
							machine={this.state.machine}
							config={this.state.config}
							onChange={this.setState.bind (this)} />
					</td>
					<td>
						<xp_common.MachineDescription
							machine={this.state.machine} />
					</td>
					<td>
						<xp_common.ConfigDescription
							config={this.state.config} />
					</td>
				</tr>
			</table>
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

	componentWillReceiveProps (nextProps) {
		if (this.props.machine === nextProps.machine && this.props.config === nextProps.config)
			return;
		this.invalidateState (nextProps.machine, nextProps.config);
	}

	render () {
		if (this.table === undefined)
			return <div className="diagnostic">Loading&hellip;</div>;

		return <xp_common.TimelineAMChart
			graphName='timelineChart'
			height={300}
			data={this.table}
			selectListener={this.selectListener.bind (this)} />;
	}

	selectListener (itemIndex) {
		console.log ("selected ", itemIndex);
		var runSet = this.sortedRunSets [itemIndex];
		this.props.runSetSelected (runSet);
	}

	googleChartsLoaded () {
		this.invalidateState (this.props.machine, this.props.config);
	}

	invalidateState (machine, config) {
		this.table = undefined;

		var i = 0, j = 0;
		var allBenchmarks = this.props.controller.allBenchmarks;
		var runSets = this.props.controller.runSetsForMachineAndConfig (machine, config);

		if (!xp_common.canUseGoogleCharts ())
			return;

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
			var sumForRunSet = 0;
			var count = 0;
			var min = undefined;
			var minName = undefined;
			var max = undefined;
			var maxName = undefined;
			for (i = 0; i < allBenchmarks.length; ++i) {
				var val = runMetricsTable [i] [j];
				if (isNaN (val))
					continue;
				sumForRunSet += val;
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
			var tooltip = tooltipForRunSet (this.props.controller, runSets [j]);
			table.push ({
				low: min,
				lowName: minName,
				high: max,
				highName: maxName,
				average: sumForRunSet / count,
				tooltip: tooltip
			});
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

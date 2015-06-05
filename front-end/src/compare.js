/* global google */

"use strict";

import * as xp_utils from './utils.js';
import * as xp_common from './common.js';
import {Parse} from 'parse';
import React from 'react';

class Controller extends xp_common.Controller {

	constructor (startupRunSetIds) {
		super ();
		this.startupRunSetIds = startupRunSetIds;
	}

	allDataLoaded () {
		var selections;

		if (this.startupRunSetIds === undefined) {
			selections = [{}];
		} else {
			selections = this.startupRunSetIds.map (id => {
				let runSet = this.runSetForId (id);
				let machine = this.machineForId (runSet.get ('machine').id);
				return {machine: machine, config: runSet.get ('config'), runSet: runSet};
			});
		}

		React.render (
			React.createElement (
				Page,
				{
					controller: this,
					initialSelections: selections,
					onChange: this.updateForSelection.bind (this)
				}
			),
			document.getElementById ('comparePage')
		);

		this.updateForSelection (selections);
	}

	updateForSelection (selections) {
		var runSets = selections.map (s => s.runSet).filter (rs => rs !== undefined);
		window.location.hash = xp_common.hashForRunSets (runSets);
	}
}

class Page extends React.Component {
	constructor (props) {
		super (props);
		this.state = {selections: this.props.initialSelections};
	}

	setState (newState) {
		super.setState (newState);
		this.props.onChange (newState.selections);
	}

	render () {
		console.log ("rendering compare page");

		var selections = this.state.selections;
		var runSets = selections.map (s => s.runSet).filter (rs => rs !== undefined);

		let chart;
		if (runSets.length > 1)
			chart = <Chart controller={this.props.controller} runSets={runSets} />;
		else
			chart = <div className='diagnostic'>Please select at least two run sets.</div>;

		return <div>
			<RunSetSelectorList
		controller={this.props.controller}
		selections={this.state.selections}
		onChange={this.setState.bind (this)} />
			{chart}
		</div>;
	}
}

class Chart extends xp_common.GoogleChartsStateComponent {

	constructor (props) {
		console.log ("run set compare chart constructing");

		super (props);

		this.invalidateState (props.runSets);
	}

	invalidateState (runSets) {
		this.state = {};

		this.runsByIndex = [];
		/* FIXME: use the containedIn constraint? */
		for (let i = 0; i < runSets.length; ++i) {
			var rs = runSets [i];
			var query = new Parse.Query (xp_common.Run);
			query.equalTo ('runSet', rs);
			query.find ({
				success: results => {
					if (this.props.runSets !== runSets)
						return;

					this.runsByIndex [i] = results;
					this.runsLoaded ();
				},
				error: function (error) {
					alert ("error loading runs: " + error);
				}
			});
		}
	}

	componentWillReceiveProps (nextProps) {
		this.invalidateState (nextProps.runSets);
	}

	googleChartsLoaded () {
		this.runsLoaded ();
	}

	runsLoaded () {
		console.log ("run loaded");

		if (!xp_common.canUseGoogleCharts ())
			return;

		for (let i = 0; i < this.props.runSets.length; ++i) {
			if (this.runsByIndex [i] === undefined)
				return;
		}

		console.log ("all runs loaded");

		var commonBenchmarkIds;

		for (let i = 0; i < this.props.runSets.length; ++i) {
			let runs = this.runsByIndex [i];
			var benchmarkIds = xp_utils.uniqArray (runs.map (o => o.get ('benchmark').id));
			if (commonBenchmarkIds === undefined) {
				commonBenchmarkIds = benchmarkIds;
				continue;
			}
			commonBenchmarkIds = xp_utils.intersectArray (benchmarkIds, commonBenchmarkIds);
		}

		var dataArray = [];

		for (let i = 0; i < commonBenchmarkIds.length; ++i) {
			var benchmarkId = commonBenchmarkIds [i];
			var row = [this.props.controller.benchmarkNameForId (benchmarkId)];
			let mean;
			for (var j = 0; j < this.props.runSets.length; ++j) {
				let runs = this.runsByIndex [j].filter (r => r.get ('benchmark').id === benchmarkId);
				var range = xp_common.calculateRunsRange (runs);
				if (mean === undefined) {
					// FIXME: eventually we'll have more meaningful ranges
					mean = range [1];
				}
				row = row.concat (xp_common.normalizeRange (mean, range));
			}
			dataArray.push (row);
		}

		var data = google.visualization.arrayToDataTable (dataArray, true);
		for (let i = 0; i < this.props.runSets.length; ++i)
			data.setColumnLabel (1 + 4 * i, this.props.runSets [i].get ('startedAt'));

		var height = (35 + (15 * this.props.runSets.length) * commonBenchmarkIds.length) + "px";

		this.setState ({table: data, height: height});
	}

	render () {
		if (this.state.table === undefined)
			return <div className='diagnostic'>Loading&hellip;</div>;

		var options = { orientation: 'vertical' };
		return <xp_common.GoogleChart
		graphName='compareChart'
		chartClass={google.visualization.CandlestickChart}
		height={this.state.height}
		table={this.state.table}
		options={options} />;
	}
}

class RunSetSelectorList extends React.Component {
	handleChange (index, newSelection) {
		var selections = xp_utils.updateArray (this.props.selections, index, newSelection);
		this.props.onChange ({selections: selections});
	}

	addSelector () {
		this.props.onChange ({selections: this.props.selections.concat ({})});
	}

	removeSelector (i) {
		this.props.onChange ({selections: xp_utils.removeArrayElement (this.props.selections, i)});
	}

	render () {
		function renderSelector (selection, index) {
			return <section>
				<button onClick={this.removeSelector.bind (this, index)}>Remove</button>
				<RunSetSelector
			controller={this.props.controller}
			selection={selection}
			onChange={this.handleChange.bind (this, index)} />
				</section>;
		}
		return <div className="RunSetSelectorList">
			{this.props.selections.map (renderSelector.bind (this))}
			<footer><button onClick={this.addSelector.bind (this)}>Add Run Set</button></footer>
			</div>;
	}
}

class RunSetSelector extends React.Component {

	runSetSelected (event) {
		let selection = this.props.selection;
		let runSetId = event.target.value;
		console.log ("run set selected: " + runSetId);
		let runSet = this.props.controller.runSetForId (runSetId);
		this.props.onChange ({machine: selection.machine, config: selection.config, runSet: runSet});
	}

	render () {
		let selection = this.props.selection;
		console.log (selection);

		let machineId, runSetId, filteredRunSets;

		if (selection.machine !== undefined)
			machineId = selection.machine.id;

		if (selection.runSet !== undefined)
			runSetId = selection.runSet.id;

		if (selection.machine !== undefined && selection.config !== undefined)
			filteredRunSets = this.props.controller.runSetsForMachineAndConfig (selection.machine, selection.config);
		else
			filteredRunSets = [];

		console.log (filteredRunSets);

		function renderRunSetOption (rs) {
			return <option value={rs.id} key={rs.id}>{rs.get ('startedAt').toString ()}</option>;
		}

		let config = selection.config === undefined
			? undefined
			: this.props.controller.configForId (selection.config.id);

		let configSelector =
			<xp_common.ConfigSelector
		controller={this.props.controller}
		machine={selection.machine}
		config={config}
		onChange={this.props.onChange} />;
		let runSetsSelect = filteredRunSets.length === 0
			? <select size="6" disabled="true">
			<option className="diagnostic">Please select a machine and config.</option>
			</select>
			: <select
		size="6"
		selectedIndex="-1"
		value={runSetId}
		onChange={this.runSetSelected.bind (this)}>
			{filteredRunSets.map (renderRunSetOption)}
		</select>;

		console.log ("runSetId is " + runSetId);

		return <div className="RunSetSelector">
			{configSelector}
		{runSetsSelect}
			<xp_common.MachineDescription machine={selection.machine} />
			<xp_common.ConfigDescription config={config} />
			</div>;
	}

	getRunSet () {
		return this.state.runSet;
	}
}

function started () {
	var startupRunSetIds;
	if (window.location.hash)
		startupRunSetIds = window.location.hash.substring (1).split ('+');
	var controller = new Controller (startupRunSetIds);
}

xp_common.start (started);

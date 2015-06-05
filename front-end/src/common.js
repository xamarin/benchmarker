/* global google */

"use strict";

import * as xp_utils from './utils.js';
import {Parse} from 'parse';
import React from 'react';

export var Benchmark;
export var Config;
export var Machine;
export var Run;
export var RunSet;

export function start (started) {
	google.load ('visualization', '1.0', {
		packages: ['corechart'],
		callback: googleChartsDidLoad
	});

	Parse.initialize('7khPUBga9c7L1YryD1se1bp6VRzKKJESc0baS9ES', 'qnBBT97Mttqsvq3g9zghnBVn2iiHLAQvTzekUigm');

	Benchmark = Parse.Object.extend ('Benchmark');
	Config = Parse.Object.extend ('Config');
	Machine = Parse.Object.extend ('Machine');
	Run = Parse.Object.extend ('Run');
	RunSet = Parse.Object.extend ('RunSet');

	started ();
}

export class Controller {

	allBenchmarks: Array<Object>;
	allMachines: Array<Object>;
	allRunSets: Array<Object>;
	allConfigs: Array<Object>;

	constructor () {

		var machineQuery = new Parse.Query (Machine);
		machineQuery.find ({
			success: this.machinesLoaded.bind (this),
			error: function (error) {
				alert ("error loading machines: " + error);
			}
		});

		var runSetQuery = new Parse.Query (RunSet)
			.include ('commit');
		runSetQuery.find ({
			success: this.runSetsLoaded.bind (this),
			error: function (error) {
				alert ("error loading run sets: " + error);
			}
		});

		var configQuery = new Parse.Query (Config);
		configQuery.find ({
			success: this.configsLoaded.bind (this),
			error: function (error) {
				alert ("error loading configs: " + error);
			}
		});

		var benchmarkQuery = new Parse.Query (Benchmark);
		benchmarkQuery.find ({
			success: results => {
				this.allBenchmarks = results;
				this.checkAllDataLoaded ();
			},
			error: function (error) {
				alert ("error loading benchmarks: " + error);
			}
		});
	}

	allDataLoaded () {
	}

	machinesLoaded (results) {
		console.log ("machines loaded: " + results.length);
		this.allMachines = results;
		this.checkAllDataLoaded ();
	}

	configsLoaded (results) {
		this.allConfigs = results;
		this.checkAllDataLoaded ();
	}

	runSetsLoaded (results) {
		console.log ("run sets loaded: " + results.length);
		this.allRunSets = results;
		this.checkAllDataLoaded ();
	}

	benchmarkNameForId (id) {
		for (var i = 0; i < this.allBenchmarks.length; ++i) {
			if (this.allBenchmarks [i].id === id)
				return this.allBenchmarks [i].get ('name');
		}
	}

	machineForId (id) {
		return xp_utils.find (this.allMachines, m => m.id === id);
	}

	configForId (id) {
		return xp_utils.find (this.allConfigs, m => m.id === id);
	}

	runSetForId (id) {
		return xp_utils.find (this.allRunSets, rs => rs.id === id);
	}

	runSetsForMachineAndConfig (machine, config) {
		return this.allRunSets.filter (
			rs =>
				rs.get ('machine').id === machine.id &&
				rs.get ('config').id === config.id
		);
	}

	checkAllDataLoaded () {
		if (this.allMachines === undefined
			|| this.allRunSets === undefined
			|| this.allBenchmarks === undefined
			|| this.allConfigs === undefined)
			return;

		this.allDataLoaded ();
	}

}

var googleChartsLoaded = false;
var googleChartsStateComponents = [];

function googleChartsDidLoad () {
	googleChartsLoaded = true;
	for (var i = 0; i < googleChartsStateComponents.length; ++i) {
		var component = googleChartsStateComponents [i];
		if (component === undefined)
			continue;
		component.googleChartsLoaded ();
	}
	googleChartsStateComponents = undefined;
}

export function canUseGoogleCharts () {
	return googleChartsLoaded;
}

export class GoogleChart extends React.Component {
	render () {
		return React.DOM.div({id: this.props.graphName, style: {height: this.props.height}});
	}

	componentDidMount () {
		this.drawCharts();
	}

	componentDidUpdate () {
		this.drawCharts();
	}

	drawCharts () {
		var ChartClass = this.props.chartClass;
		var chart = new ChartClass (document.getElementById (this.props.graphName));
		chart.draw (this.props.table, this.props.options);
		if (this.props.selectListener !== undefined)
			google.visualization.events.addListener (chart, 'select', this.props.selectListener.bind (null, chart));
	}
}

export class GoogleChartsStateComponent extends React.Component {
	componentWillMount () {
		if (googleChartsLoaded)
			return;

		googleChartsStateComponents.push (this);
	}

	componentWillUnmount () {
		if (googleChartsLoaded)
			return;

		googleChartsStateComponents [googleChartsStateComponents.indexOf (this)] = undefined;
	}

	googleChartsLoaded () {
	}
}

export function calculateRunsRange (runs) {
	var min, max;
	var sum = 0;
	for (var i = 0; i < runs.length; ++i) {
		var v = runs [i].get ('elapsedMilliseconds');
		if (min === undefined || v < min)
			min = v;
		if (max === undefined || v > max)
			max = v;
		sum += v;
	}
	var mean = sum / runs.length;
	return [min, mean, mean, max];
}

export function normalizeRange (mean, range) {
	return range.map (x => x / mean);
}

export function hashForRunSets (runSets) {
	var ids = runSets.map (o => o.id);
	return ids.join ('+');
}

export class ConfigSelector extends React.Component {
	render () {
		function renderMachineOption (machine) {
			return <option value={machine.id} key={machine.id}>{machine.get ('name')}</option>;
		}
		function renderConfigOption (config) {
			return <option value={config.id} key={config.id}>{config.get ('name')}</option>;
		}
		let machineId;
		if (this.props.machine !== undefined)
			machineId = this.props.machine.id;
		let configId;
		if (this.props.config !== undefined)
			configId = this.props.config.id;
		return <div className="ConfigSelector">
			<select size="6" value={machineId} onChange={this.machineSelected.bind (this)}>
			{this.props.controller.allMachines.map (renderMachineOption)}
		</select>
			<select size="6" value={configId} onChange={this.configSelected.bind (this)}>
			{this.props.controller.allConfigs.map (renderConfigOption)}
		</select>
			</div>;
	}

	machineSelected (event) {
		let machine = this.props.controller.machineForId (event.target.value);
		this.props.onChange ({machine: machine, config: this.props.config});
	}

	configSelected (event) {
		let config = this.props.controller.configForId (event.target.value);
		this.props.onChange ({machine: this.props.machine, config: config});
	}

}

export class ConfigDescription extends React.Component {
	render () {
		let config = this.props.config;

		if (config === undefined)
			return <div className="ConfigDescription"></div>;

		let mono = config.get ('monoExecutable');
		let monoExecutable = mono === undefined
			? <span className="diagnostic">No mono executable specified.</span>
			: <code>{mono}</code>;
		let envVarsMap = config.get ('monoEnvironmentVariables') || {};
		let envVars = Object.keys (envVarsMap);
		let envVarsList = envVars.length === 0
			? <span className="diagnostic">No environment variables specified.</span>
			: <ul>
			{envVars.map (name => <li><code>{name + "=" + envVarsMap [name]}</code></li>)}
		</ul>;
		let options = config.get ('monoOptions') || [];
		let optionsList = options.length === 0
			? <span className="diagnostic">No command-line options specified.</span>
			: <code>{options.join (' ')}</code>;

		return <div className="ConfigDescription">
			<hr />
			<dl>
			<dt>Mono Executable</dt>
			<dd>{monoExecutable}</dd>
			<dt>Environment Variables</dt>
			<dd>{envVarsList}</dd>
			<dt>Command-line Options</dt>
			<dd>{optionsList}</dd>
			</dl>
			</div>;
	}
}

export class MachineDescription extends React.Component {
	render () {
		let machine = this.props.machine;

		if (machine === undefined)
			return <div className="MachineDescription"></div>;

		return <div className="MachineDescription">
			<dl>
			<dt>Name</dt>
			<dd>{machine.get ('name')}</dd>
			<dt>Architecture</dt>
			<dd>{machine.get ('architecture')}</dd>
			<dt>Dedicated?</dt>
			<dd>{machine.get ('isDedicated').toString ()}</dd>
			</dl>
		</div>;
	}
}

export function githubCommitLink (commit) {
	return "https://github.com/mono/mono/commit/" + commit;
}

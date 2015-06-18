/* @flow */

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

export function start (started: () => void) {
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

	allBenchmarks: Array<ParseObject>;
	allMachines: Array<ParseObject>;
	allRunSets: Array<ParseObject>;
	allConfigs: Array<ParseObject>;

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

	machinesLoaded (results: Array<ParseObject>) {
		console.log ("machines loaded: " + results.length);
		this.allMachines = results;
		this.checkAllDataLoaded ();
	}

	configsLoaded (results: Array<ParseObject>) {
		this.allConfigs = results;
		this.checkAllDataLoaded ();
	}

	runSetsLoaded (results: Array<ParseObject>) {
		console.log ("run sets loaded: " + results.length);
		this.allRunSets = results;
		this.checkAllDataLoaded ();
	}

	benchmarkNameForId (id: string) : (string | void) {
		for (var i = 0; i < this.allBenchmarks.length; ++i) {
			if (this.allBenchmarks [i].id === id)
				return this.allBenchmarks [i].get ('name');
		}
		return undefined;
	}

	machineForId (id: string) : ParseObject {
		return xp_utils.find (this.allMachines, m => m.id === id);
	}

	configForId (id: string) : ParseObject {
		return xp_utils.find (this.allConfigs, m => m.id === id);
	}

	runSetForId (id: string) : ParseObject {
		return xp_utils.find (this.allRunSets, rs => rs.id === id);
	}

	runSetsForMachineAndConfig (machine: ParseObject, config: ParseObject) : Array<ParseObject> {
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

var googleChartsStateComponents = [];

function googleChartsDidLoad () {
	if (googleChartsStateComponents === undefined)
		return;
	var components = googleChartsStateComponents;
	googleChartsStateComponents = undefined;
	for (var i = 0; i < components.length; ++i) {
		var component = components [i];
		if (component === undefined)
			continue;
		component.googleChartsLoaded ();
	}
}

export function canUseGoogleCharts (): boolean {
	return googleChartsStateComponents === undefined;
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
		if (googleChartsStateComponents === undefined)
			return;

		googleChartsStateComponents.push (this);
	}

	componentWillUnmount () {
		if (googleChartsStateComponents === undefined)
			return;

		googleChartsStateComponents [googleChartsStateComponents.indexOf (this)] = undefined;
	}

	googleChartsLoaded () {
	}
}

type Range = [number, number, number, number];

export function calculateRunsRange (runs: Array<ParseObject>): Range {
	var min: number | void;
	var max: number | void;
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
	if (min === undefined || max === undefined)
		min = max = 0;
	return [min, mean, mean, max];
}

export function normalizeRange (mean: number, range: Range) : Range {
	return range.map (x => x / mean);
}

export function hashForRunSets (runSets: Array<ParseObject>) : string {
	var ids = runSets.map (o => o.id);
	return ids.join ('+');
}

export class ConfigSelector extends React.Component {
	render () : Object {
		function renderMachineOption (machine) {
			return <option
				value={machine.id}
				key={machine.id}
				onDoubleClick={this.openMachineDescription.bind (this)}
				>{machine.get ('name')}</option>;
		}
		function renderConfigOption (config) {
			return <option
				value={config.id}
				key={config.id}
				onDoubleClick={this.openConfigDescription.bind (this)}
				>{config.get ('name')}</option>;
		}
		var machineId = undefined;
		if (this.props.machine !== undefined)
			machineId = this.props.machine.id;
		var configId = undefined;
		if (this.props.config !== undefined)
			configId = this.props.config.id;
		return <div className="ConfigSelector">
			<select size="6" value={machineId} onChange={this.machineSelected.bind (this)}>
			{this.props.controller.allMachines.map (renderMachineOption.bind (this))}
		</select>
			<select size="6" value={configId} onChange={this.configSelected.bind (this)}>
			{this.props.controller.allConfigs.map (renderConfigOption.bind (this))}
		</select>
			</div>;
	}

	openMachineDescription () {
		window.open ('machine.html#' + this.props.machine.id);
	}

	openConfigDescription () {
		window.open ('config.html#' + this.props.config.id);
	}

	machineSelected (event: Object) {
		var machine = this.props.controller.machineForId (event.target.value);
		this.props.onChange ({machine: machine, config: this.props.config});
	}

	configSelected (event: Object) {
		var config = this.props.controller.configForId (event.target.value);
		this.props.onChange ({machine: this.props.machine, config: config});
	}

}

export class ConfigDescription extends React.Component {
	render () : Object {
		var config = this.props.config;

		if (config === undefined)
			return <div className="Description"></div>;

		var mono = config.get ('monoExecutable');
		var monoExecutable = mono === undefined
			? <span className="diagnostic">No mono executable specified.</span>
			: <code>{mono}</code>;
		var envVarsMap = config.get ('monoEnvironmentVariables') || {};
		var envVars = Object.keys (envVarsMap);
		var envVarsList = envVars.length === 0
			? <span className="diagnostic">No environment variables specified.</span>
			: <ul>
			{envVars.map (name => <li><code>{name + "=" + envVarsMap [name]}</code></li>)}
		</ul>;
		var options = config.get ('monoOptions') || [];
		var optionsList = options.length === 0
			? <span className="diagnostic">No command-line options specified.</span>
			: <code>{options.join (' ')}</code>;

		return <div className="Description">
			<h1>{config.get ('name')}</h1>
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
	render () : Object {
		var machine = this.props.machine;

		if (machine === undefined)
			return <div className="Description"></div>;

		return <div className="Description">
			<h1>{machine.get ('name')}</h1>
			<dl>
			<dt>Architecture</dt>
			<dd>{machine.get ('architecture')}</dd>
			<dt>Dedicated?</dt>
			<dd>{machine.get ('isDedicated').toString ()}</dd>
			</dl>
		</div>;
	}
}

export class RunSetSelector extends React.Component {

	runSetSelected (event: Object) {
		var selection = this.props.selection;
		var runSetId = event.target.value;
		console.log ("run set selected: " + runSetId);
		var runSet = this.props.controller.runSetForId (runSetId);
		this.props.onChange ({machine: selection.machine, config: selection.config, runSet: runSet});
	}

	render () : Object {
		var selection = this.props.selection;
		console.log (selection);

		var machineId = undefined;
		var runSetId = undefined;
		var filteredRunSets = undefined;

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

		var config = selection.config === undefined
			? undefined
			: this.props.controller.configForId (selection.config.id);

		var configSelector =
			<ConfigSelector
		controller={this.props.controller}
		machine={selection.machine}
		config={config}
		onChange={this.props.onChange} />;
		var runSetsSelect = filteredRunSets.length === 0
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
			</div>;
	}

	getRunSet () : ParseObject {
		return this.state.runSet;
	}
}

export class ComparisonChart extends GoogleChartsStateComponent {

	constructor (props) {
		console.log ("run set compare chart constructing");

		super (props);

		this.invalidateState (props.runSets);
	}

	invalidateState (runSets) {
		this.state = {};
		this.runsByIndex = [];

		var query = new Parse.Query (Run);
		query.containedIn ('runSet', runSets)
			.limit (10000);
		query.find ({
			success: results => {
				if (this.props.runSets !== runSets)
					return;

				var runSetIndexById = {};
				runSets.forEach ((rs, i) => {
					this.runsByIndex [i] = [];
					runSetIndexById [rs.id] = i;
				});

				results.forEach (r => {
					var i = runSetIndexById [r.get ('runSet').id];
					if (this.runsByIndex [i] === undefined)
						this.runsByIndex [i] = [];
					this.runsByIndex [i].push (r);
				});

				this.runsLoaded ();
			},
			error: function (error) {
				alert ("error loading runs: " + error);
			}
		});
	}

	componentWillReceiveProps (nextProps) {
		this.invalidateState (nextProps.runSets);
	}

	googleChartsLoaded () {
		this.runsLoaded ();
	}

	runsLoaded () {
		var i;

		console.log ("run loaded");

		if (!canUseGoogleCharts ())
			return;

		for (i = 0; i < this.props.runSets.length; ++i) {
			if (this.runsByIndex [i] === undefined)
				return;
		}

		console.log ("all runs loaded");

		var commonBenchmarkIds;

		for (i = 0; i < this.props.runSets.length; ++i) {
			var runs = this.runsByIndex [i];
			var benchmarkIds = xp_utils.uniqStringArray (runs.map (o => o.get ('benchmark').id));
			if (commonBenchmarkIds === undefined) {
				commonBenchmarkIds = benchmarkIds;
				continue;
			}
			commonBenchmarkIds = xp_utils.intersectArray (benchmarkIds, commonBenchmarkIds);
		}

		if (commonBenchmarkIds === undefined)
			return;

		var dataArray = [];

		for (i = 0; i < commonBenchmarkIds.length; ++i) {
			var benchmarkId = commonBenchmarkIds [i];
			var row = [this.props.controller.benchmarkNameForId (benchmarkId)];
			var mean = undefined;
			for (var j = 0; j < this.props.runSets.length; ++j) {
				var filteredRuns = this.runsByIndex [j].filter (r => r.get ('benchmark').id === benchmarkId);
				var range = calculateRunsRange (filteredRuns);
				if (mean === undefined) {
					// FIXME: eventually we'll have more meaningful ranges
					mean = range [1];
				}
				row = row.concat (normalizeRange (mean, range));
			}
			dataArray.push (row);
		}

		var data = google.visualization.arrayToDataTable (dataArray, true);
		for (i = 0; i < this.props.runSets.length; ++i)
			data.setColumnLabel (1 + 4 * i, this.props.runSets [i].get ('startedAt'));

		var height = (35 + (15 * this.props.runSets.length) * commonBenchmarkIds.length) + "px";

		this.setState ({table: data, height: height});
	}

	render () {
		if (this.state.table === undefined)
			return <div className='diagnostic'>Loading&hellip;</div>;

		var options = { orientation: 'vertical' };
		return <GoogleChart
		graphName='compareChart'
		chartClass={google.visualization.CandlestickChart}
		height={this.state.height}
		table={this.state.table}
		options={options} />;
	}
}

export function githubCommitLink (commit: string) : string {
	return "https://github.com/mono/mono/commit/" + commit;
}

export function pageParseQuery (makeQuery: () => Object, success: (results: Array<ParseObject>) => void, error: (error: Object) => void) : void {
	function page (soFar: Array<Object>) {
		var query = makeQuery ();
		query.limit (1000).skip (soFar.length);
		query.find ({
			success: results => {
				soFar = soFar.concat (results);
				if (results.length == 1000) {
					page (soFar);
				} else {
					success (soFar);
				}
			},
			error: e => {
				error (e);
			}
		});
	}

	page ([]);
}

export function joinBenchmarkNames (controller: Controller, benchmarks: (Array<ParseObject> | void), prefix: string) : string {
	if (benchmarks === undefined || benchmarks.length === 0)
		return "";
	return prefix + benchmarks.map (b => controller.benchmarkNameForId (b.id)).join (", ");
}

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

		pageParseQuery (() => new Parse.Query (Machine),
			this.machinesLoaded.bind (this),
			function (error) {
				alert ("error loading machines: " + error);
			});

		pageParseQuery (() => new Parse.Query (RunSet).notEqualTo ('failed', true).include ('commit'),
			this.runSetsLoaded.bind (this),
			function (error) {
				alert ("error loading run sets: " + error);
			});

		pageParseQuery (() => new Parse.Query (Config),
			this.configsLoaded.bind (this),
			function (error) {
				alert ("error loading configs: " + error);
			});

		pageParseQuery (() => new Parse.Query (Benchmark),
			results => {
				this.allBenchmarks = results;
				this.checkAllDataLoaded ();
			},
			function (error) {
				alert ("error loading benchmarks: " + error);
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
		this.drawChart (this.props);
	}

	componentDidUpdate () {
		this.drawChart (this.props);
	}

	componentWillReceiveProps (nextProps) {
		if (this.shouldComponentUpdate (nextProps))
			return;
		console.log ("updating chart");
		this.updateChart (nextProps);
	}

	shouldComponentUpdate (nextProps, nextState) {
		if (this.props.chartClass !== nextProps.chartClass)
			return true;
		if (this.props.graphName !== nextProps.graphName)
			return true;
		if (this.props.height !== nextProps.height)
			return true;
		// FIXME: what do we do with the selectListener?
		return false;
	}

	updateChart (props) {
		this.chart.draw (props.table, props.options);
		if (props.selectListener !== undefined)
			google.visualization.events.addListener (this.chart, 'select', props.selectListener.bind (null, this.chart));
	}

	drawChart (props) {
		var ChartClass = props.chartClass;
		this.chart = new ChartClass (document.getElementById (props.graphName));
		this.updateChart (props);
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

export class AMChart extends React.Component {
	chart: Object;

	render () {
		return React.DOM.div({id: this.props.graphName, style: {height: this.props.height}});
	}

	componentDidMount () {
		console.log ("mounting chart")
		this.drawChart (this.props);
	}

	componentWillUnmount () {
		console.log ("unmounting chart")
		this.chart.clear ();
	}

	shouldComponentUpdate (nextProps, nextState) {
		if (this.props.graphName !== nextProps.graphName)
			return true;
		if (this.props.height !== nextProps.height)
			return true;
		if (this.props.options !== nextProps.options)
			return true;
		if (this.props.data !== nextProps.data)
			return true;
		// FIXME: what do we do with the selectListener?
		return false;
	}

	componentDidUpdate () {
		this.drawChart (this.props);
	}

	drawChart (props) {
		console.log ("drawing");
		if (this.chart === undefined) {
			var options = {};
			Object.keys (props.options).forEach (k => { options [k] = props.options [k] });
			options.dataProvider = props.data;
			this.chart = AmCharts.makeChart (props.graphName, options);
			if (this.props.selectListener !== undefined)
				this.chart.addListener ('clickGraphItem', e => { this.props.selectListener (e.index); });
		} else {
			this.chart.dataProvider = props.data;
			this.chart.validateData ();
		}
	}
}

var timelineOptions = {
				"type": "serial",
				"theme": "default",
				"categoryAxis": {
					"axisThickness": 0,
					"gridThickness": 0,
					"labelsEnabled": false,
					"tickLength": 0
				},
				"chartScrollbar": {
					"graph": "average"
				},
				"trendLines": [],
				"graphs": [
					{
						"balloonText": "[[lowName]]",
						"bullet": "round",
						"bulletAlpha": 0,
						"lineColor": "#3498DB",
						"lineThickness": 0,
						"id": "low",
						"title": "low",
						"valueField": "low"
					},
					{
						"balloonText": "[[highName]]",
						"bullet": "round",
						"bulletAlpha": 0,
						"lineColor": "#3498DB",
						"fillAlphas": 0.13,
						"fillToGraph": "low",
						"fillColors": "#3498DB",
						"id": "high",
						"lineThickness": 0,
						"title": "high",
						"valueField": "high"
					},
					{
						"balloonText": "[[tooltip]]",
						"bullet": "round",
						"bulletSize": 4,
						"lineColor": "#3498DB",
						"id": "average",
						"title": "average",
						"valueField": "average"
					}
				],
				"guides": [],
				"valueAxes": [
					{
						"baseValue": -13,
						"id": "time",
						"axisThickness": 0,
						"fontSize": 12,
						"gridAlpha": 0.07,
						"title": "",
						"titleFontSize": 0
					}
				],
				"allLabels": [],
				"balloon": {},
				"titles": []
			};

export class TimelineAMChart extends React.Component {
	render () {
		return <AMChart
			graphName={this.props.graphName}
			height={this.props.height}
			options={timelineOptions}
			data={this.props.data}
			selectListener={this.props.selectListener} />;
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

		var header = this.props.omitHeader
			? undefined
			: <h1>{config.get ('name')}</h1>;
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
			{header}
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
		var header = this.props.omitHeader
			? undefined
			: <h1>{machine.get ('name')}</h1>;

		return <div className="Description">
			{header}
			<dl>
			<dt>Architecture</dt>
			<dd>{machine.get ('architecture')}</dd>
			<dt>Dedicated?</dt>
			<dd>{machine.get ('isDedicated').toString ()}</dd>
			</dl>
		</div>;
	}
}

export class CombinedConfigSelector extends React.Component {
	render () : Object {
		function idsToString (ids) {
			var machineId = ids [0];
			var configId = ids [1];
			if (machineId === undefined || configId === undefined)
				return undefined;
			return machineId + "+" + configId;
		}

		var combinations = this.props.controller.allRunSets.map (rs => [rs.get ('machine').id, rs.get ('config').id]);
		var histogram = xp_utils.histogramByString (combinations, ids => idsToString (ids));

		var userStringForIds = ids => {
			var machine = this.props.controller.machineForId (ids [0]);
			var config = this.props.controller.configForId (ids [1]);
			return machine.get ('name') + " / " + config.get ('name');
		};

		histogram = xp_utils.sortArrayBy (histogram, e => userStringForIds (e [0]).toLowerCase ());

		function renderEntry (entry) {
			var ids = entry [0];
			var count = entry [1];
			var string = idsToString (ids);
			return <option
				value={string}
				key={string}>
				{userStringForIds (ids) + " (" + count + ")"}
			</option>;
		}

		var machineId = undefined;
		if (this.props.machine !== undefined)
			machineId = this.props.machine.id;
		var configId = undefined;
		if (this.props.config !== undefined)
			configId = this.props.config.id;
		var selectedValue = idsToString ([machineId, configId]);
		return <div className="CombinedConfigSelector">
			<select size="6" value={selectedValue} onChange={this.combinationSelected.bind (this)}>
			{histogram.map (renderEntry.bind (this))}
		</select>
			</div>;
	}

	combinationSelected (event: Object) {
		var ids = event.target.value.split ("+");
		var machine = this.props.controller.machineForId (ids [0]);
		var config = this.props.controller.configForId (ids [1]);
		this.props.onChange ({machine: machine, config: config});
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
		this.runsByIndex = [];

		pageParseQuery (
			() => {
				var query = new Parse.Query (Run);
				query.containedIn ('runSet', runSets);
				return query;
			},
			results => {
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
			function (error) {
				alert ("error loading runs: " + error);
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

		if (commonBenchmarkIds === undefined || commonBenchmarkIds.length == 0)
			return;

		commonBenchmarkIds = xp_utils.sortArrayBy (commonBenchmarkIds, id => this.props.controller.benchmarkNameForId (id));

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

		this.table = data;
		this.height = height;
		this.forceUpdate ();
	}

	render () {
		if (this.table === undefined)
			return <div className='diagnostic'>Loading&hellip;</div>;

		var options = {
			orientation: 'vertical',
			chartArea: {height: '100%'},
			animation: {
				duration: 1000,
				easing: 'out',
			},
			hAxis: {
				gridlines: {
					color: 'transparent'
				},
				baseline: 1.0,
				textPosition: 'none'
			},
			vAxis: {
				gridlines: {
					color: 'transparent'
				}
			}
		};
		return <GoogleChart
		graphName='compareChart'
		chartClass={google.visualization.CandlestickChart}
		height={800 /*{this.height}*/}
		table={this.table}
		options={options} />;
	}
}

export function githubCommitLink (commit: string) : string {
	return "https://github.com/mono/mono/commit/" + commit;
}

export function pageParseQuery (makeQuery: () => Object, success: (results: Array<ParseObject>) => void, error: (error: Object) => void) : void {
    var limit = 1000;

    function done (results) {
		var values = [];
		for(var key in results) {
			var value = results [key];
			values.push (value);
		}
		success (values);
    }

    function page (skip, earliest, previousResults, soFar) {
		var query = makeQuery ();
		query.limit (limit);
		query.ascending ('createdAt');

		if (skip >= 10000) {
			skip = 0;
			earliest = previousResults [previousResults.length - 1].createdAt;
		}

		query.skip (skip);
		if (earliest !== undefined)
			query.greaterThanOrEqualTo ('createdAt', earliest);

		query.find ({
			success: function (results) {
				for (var i = 0; i < results.length; ++i)
					soFar [results [i].id] = results [i];
				if (results.length >= limit)
					page (skip + limit, earliest, results, soFar);
				else
					done (soFar);
			},
			error: function (e) {
				console.log ("Parse error:");
				console.log (e);
				error (e);
			}
		});
    }

    page (0, undefined, undefined, {});
}

export function joinBenchmarkNames (controller: Controller, benchmarks: (Array<ParseObject> | void), prefix: string) : string {
	if (benchmarks === undefined || benchmarks.length === 0)
		return "";
	return prefix + benchmarks.map (b => controller.benchmarkNameForId (b.id)).join (", ");
}

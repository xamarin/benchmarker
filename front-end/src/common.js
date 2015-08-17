/* @flow */

"use strict";

import * as xp_utils from './utils.js';
import {Parse} from 'parse';
import React from 'react';

export var Benchmark = Parse.Object.extend ('Benchmark');
export var Config = Parse.Object.extend ('Config');
export var Machine = Parse.Object.extend ('Machine');
export var Run = Parse.Object.extend ('Run');
export var RunSet = Parse.Object.extend ('RunSet');
export var PullRequest = Parse.Object.extend ('PullRequest');

export var xamarinColors = {
	//        light2     light1     normal     dark1      dark2
	"blue": [ "#91E2F4", "#4FCAE6", "#1FAECE", "#3192B3", "#2C7797" ],
	"teal": [ "#A3EBE1", "#7AD5C9", "#44B8A8", "#38A495", "#278E80" ],
	"green": [ "#CFEFA7", "#B3E770", "#91CA47", "#6FA22E", "#5A8622" ],
	"violet": [ "#CEC0EC", "#B5A1E0", "#9378CD", "#7E68C2", "#614CA0" ],
	"red": [ "#F8C6BB", "#F69781", "#F56D4F", "#E2553D", "#BC3C26" ],
	"amber": [ "#F7E28B", "#F9D33C", "#F1C40F", "#F0B240", "#E7963B" ],
	"gray": [ "#ECF0F1", "#D1D9DD", "#ADB7BE", "#9AA4AB", "#76828A" ],
	"asphalt": [ "#889DB5", "#66819E", "#365271", "#2B3E50", "#1C2B39" ]
};
export var xamarinColorsOrder = [ "blue", "green", "violet", "red", "asphalt", "amber", "gray", "teal" ];

export function start (started: () => void) {
	Parse.initialize('7khPUBga9c7L1YryD1se1bp6VRzKKJESc0baS9ES', 'qnBBT97Mttqsvq3g9zghnBVn2iiHLAQvTzekUigm');

	started ();
}

export class Controller {

	allBenchmarks: Array<Parse.Object>;
	allMachines: Array<Parse.Object>;
	allRunSets: Array<Parse.Object>;
	pullRequestRunSets: Array<Parse.Object>;
	allConfigs: Array<Parse.Object>;

	loadAsync () {

		pageParseQuery (() => new Parse.Query ((Machine : Parse.Object)),
			this.machinesLoaded.bind (this),
			function (error) {
				alert ("error loading machines: " + error.toString ());
			});

		pageParseQuery (() => new Parse.Query (RunSet)
				.notEqualTo ('failed', true)
				.include ('commit')
				.include ('pullRequest')
				.include ('pullRequest.baselineRunSet')
				.include ('pullRequest.baselineRunSet.commit'),
			this.runSetsLoaded.bind (this),
			function (error) {
				alert ("error loading run sets: " + error.toString ());
			});

		pageParseQuery (() => new Parse.Query (Config),
			this.configsLoaded.bind (this),
			function (error) {
				alert ("error loading configs: " + error.toString ());
			});

		pageParseQuery (() => new Parse.Query (Benchmark),
			results => {
				this.allBenchmarks = results;
				this.checkAllDataLoaded ();
			},
			function (error) {
				alert ("error loading benchmarks: " + error.toString ());
			});
	}

	allDataLoaded () {
	}

	machinesLoaded (results: Array<Parse.Object>) {
		this.allMachines = results;
		this.checkAllDataLoaded ();
	}

	configsLoaded (results: Array<Parse.Object>) {
		this.allConfigs = results;
		this.checkAllDataLoaded ();
	}

	runSetsLoaded (results: Array<Parse.Object>) {
		var partition = xp_utils.partitionArrayByString (results, rs => (rs.get ('pullRequest') === undefined).toString ());
		this.allRunSets = partition ["true"];
		this.pullRequestRunSets = partition ["false"];
		this.checkAllDataLoaded ();
	}

	allEnabledBenchmarks () : Array<Parse.Object> {
		return this.allBenchmarks.filter (b => !b.get ('disabled'));
	}

	benchmarkForId (id: string) : (Parse.Object | void) {
		for (var i = 0; i < this.allBenchmarks.length; ++i) {
			if (this.allBenchmarks [i].id === id)
				return this.allBenchmarks [i];
		}
		return undefined;
	}

	benchmarkNameForId (id: string) : string {
		var benchmark = this.benchmarkForId (id);
		if (benchmark === undefined)
			return "*unknown*";
		return benchmark.get ('name');
	}

	machineForId (id: string) : Parse.Object {
		var machine = xp_utils.find (this.allMachines, m => m.id === id);
		if (machine === undefined)
			throw "Machine not found";
		return machine;
	}

	configForId (id: string) : Parse.Object {
		var config = xp_utils.find (this.allConfigs, m => m.id === id);
		if (config === undefined)
			throw "Config not found";
		return config;
	}

	runSetForId (id: string) : Parse.Object {
		var runSet = xp_utils.find (this.allRunSets, rs => rs.id === id);
		if (runSet === undefined)
			throw "Run set not found";
		return runSet;
	}

	runSetsForMachineAndConfig (machine: Parse.Object, config: Parse.Object) : Array<Parse.Object> {
		return this.allRunSets.filter (
			rs =>
				rs.get ('machine').id === machine.id &&
				rs.get ('config').id === config.id
		);
	}

	runSetForPullRequestId (id: string) : Parse.Object {
		for (var i = 0; i < this.pullRequestRunSets.length; ++i) {
			var rs = this.pullRequestRunSets [i];
			var pr = rs.get ('pullRequest');
			if (pr.id === id)
				return rs;
		}
		throw "Run set for pull request not found";
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

export function hashForRunSets (runSets: Array<Parse.Object>) : string {
	var ids = runSets.map (o => o.id);
	return ids.join ('+');
}

type MachineConfigSelection = {
	machine: Parse.Object | void;
	config: Parse.Object | void;
}

type ConfigSelectorProps = {
	controller: Controller;
	machine: Parse.Object | void;
	config: Parse.Object | void;
	onChange: (selection: MachineConfigSelection) => void;
};

type ConfigDescriptionProps = {
	config: Parse.Object | void;
	omitHeader: boolean;
};

export class ConfigDescription extends React.Component<ConfigDescriptionProps, ConfigDescriptionProps, void> {
	render () : Object {
		var config = this.props.config;

		if (config === undefined)
			return <div></div>;

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

type MachineDescriptionProps = {
	machine: Parse.Object | void;
	omitHeader: boolean;
};

export class MachineDescription extends React.Component<MachineDescriptionProps, MachineDescriptionProps, void> {
	render () : Object {
		var machine = this.props.machine;

		if (machine === undefined)
			return <div></div>;
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

export class CombinedConfigSelector extends React.Component<ConfigSelectorProps, ConfigSelectorProps, void> {
	render () : Object {
		function idsToString (machine, config) : string {
			return machine + "+" + config;
		}

		var combinations = this.props.controller.allRunSets.map (rs => [rs.get ('machine').id, rs.get ('config').id]);
		var histogram = xp_utils.histogramByString (combinations, ids => idsToString (ids [0], ids [1]));

		var userStringForIds = ids => {
			var machine = this.props.controller.machineForId (ids [0]);
			var config = this.props.controller.configForId (ids [1]);
			return machine.get ('name') + " / " + config.get ('name');
		};

		histogram = xp_utils.sortArrayLexicographicallyBy (histogram, e => userStringForIds (e [0]).toLowerCase ());

		var machines = {};
		for (var i = 0; i < histogram.length; ++i) {
			var machineId = histogram [i] [0] [0];
			var configId = histogram [i] [0] [1];
			var count = histogram [i] [1];
			var machine = this.props.controller.machineForId (machineId).get ('name');
			var config = this.props.controller.configForId (configId).get ('name');
			machines [machine] = machines [machine] || [];
			machines [machine].push ({
				configId: configId,
				configName: config,
				count: count,
				machineId: machineId,
				machineName: machine,
			});
		}

		function renderEntry (entry) {
			var string = idsToString (entry.machineId, entry.configId);
			return <option
				value={string}
				key={string}>
				{entry.configName} ({entry.count})
			</option>;
		}

		function renderGroup (machines, machine) {
			return <optgroup label={machine}>
				{xp_utils.sortArrayNumericallyBy (machines [machine], x => -x.count).map (renderEntry.bind (this))}
			</optgroup>;
		}

		var machineId = undefined;
		if (this.props.machine !== undefined)
			machineId = this.props.machine.id;
		var configId = undefined;
		if (this.props.config !== undefined)
			configId = this.props.config.id;
		var selectedValue = (machineId === undefined || configId === undefined) ? undefined : idsToString (machineId, configId);
		var aboutConfig = undefined;
		var aboutMachine = undefined;
		if (this.props.showControls) {
			aboutConfig = <button onClick={this.openConfigDescription.bind (this)}>About Config</button>;
			aboutMachine = <button onClick={this.openMachineDescription.bind (this)}>About Machine</button>;
		}
		return <div className="CombinedConfigSelector">
			<label>Machine &amp; Config</label>
			<select size="6" value={selectedValue} onChange={this.combinationSelected.bind (this)}>
				{Object.keys (machines).map (renderGroup.bind (this, machines))}
			</select>
			{aboutConfig}{aboutMachine}
			<div style={{ clear: 'both' }}></div>
		</div>;
	}

	openConfigDescription () {
        if (this.props.config === undefined)
            return;
        window.open ('config.html#' + this.props.config.id)
	}

	openMachineDescription () {
        if (this.props.machine === undefined)
            return;
        window.open ('machine.html#' + this.props.machine.id)
	}

	combinationSelected (event: Object) {
		var ids = event.target.value.split ("+");
		var machine = this.props.controller.machineForId (ids [0]);
		var config = this.props.controller.configForId (ids [1]);
		this.props.onChange ({machine: machine, config: config});
	}
}

type RunSetSelection = {
	machine: Parse.Object | void;
	config: Parse.Object | void;
	runSet: Parse.Object | void;
}

type RunSetSelectorProps = {
	controller: Controller;
	selection: RunSetSelection;
	onChange: (selection: RunSetSelection) => void;
};

export class RunSetSelector extends React.Component<RunSetSelectorProps, RunSetSelectorProps, void> {

	runSetSelected (event: Object) {
		var selection = this.props.selection;
		var runSetId = event.target.value;
		var runSet = this.props.controller.runSetForId (runSetId);
		this.props.onChange ({machine: selection.machine, config: selection.config, runSet: runSet});
	}

	configSelected (selection: MachineConfigSelection) {
		this.props.onChange ({machine: selection.machine, config: selection.config, runSet: undefined});
	}

	render () : Object {
		var selection = this.props.selection;

		var runSetId = undefined;
		var filteredRunSets = undefined;

		if (selection.runSet !== undefined)
			runSetId = selection.runSet.id;

		if (selection.machine !== undefined && selection.config !== undefined)
			filteredRunSets = this.props.controller.runSetsForMachineAndConfig (selection.machine, selection.config);

		function renderRunSetOption (rs) {
			return <option value={rs.id} key={rs.id}>{rs.get ('startedAt').toString ()}</option>;
		}

		var config = selection.config === undefined
			? undefined
			: this.props.controller.configForId (selection.config.id);

		var configSelector =
			<CombinedConfigSelector
				controller={this.props.controller}
				machine={selection.machine}
				config={config}
				onChange={this.configSelected.bind (this)}
				showControls={true} />;
		var runSetsSelect = undefined;
		if (filteredRunSets === undefined) {
			runSetsSelect = <select size="6" disabled="true">
				<option className="diagnostic">Please select a machine and config.</option>
			</select>;
		} else if (filteredRunSets.length === 0) {
			runSetsSelect = <select size="6" disabled="true">
				<option className="diagnostic">No run sets found for this machine and config.</option>
			</select>;
		} else {
			runSetsSelect = <select
				size="6"
				selectedIndex="-1"
				value={runSetId}
				onChange={this.runSetSelected.bind (this)}>
				{filteredRunSets.map (renderRunSetOption)}
			</select>;
		}
		return <div className="RunSetSelector">
			{configSelector}
			<label>Run Set</label>
			{runSetsSelect}
			</div>;
	}
}

export function githubCommitLink (commit: string) : string {
	return "https://github.com/mono/mono/commit/" + commit;
}

export function pageParseQuery (makeQuery: () => Object, success: (results: Array<Parse.Object>) => void, error: (error: Object) => void) : void {
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
				console.log ("Parse error: ", e);
				error (e);
			}
		});
    }

    page (0, undefined, [], {});
}

export function joinBenchmarkNames (controller: Controller, benchmarks: (Array<Parse.Object> | void), prefix: string) : string {
	if (benchmarks === undefined || benchmarks.length === 0)
		return "";
	return prefix + benchmarks.map (b => controller.benchmarkNameForId (b.id)).join (", ");
}

type NavigationProps = {
	currentPage: string;
}

export class Navigation extends React.Component<NavigationProps, NavigationProps, void> {

	render () : Object {
		var classFor = (page) =>
			this.props.currentPage === page ? 'selected' : 'deselected';
		return <div className="Navigation">
			<ul>
				<li
					title="View a timeline of all benchmarks"
					className={classFor ('timeline')}>
					<a href="index.html">Timeline</a>
				</li>
				<li
					title="Compare the results of multiple run sets"
					className={classFor ('compare')}>
					<a href="compare.html">Compare</a>
				</li>
			</ul>
		</div>;
	}

}

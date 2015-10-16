///<reference path="../typings/react/react.d.ts"/>
///<reference path="../typings/github-api/github-api.d.ts"/>

/* @flow */

/* global process */

"use strict";

declare var process: any;

import * as xp_utils from './utils.ts';
import * as Database from './database.ts';
import * as Outliers from './outliers.ts';
import React = require ('react');
import GitHub = require ('github-api');

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

type ConfigDescriptionProps = {
	config: Database.DBObject;
	omitHeader: boolean;
};

export class ConfigDescription extends React.Component<ConfigDescriptionProps, void> {
	render () : JSX.Element {
		var config = this.props.config;

		if (config === undefined)
			return <div></div>;

		var header = this.props.omitHeader
			? undefined
			: <h1>Config: {config.get ('name')}</h1>;
		var mono = config.get ('monoExecutable');
		var monoExecutable = mono === undefined
			? <span className="diagnostic">No mono executable specified.</span>
			: <code>{mono}</code>;
		var envVarsMap = config.get ('monoEnvironmentVariables') || {};
		var envVars = Object.keys (envVarsMap);
		var envVarsList = envVars.length === 0
			? <span className="diagnostic">No environment variables specified.</span>
			: <ul>
			{envVars.map ((name, i) => <li key={"var" + i.toString ()}><code>{name + "=" + envVarsMap [name]}</code></li>)}
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
	machine: Database.DBObject;
	omitHeader: boolean;
};

export class MachineDescription extends React.Component<MachineDescriptionProps, void> {
	render () : JSX.Element {
		var machine = this.props.machine;

		if (machine === undefined)
			return <div></div>;
		var header = this.props.omitHeader
			? undefined
			: <h1>Machine: {machine.get ('name')}</h1>;

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

type MachineConfigSelection = {
	machine: Database.DBObject;
	config: Database.DBObject;
	metric: string;
};

type ConfigSelectorProps = {
	includeMetric: boolean;
	runSetCounts: Array<Database.RunSetCount>;
	featuredTimelines: Array<Database.DBObject>;
	machine: Database.DBObject;
	config: Database.DBObject;
	metric: string;
	showControls: boolean;
	onChange: (selection: MachineConfigSelection) => void;
};

export class CombinedConfigSelector extends React.Component<ConfigSelectorProps, void> {
	runSetCounts () {
		if (this.props.includeMetric)
			return this.props.runSetCounts;
		return Database.combineRunSetCountsAcrossMetric (this.props.runSetCounts);
	}

	render () : JSX.Element {
		var userStringForRSC = r => {
			var s = r.machine.get ('name') + " / " + r.config.get ('name');
			if (this.props.includeMetric)
				s = s + " / " + r.metric;
			return s;
		};

		var valueStringForRSC = (machine: Database.DBObject, config: Database.DBObject, metric: string) => {
			var s = machine.get ('name') + '+' + config.get ('name');
			if (this.props.includeMetric)
				s = s + '+' + metric;
			return s;
		};

		var histogram = xp_utils.sortArrayLexicographicallyBy (this.runSetCounts (), r => userStringForRSC (r).toLowerCase ());

		type MachinesMap = { [name: string]: Array<Database.RunSetCount> };
		var machines: MachinesMap = {};
		var featuredRSCs = [];
		for (var i = 0; i < histogram.length; ++i) {
			var rsc = histogram [i];
			var machineName = rsc.machine.get ('name');

			if (this.props.featuredTimelines !== undefined) {
				var configName = rsc.config.get ('name');
				var index = xp_utils.findIndex (this.props.featuredTimelines, ftl => {
					return ftl.get ('machine') === machineName && ftl.get ('config') === configName && ftl.get ('metric') === rsc.metric;
				});
				if (index >= 0) {
					featuredRSCs [index] = {
						machine: rsc.machine,
						config: rsc.config,
						metric: rsc.metric,
						ids: [], // just for typechecking purposes
						displayString: this.props.featuredTimelines [index].get ('name'),
						count: rsc.count
					};
				}
			}

			machines [machineName] = machines [machineName] || [];
			machines [machineName].push (rsc);
		}

		function renderRSC (entry: Database.RunSetCount, displayString: string) {
			var string = valueStringForRSC (entry.machine, entry.config, entry.metric);
			if (displayString === undefined) {
				displayString = entry.config.get ('name');
				if (this.props.includeMetric)
					displayString = displayString + " / " + entry.metric;
			}
			displayString = displayString + " (" + entry.count + ")";
			return <option
				value={string}
				key={string}>
				{displayString}
			</option>;
		}
		
		function renderFeaturedTimelines () {
			if (this.props.featuredTimelines === undefined)
				return undefined;

			return <optgroup label="Featured">
				{featuredRSCs.map (rsc => renderRSC.call (this, rsc, rsc.displayString))}
			</optgroup>;
		}

		function renderGroup (machines: MachinesMap, machineName: string) {
			return <optgroup key={"group" + machineName} label={machineName}>
				{xp_utils.sortArrayNumericallyBy (machines [machineName], x => -x.count).map (rsc => renderRSC.call (this, rsc, undefined))}
			</optgroup>;
		}

		var machine = this.props.machine;
		var config = this.props.config;
		var metric = this.props.metric;
		var selectedValue;
		if (!(machine === undefined || config === undefined || (this.props.includeMetric && metric === undefined)))
			selectedValue = valueStringForRSC (machine, config, metric);
		var aboutConfig = undefined;
		var aboutMachine = undefined;
		if (this.props.showControls) {
			aboutConfig = <button onClick={this.openConfigDescription.bind (this)}>About Config</button>;
			aboutMachine = <button onClick={this.openMachineDescription.bind (this)}>About Machine</button>;
		}
		return <div className="CombinedConfigSelector">
			<label>Machine &amp; Config</label>
			<select size={6} value={selectedValue} onChange={this.combinationSelected.bind (this)}>
				{renderFeaturedTimelines.bind (this) ()}
				{Object.keys (machines).map (renderGroup.bind (this, machines))}
			</select>
			{aboutConfig}{aboutMachine}
			<div style={{ clear: 'both' }}></div>
		</div>;
	}

	openConfigDescription () {
		if (this.props.config === undefined)
			return;
		window.open ('config.html#name=' + this.props.config.get ('name'));
	}

	openMachineDescription () {
		if (this.props.machine === undefined)
			return;
		window.open ('machine.html#name=' + this.props.machine.get ('name'));
	}

	combinationSelected (event: React.FormEvent) {
		var target: any = event.target;
		var names = target.value.split ('+');
		var rsc = Database.findRunSetCount (this.runSetCounts (), names [0], names [1], names [2]);
		if (rsc !== undefined)
			this.props.onChange (rsc);
		else
			console.log ("Couldn't find run set count.");
	}
}

export type RunSetSelection = {
	machine: Database.DBObject;
	config: Database.DBObject;
	runSet: Database.DBRunSet;
}

type RunSetSelectorProps = {
	runSetCounts: Array<Database.RunSetCount>;
	selection: RunSetSelection;
	onChange: (selection: RunSetSelection) => void;
};

type RunSetSelectorState = {
	runSets: Array<Database.DBRunSet>;
};

export class RunSetSelector extends React.Component<RunSetSelectorProps, RunSetSelectorState> {
	constructor (props: RunSetSelectorProps) {
		super (props);
		this.state = { runSets: undefined };
	}

	componentWillMount () {
		this.fetchData (this.props);
	}

	componentWillReceiveProps (nextProps: RunSetSelectorProps) {
		function getName (obj) {
			if (obj === undefined)
				return undefined;
			return obj.get ('name');
		}

		if (this.props.runSetCounts === nextProps.runSetCounts &&
				getName (this.props.selection.machine) === getName (nextProps.selection.machine) &&
				getName (this.props.selection.config) === getName (nextProps.selection.config)) {
			return;
		}
		this.setState ({ runSets: undefined });
		this.fetchData (nextProps);
	}

	fetchData (props: RunSetSelectorProps) {
		var machine = props.selection.machine;
		var config = props.selection.config;

		if (machine === undefined || config === undefined)
			return;

		Database.fetchRunSetsForMachineAndConfig (machine, config, runSets => {
			this.setState ({ runSets: runSets });
		}, error => {
			alert ("error loading run sets: " + error.toString ());
		});
	}

	runSetSelected (event: React.FormEvent) {
		if (this.state.runSets === undefined)
			return;
		var target: any = event.target;
		var runSetId: number = target.value;
		var runSet = Database.findRunSet (this.state.runSets, runSetId);
		if (runSet !== undefined)
			this.props.onChange ({machine: runSet.machine, config: runSet.config, runSet: runSet});
	}

	configSelected (selection: MachineConfigSelection) {
		this.props.onChange ({machine: selection.machine, config: selection.config, runSet: undefined});
	}

	render () : JSX.Element {
		var selection = this.props.selection;
		var machine = selection.machine;
		var config = selection.config;
		var runSets = this.state.runSets;

		var runSetId = undefined;

		if (selection.runSet !== undefined)
			runSetId = selection.runSet.get ('id');

		function openRunSetDescription (id: number) {
			return window.open ('runset.html#id=' + id);
		}

		function renderRunSet (runSet: Database.DBRunSet) {
			var id = runSet.get ('id');
			return <option value={id} key={id} onDoubleClick={openRunSetDescription.bind (this, id)}>
				{xp_utils.formatDate (runSet.commit.get ('commitDate'))} - {runSet.commit.get ('hash').substring (0, 10)}
			</option>;
		}

		var configSelector =
			<CombinedConfigSelector
				featuredTimelines={undefined}
				metric={undefined}
				includeMetric={false}
				runSetCounts={this.props.runSetCounts}
				machine={selection.machine}
				config={selection.config}
				onChange={this.configSelected.bind (this)}
				showControls={true} />;
		var runSetsSelect = undefined;
		if (runSets === undefined && machine !== undefined && config !== undefined) {
			runSetsSelect = <div className="diagnostic">Loading&hellip;</div>;
		} else if (runSets === undefined) {
			runSetsSelect = <div className="diagnostic">Please select a machine and config.</div>;
		} else if (runSets.length === 0) {
			runSetsSelect = <div className="diagnostic">No run sets found for this machine and config.</div>;
		} else {
			runSetsSelect = <select
				size={6}
				value={runSetId}
				onChange={this.runSetSelected.bind (this)}>
				{runSets.map (renderRunSet)}
			</select>;
		}
		return <div className="RunSetSelector">
			{configSelector}
			<label>Run Set</label>
			{runSetsSelect}
			</div>;
	}
}

function descriptiveMetricName (metric: string) : string {
	switch (metric) {
	case 'time':
		return "Elapsed Times (ms)";
	case 'instructions':
		return "Instruction count";
	case 'memory-integral':
		return "Memory usage (MB * Giga Instructions)";
	default:
		return "Unknown metric";
	}
}

type RunSetDescriptionProps = {
	runSet: Database.DBRunSet;
};

type RunSetDescriptionState = {
	results: Array<Object>;
	secondaryCommits: Array<Object>;
};

export class RunSetDescription extends React.Component<RunSetDescriptionProps, RunSetDescriptionState> {
	constructor (props: RunSetDescriptionProps) {
		super (props);
		this.state = { results: undefined, secondaryCommits: undefined };
		this.fetchResults (props.runSet);
	}

	fetchResults (runSet: Database.DBRunSet) {
		Database.fetch ('results?runset=eq.' + runSet.get ('id'),
			objs => {
				if (runSet !== this.props.runSet)
					return;
				this.setState ({ results: objs } as any);
			}, error => {
				alert ("error loading results: " + error.toString ());
			});
		var secondaryCommits = runSet.get ('secondaryCommits');
		if (secondaryCommits !== undefined && secondaryCommits.length > 0) {
			Database.fetch ('commit?hash=in.' + secondaryCommits.join (','),
				objs => {
					if (runSet !== this.props.runSet)
						return;
					this.setState ({ secondaryCommits: objs } as any);
				}, error => {
					alert ("error loading commits: " + error.toString ());
				});
		}
	}

	componentWillReceiveProps (nextProps: RunSetDescriptionProps) {
		this.setState ({ results: undefined, secondaryCommits: undefined });
		this.fetchResults (nextProps.runSet);
	}

	render () : JSX.Element  {
		var runSet = this.props.runSet;
		var buildURL = runSet.get ('buildURL');
		var buildLink;
		var logURLs = runSet.get ('logURLs');
		var logLinks = [];
		var logLinkList;
		var table;
		var secondaryProductsList;

		if (buildURL !== undefined)
			buildLink = <a href={buildURL}>build</a>;

		if (logURLs !== undefined && Object.keys (logURLs).length !== 0) {
			for (var key in logURLs) {
				var url = logURLs [key];
				var anchor = document.createElement ('a');
				anchor.href = url;

				var shortUrl = <span>{anchor.hostname}/&hellip;{anchor.pathname.substring (anchor.pathname.lastIndexOf ('/'))}</span>;
				logLinks.push(<li key={"link" + key}><a href={url}><code>{key}</code> ({shortUrl})</a></li>);
			}
			if (logLinks.length === 0) {
				logLinkList = undefined;
			} else {
				logLinkList = <ul key="logLinks">{logLinks}</ul>;
			}
		}

		var secondaryCommits = runSet.get ('secondaryCommits');
		if (secondaryCommits !== undefined && secondaryCommits.length > 0) {
			var elements;
			if (this.state.secondaryCommits === undefined) {
				elements = secondaryCommits.map (c => {
					var short = c.substring (0, 10);
					return <li key={"commit" + c}>{short}</li>;
				});
			} else {
				elements = this.state.secondaryCommits.map (c => {
					var short = c ['hash'].substring(0, 10);
					var link = githubCommitLink (c ['product'], c ['hash']);
					return <li key={"commit" + c ['hash']}><a href={link}>{c ['product']} {short}</a></li>;
				});
			}
			secondaryProductsList = [<h1 key="secondaryProductsHeader">Secondary products</h1>,
					<ul key="secondaryProductsList" className='secondaryProducts'>{elements}</ul>];
		}

		if (this.state.results === undefined) {
			table = <div key="table" className='DiagnosticBlock'>Loading run data&hellip;</div>;
		} else {
			var resultsByBenchmark = {};
			var metricsDict = {};
			for (var i = 0; i < this.state.results.length; ++i) {
				var result = this.state.results [i];
				var benchmark = result ['benchmark'];
				var metric = result ['metric'];
				var entry = resultsByBenchmark [benchmark] || { metrics: {}, disabled: result ['disabled'] };
				entry.metrics [metric] = result ['results'];
				resultsByBenchmark [benchmark] = entry;

				metricsDict [metric] = {};
			}
			var crashedBenchmarks = runSet.get ('crashedBenchmarks');
			var timedOutBenchmarks = runSet.get ('timedOutBenchmarks');
			var benchmarkNames = Object.keys (resultsByBenchmark);
			benchmarkNames.sort ();
			var metrics = Object.keys (metricsDict);
			metrics.sort ();
			var tableHeaders = [];
			metrics.forEach (m => {
				tableHeaders.push (<th key={"metricValues" + m}>{descriptiveMetricName (m)}</th>);
				tableHeaders.push (<th key={"metricDegree" + m}>Bias due to Outliers</th>);
			});
			table = <table key="table">
				<thead>
				<tr key="header">
					<th key="name">Benchmark</th>
					<th key="status">Status</th>
					{tableHeaders}
				</tr>
				</thead>
				<tbody>
				{benchmarkNames.map (name => {
					var result = resultsByBenchmark [name];
					var disabled = result.disabled;
					var statusIcons = [];
					if (Array.isArray (crashedBenchmarks) && crashedBenchmarks.indexOf (name) !== -1)
						statusIcons.push (<span key="crashed" className="statusIcon crashed fa fa-exclamation-circle" title="Crashed"></span>);
					if (Array.isArray (timedOutBenchmarks) && timedOutBenchmarks.indexOf (name) !== -1)
						statusIcons.push (<span key="timedOut" className="statusIcon timedOut fa fa-clock-o" title="Timed Out"></span>);
					if (statusIcons.length === 0)
						statusIcons.push (<span key="good" className="statusIcon good fa fa-check" title="Good"></span>);

					var metricColumns = [];
					metrics.forEach (m => {
						var dataPoints = result.metrics [m];
						dataPoints.sort ();
						var dataPointsString = dataPoints.join (", ");
						var variance = Outliers.outlierVariance (dataPoints);
						metricColumns.push (<td key={"metricValues" + m}>{dataPointsString}</td>);
						metricColumns.push (<td key={"metricDegree" + m}>
								<div className="degree" title={variance}>
									<div className={variance}>&nbsp;</div>
								</div>
							</td>);
					});

					return <tr key={"benchmark" + name} className={disabled ? 'disabled' : ''}>
						<td key="name"><code>{name}</code>{disabled ? ' (disabled)' : ''}</td>
						<td key="status" className="statusColumn">{statusIcons}</td>
						{metricColumns}
					</tr>;
				})}
			</tbody></table>;
		}

		var commitHash = runSet.get ('commit');
		var product = runSet.commit ? runSet.commit.get ('product') : 'mono';
		var commitLink = githubCommitLink (product, commitHash);


		return <div className="Description">
			<h1 key="commit"><a href={commitLink}>{commitHash.substring (0, 10)}</a> ({buildLink}, <a href={'compare.html#ids=' + runSet.get ('id')}>compare</a>)</h1>
			{logLinkList}
			{secondaryProductsList}
			{table}
		</div>;
	}
}

export function githubCommitLink (product: string, commit: string) : string {
	var repo = "";
	switch (product) {
		case 'mono':
			repo = "mono/mono";
			break;
		case 'monodroid':
			repo = "xamarin/monodroid";
			break;
		default:
			alert("Unknown product " + product);
			return "";
	}
	return "https://github.com/" + repo + "/commit/" + commit;
}

export function githubCompareLink (base: string, compare: string) : string {
	return "https://github.com/mono/mono/compare/" + base + "..." + compare;
}

type NavigationProps = {
	currentPage: string;
}

export class Navigation extends React.Component<NavigationProps, void> {
	openDeployment () : boolean {
		var lastSlashIndex = window.location.href.search ("/[^/]+$");
		var path = window.location.href.substring (lastSlashIndex + 1);
		var deploymentLink = "http://xamarin.github.io/benchmarker/front-end/" + path;
		window.open (deploymentLink);
		return false;
	}

	render () : JSX.Element {
		var deploymentLink;
		if (process.env.NODE_ENV !== 'production') {
			deploymentLink =
				<a title="Go to the deployed page"
					className="deselected deployment"
					onClick={this.openDeployment.bind (this)}>Deployment</a>;
		}

		var classFor = (page) =>
			this.props.currentPage === page ? 'selected' : 'deselected';
		return <div className="Navigation">
			<div className="NavigationSection" />
			<div className="NavigationSection Center" >
				<a title="View a timeline of all benchmarks"
					className={classFor ('timeline')}
					href="index.html">Timeline</a>
				<a title="Compare the results of multiple run sets"
					className={classFor ('compare')}
					href="compare.html">Compare</a>
				<a title="View benchmark results for pull requests"
					className={classFor ('pullRequests')}
					href="pullrequests.html">Pull Requests</a>
			</div>
			<div className="NavigationSection Right" >
				{deploymentLink}
			</div>
		</div>;
	}
}

export function parseLocationHashForDict (items: Array<string>, startFunc: (keyMap: Object) => void) {
	var hash = window.location.hash.substring (1);

	if (hash.length === 0) {
		startFunc ({});
		return;
	}

	var components = hash.split ('&');
	var parsed = {};
	var error = false;
	for (var i = 0; i < components.length; ++i) {
		var kv = components [i].split ('=');
		if (kv.length != 2) {
			error = true;
			break;
		}
		if (xp_utils.findIndex (items, n => n === kv [0]) < 0) {
			alert ("Warning: Parameter " + kv [0] + " not supported.");
			continue;
		}
		parsed [kv [0]] = kv [1];
	}

	if (!error) {
		startFunc (parsed);
		return;
	}

	var ids = hash.split ('+');
	Database.fetchParseObjectIds (ids, keys => {
		var keyMap = {};
		keys.forEach ((k, i) => keyMap [items [i]] = k);
		startFunc (keyMap);
	}, error => {
		alert ("Error: " + error.toString ());
	});
}

export function setLocationForDict (dict: Object) {
	var components = [];
	var keys = Object.keys (dict);
	for (var i = 0; i < keys.length; ++i) {
		var k = keys [i];
		components.push (k + "=" + dict [k].toString ());
	}
	window.location.hash = components.join ("&");
}

export function parseLocationHashForArray (key: string, startFunc: (keyArray: Array<string | number>) => void) {
	var hash = window.location.hash.substring (1);

	if (hash.length === 0) {
		startFunc ([]);
		return;
	}

	var kv = hash.split ('=');
	if (kv.length === 2 && kv [0] === key) {
		var items = kv [1].split ('+');
		startFunc (items);
		return;
	}

	var ids = hash.split ('+');
	Database.fetchParseObjectIds (ids, startFunc,
		error => {
			alert ("Error: " + error.toString ());
		});
}

export function setLocationForArray (key: string, ids: Array<string>) {
	window.location.hash = key + "=" + ids.join ("+");
}

export function pullRequestIdFromUrl (url: string): number {
	var match = url.match (/^https?:\/\/github\.com\/mono\/mono\/pull\/(\d+)\/?$/);
	if (match === null)
		return undefined;
	return Number (match [1]);
}

export function getPullRequestInfo (url: string, success: (info: Object) => void): void {
	var id = pullRequestIdFromUrl (url);
	if (id === undefined)
		return;
	var github = new GitHub ({
		// HACK: A read-only access token to allow higher rate limits.
		token: '319339f37f8f19b7b5ba92ebfcbdb965871440e0',
		auth: 'oauth'
	});
	var repo = github.getRepo ("mono", "mono");
	repo.getPull (id, (err, info) => success (info));
}

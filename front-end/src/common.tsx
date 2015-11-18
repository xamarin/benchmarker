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
	public render () : JSX.Element {
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
			{envVars.map ((name: string, i: number) => <li key={"var" + i.toString ()}><code>{name + "=" + envVarsMap [name]}</code></li>)}
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
	public render () : JSX.Element {
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

export interface MachineConfigSelection {
	machine: Database.DBObject;
	config: Database.DBObject;
	metric: string;
}

type ConfigSelectorProps = {
	includeMetric: boolean;
	runSetCounts: Array<Database.RunSetCount>;
	featuredTimelines: Array<Database.DBObject>;
	selection: Array<MachineConfigSelection>;
	showControls: boolean;
	onChange: (selection: Array<MachineConfigSelection>) => void;
};

interface CombinedConfigEntry {
	runSetCounts: Array<Database.RunSetCount>;
	displayString?: string;
}

export class CombinedConfigSelector extends React.Component<ConfigSelectorProps, void> {
	private runSetCounts () : Array<Database.RunSetCount> {
		if (this.props.includeMetric)
			return this.props.runSetCounts;
		return Database.combineRunSetCountsAcrossMetric (this.props.runSetCounts);
	}

	public render () : JSX.Element {
		var userStringForRSC = (r: Database.RunSetCount) => {
			var s = r.machine.get ('name') + " / " + r.config.get ('name');
			if (this.props.includeMetric)
				s = s + " / " + r.metric;
			return s;
		};

		const valueStringForSingleSelection = (selection: MachineConfigSelection) => {
			if (selection.machine === undefined || selection.config === undefined) {
				console.log ("what is this?", selection);
				throw "BLA";
			}
			var s = selection.machine.get ('name') + '+' + selection.config.get ('name');
			if (this.props.includeMetric)
				s = s + '+' + selection.metric;
			return s;
		};

		const valueStringForSelection = (selection: Array<MachineConfigSelection>) => {
			if (selection.length === 0) {
				return undefined;
			}
			return selection.map (valueStringForSingleSelection).join ('+');
		};

		var sortedRunSetCounts = xp_utils.sortArrayLexicographicallyBy (
			this.runSetCounts (),
			(r: Database.RunSetCount) => userStringForRSC (r).toLowerCase ());

		type MachinesMap = { [name: string]: Array<Database.RunSetCount> };
		var machines: MachinesMap = {};
		for (var i = 0; i < sortedRunSetCounts.length; ++i) {
			const rsc = sortedRunSetCounts [i];
			const machineName = rsc.machine.get ('name');
			machines [machineName] = machines [machineName] || [];
			machines [machineName].push (rsc);
		}

		const featuredRSCs: Array<CombinedConfigEntry> = [];

		if (this.props.featuredTimelines !== undefined) {
			const ftlNames = xp_utils.uniqStringArray (this.props.featuredTimelines.map ((ftl: Database.DBObject) => ftl.get ('name')));
			ftlNames.sort ();
			ftlNames.forEach ((name: string) => {
				const ftls = this.props.featuredTimelines.filter ((ftl: Database.DBObject) => ftl.get ('name') === name);
				const rscs = ftls.map ((ftl: Database.DBObject) => {
					const machine = ftl.get ('machine');
					const config = ftl.get ('config');
					const metric = ftl.get ('metric');
					return Database.findRunSetCount (sortedRunSetCounts, machine, config, metric);
				});
				featuredRSCs.push ({ runSetCounts: rscs, displayString: name });
			});
		}

		const renderEntry = (entry: CombinedConfigEntry) => {
			const string = valueStringForSelection (entry.runSetCounts);
			let displayString = entry.displayString;
			if (displayString === undefined) {
				displayString = entry.runSetCounts [0].config.get ('name');
				if (this.props.includeMetric)
					displayString = displayString + " / " + entry.runSetCounts [0].metric;
			}
			const count = xp_utils.sum (entry.runSetCounts.map ((rsc: Database.RunSetCount) => rsc.count));
			displayString = displayString + " (" + count + ")";
			return <option
				value={string}
				key={string}>
				{displayString}
			</option>;
		};

		const renderGroup = (machinesMap: MachinesMap, name: string) => {
			const sorted = xp_utils.sortArrayNumericallyBy (
				machinesMap [name],
				(x: Database.RunSetCount) => -x.count);
			return <optgroup key={"group" + name} label={name}>
				{sorted.map ((rsc: Database.RunSetCount) => renderEntry ({ runSetCounts: [rsc] }))}
			</optgroup>;
		};

		const selectedValue = valueStringForSelection (this.props.selection);
		var aboutConfig = undefined;
		var aboutMachine = undefined;
		if (this.props.showControls) {
			aboutConfig = <button onClick={() => this.openConfigDescription ()}>About Config</button>;
			aboutMachine = <button onClick={() => this.openMachineDescription ()}>About Machine</button>;
		}
		let featuredTimelinesElement: JSX.Element;
		if (this.props.featuredTimelines !== undefined) {
			featuredTimelinesElement = <optgroup label="Featured">
				{featuredRSCs.map (renderEntry)}
			</optgroup>;
		}
		return <div className="CombinedConfigSelector">
			<label>Machine &amp; Config</label>
			<select size={6} value={selectedValue} onChange={(e: React.FormEvent) => this.combinationSelected (e)}>
				{featuredTimelinesElement}
				{Object.keys (machines).map ((m: string) => renderGroup (machines, m))}
			</select>
			{aboutConfig}{aboutMachine}
			<div style={{ clear: 'both' }}></div>
		</div>;
	}

	private openConfigDescription () : void {
		if (this.props.selection.length < 1)
			return;
		window.open ('config.html#name=' + this.props.selection [0].config.get ('name'));
	}

	private openMachineDescription () : void {
		if (this.props.selection.length < 1)
			return;
		window.open ('machine.html#name=' + this.props.selection [0].machine.get ('name'));
	}

	private combinationSelected (event: React.FormEvent) : void {
		const target: any = event.target;
		const partitions = xp_utils.partition (target.value.split ('+'), 3);
		const rscs = partitions.map ((names: Array<string>) =>
			Database.findRunSetCount (this.runSetCounts (), names [0], names [1], names [2]));
		this.props.onChange (rscs);
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

	public componentWillMount () : void {
		this.fetchData (this.props);
	}

	public componentWillReceiveProps (nextProps: RunSetSelectorProps) : void {
		function getName (obj: Database.DBObject) : string {
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

	private fetchData (props: RunSetSelectorProps) : void {
		var machine = props.selection.machine;
		var config = props.selection.config;

		if (machine === undefined || config === undefined)
			return;

		Database.fetchRunSetsForMachineAndConfig (machine, config, (runSets: Array<Database.DBRunSet>) => {
			this.setState ({ runSets: runSets });
		}, (error: Object) => {
			alert ("error loading run sets: " + error.toString ());
		});
	}

	private runSetSelected (event: React.FormEvent) : void {
		if (this.state.runSets === undefined)
			return;
		var target: any = event.target;
		var runSetId = parseInt (target.value);
		var runSet = Database.findRunSet (this.state.runSets, runSetId);
		if (runSet !== undefined)
			this.props.onChange ({machine: runSet.machine, config: runSet.config, runSet: runSet});
	}

	private configsSelected (selection: Array<MachineConfigSelection>) : void {
		if (selection.length !== 1) {
			console.log ("Error: more than one config selected in RunSetSelector");
			return;
		}
		this.props.onChange ({machine: selection [0].machine, config: selection [0].config, runSet: undefined});
	}

	public render () : JSX.Element {
		var selection = this.props.selection;
		var machine = selection.machine;
		var config = selection.config;
		var runSets = this.state.runSets;

		var runSetId = undefined;

		if (selection.runSet !== undefined)
			runSetId = selection.runSet.get ('id');

		const openRunSetDescription = (id: number) => {
			return window.open ('runset.html#id=' + id);
		};

		const renderRunSet = (runSet: Database.DBRunSet) => {
			const id = runSet.get ('id');
			return <option value={id} key={id} onDoubleClick={() => openRunSetDescription (id)}>
				{xp_utils.formatDate (runSet.commit.get ('commitDate'))} - {runSet.commit.get ('hash').substring (0, 10)}
			</option>;
		};

		const machineConfigSelections = (machine !== undefined && config !== undefined)
			? [{ machine: selection.machine, config: selection.config, metric: undefined}]
			: [];
		var configSelector =
			<CombinedConfigSelector
				featuredTimelines={undefined}
				selection={ machineConfigSelections }
				includeMetric={false}
				runSetCounts={this.props.runSetCounts}
				onChange={(s: Array<MachineConfigSelection>) => this.configsSelected (s)}
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
				onChange={(e: React.FormEvent) => this.runSetSelected (e)}>
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
	commitInfo: Object;
};

export class RunSetDescription extends React.Component<RunSetDescriptionProps, RunSetDescriptionState> {
	constructor (props: RunSetDescriptionProps) {
		super (props);
		this.state = { results: undefined, secondaryCommits: undefined, commitInfo: undefined };
		this.fetchResults (props.runSet);
	}

	private fetchResults (runSet: Database.DBRunSet) : void {
		Database.fetch ('results?runset=eq.' + runSet.get ('id'),
			(objs: Array<Object>) => {
				if (runSet !== this.props.runSet)
					return;
				this.setState ({ results: objs } as any);
			}, (error: Object) => {
				alert ("error loading results: " + error.toString ());
			});
		var secondaryCommits = runSet.get ('secondaryCommits');
		if (secondaryCommits !== undefined && secondaryCommits.length > 0) {
			Database.fetch ('commit?hash=in.' + secondaryCommits.join (','),
				(objs: Array<Object>) => {
					if (runSet !== this.props.runSet)
						return;
					this.setState ({ secondaryCommits: objs } as any);
				}, (error: Object) => {
					alert ("error loading commits: " + error.toString ());
				});
		}

		getCommitInfo (runSet.get ('commit'), (info: Object) => {
			this.setState ({ commitInfo: info } as any);
			console.log (info);
		});
	}

	public componentWillReceiveProps (nextProps: RunSetDescriptionProps) : void {
		this.setState ({ results: undefined, secondaryCommits: undefined, commitInfo: undefined });
		this.fetchResults (nextProps.runSet);
	}

	public render () : JSX.Element  {
		var runSet = this.props.runSet;
		var buildURL = runSet.get ('buildURL');
		var buildLink: JSX.Element;
		var logURLs = runSet.get ('logURLs');
		var logLinks = [];
		var logLinkList: JSX.Element;
		var table: JSX.Element;
		var secondaryProductsList: Array<JSX.Element>;
		var crashedElem: JSX.Element;
		var timedOutElem: JSX.Element;

		if (buildURL !== undefined)
			buildLink = <a href={buildURL}>build</a>;

		if (logURLs !== undefined && Object.keys (logURLs).length !== 0) {
			for (var key of Object.keys (logURLs)) {
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

		var secondaryCommits = runSet.get ('secondaryCommits') as Array<string>;
		if (secondaryCommits !== undefined && secondaryCommits.length > 0) {
			var elements;
			if (this.state.secondaryCommits === undefined) {
				elements = secondaryCommits.map ((c: string) => {
					var short = c.substring (0, 10);
					return <li key={"commit" + c}>{short}</li>;
				});
			} else {
				elements = this.state.secondaryCommits.map ((c: string) => {
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
				let result = this.state.results [i];
				var benchmark = result ['benchmark'];
				var metric = result ['metric'];
				var entry = resultsByBenchmark [benchmark] || { metrics: {}, disabled: result ['disabled'] };
				entry.metrics [metric] = result ['results'];
				resultsByBenchmark [benchmark] = entry;

				metricsDict [metric] = {};
			}
			var crashedBenchmarks = (runSet.get ('crashedBenchmarks') || []) as Array<string>;
			var timedOutBenchmarks = (runSet.get ('timedOutBenchmarks') || []) as Array<string>;
			var reportedCrashed: {[name: string]: boolean} = {};
			var reportedTimedOut: {[name: string]: boolean} = {};
			var benchmarkNames = Object.keys (resultsByBenchmark);
			benchmarkNames.sort ();
			var metrics = Object.keys (metricsDict);
			metrics.sort ();
			var tableHeaders = [];
			metrics.forEach ((m: string) => {
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
				{benchmarkNames.map ((name: string) => {
					var result = resultsByBenchmark [name];
					var disabled = result.disabled;
					var statusIcons = [];
					if (crashedBenchmarks.indexOf (name) !== -1) {
						statusIcons.push (<span key="crashed" className="statusIcon crashed fa fa-exclamation-circle" title="Crashed"></span>);
						reportedCrashed [name] = true;
					}
					if (timedOutBenchmarks.indexOf (name) !== -1) {
						statusIcons.push (<span key="timedOut" className="statusIcon timedOut fa fa-clock-o" title="Timed Out"></span>);
						reportedTimedOut [name] = true;
					}
					if (statusIcons.length === 0)
						statusIcons.push (<span key="good" className="statusIcon good fa fa-check" title="Good"></span>);

					var metricColumns = [];
					metrics.forEach ((m: string) => {
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

			var notReportedCrashed = crashedBenchmarks.filter ((n: string) => !reportedCrashed [n]);
			var notReportedTimedOut = timedOutBenchmarks.filter ((n: string) => !reportedTimedOut [n]);

			if (notReportedCrashed.length > 0) {
				crashedElem = <p>All crashed: {notReportedCrashed.join (", ")}</p>;
			}
			if (notReportedTimedOut.length > 0) {
				timedOutElem = <p>All timed out: {notReportedTimedOut.join (", ")}</p>;
			}
		}

		const product = runSet.commit ? runSet.commit.get ('product') : 'mono';
		const commitHash = runSet.get ('commit');
		const commitLink = githubCommitLink (product, commitHash);
		let commitName = undefined;
		let commitInfo: JSX.Element = undefined;

		if (this.state.commitInfo !== undefined) {
			commitName = this.state.commitInfo ['message'];
			commitInfo = <p>Authored by {this.state.commitInfo ['author']['name']}.</p>;
		} else {
			commitName = commitHash.substring (0, 10);
		}

		const commitElement = <a href={commitLink}>{commitName}</a>;
		return <div className="Description">
			<h1 key="commit">{commitElement} ({buildLink})</h1>
			{commitInfo}
			{logLinkList}
			{secondaryProductsList}
			{crashedElem}
			{timedOutElem}
			{table}
		</div>;
	}
}

export function githubCommitLink (product: string, commit: string) : string {
	var repo = "";
	switch (product) {
		case 'mono':
			repo = 'mono/mono';
			break;
		case 'monodroid':
			repo = 'xamarin/monodroid';
			break;
		case 'benchmarker':
			repo = 'xamarin/benchmarker';
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

interface NavigationProps {
	currentPage: string;
	comparisonRunSetIds?: Array<number>;
}

export class Navigation extends React.Component<NavigationProps, void> {
	private openDeployment () : boolean {
		var lastSlashIndex = window.location.href.search ("/[^/]+$");
		var path = window.location.href.substring (lastSlashIndex + 1);
		var deploymentLink = "http://xamarin.github.io/benchmarker/front-end/" + path;
		window.open (deploymentLink);
		return false;
	}

	public render () : JSX.Element {
		var deploymentLink;
		if (process.env.NODE_ENV !== 'production') {
			deploymentLink =
				<a title="Go to the deployed page"
					className="deselected deployment"
					onClick={() => this.openDeployment ()}>Deployment</a>;
		}

		var classFor = (page: string) =>
			this.props.currentPage === page ? 'selected' : 'deselected';
		var compareLink = "compare.html";
		if (this.props.comparisonRunSetIds !== undefined) {
			compareLink = compareLink + "#ids=" + this.props.comparisonRunSetIds.join ("+");
		}
		return <div className="Navigation">
			<div className="NavigationSection" />
			<div className="NavigationSection Center" >
				<a title="View a timeline of all benchmarks"
					className={classFor ('timeline')}
					href="index.html">Timeline</a>
				<a title="Compare the results of multiple run sets"
					className={classFor ('compare')}
					href={compareLink}>Compare</a>
				<a title="View benchmark results for pull requests"
					className={classFor ('pullRequests')}
					href="pullrequests.html">Pull Requests</a>
				<a title="View GC pause times"
					className={classFor ('pauseTimes')}
					href="pausetimes.html">Pause Times</a>
			</div>
			<div className="NavigationSection Right" >
				{deploymentLink}
			</div>
		</div>;
	}
}

export function parseLocationHashForDict (items: Array<string>, startFunc: (keyMap: Object) => void) : void {
	var hash = window.location.hash.substring (1);

	if (hash.length === 0) {
		startFunc ({});
		return;
	}

	var components = hash.split ('&');
	var parsed = {};
	var error = false;
	for (let i = 0; i < components.length; ++i) {
		var kv = components [i].split ('=');
		if (kv.length !== 2) {
			error = true;
			break;
		}
		if (xp_utils.findIndex (items, (n: string) => n === kv [0]) < 0) {
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
	Database.fetchParseObjectIds (ids, (keys: Array<number | string>) => {
		var keyMap = {};
		keys.forEach ((k: number | string, i: number) => keyMap [items [i]] = k);
		startFunc (keyMap);
	}, (err: Object) => {
		alert ("Error: " + err.toString ());
	});
}

export function setLocationForDict (dict: Object) : void {
	var components = [];
	var keys = Object.keys (dict);
	for (var i = 0; i < keys.length; ++i) {
		var k = keys [i];
		components.push (k + "=" + dict [k].toString ());
	}
	window.location.hash = components.join ("&");
}

export function parseLocationHashForArray (key: string, startFunc: (keyArray: Array<string | number>) => void) : void {
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
		(error: Object) => {
			alert ("Error: " + error.toString ());
		});
}

export function setLocationForArray (key: string, ids: Array<string>) : void {
	window.location.hash = key + "=" + ids.join ("+");
}

export function pullRequestIdFromUrl (url: string) : number {
	var match = url.match (/^https?:\/\/github\.com\/mono\/mono\/pull\/(\d+)\/?$/);
	if (match === null)
		return undefined;
	return Number (match [1]);
}

function getMonoRepo () : Repo {
	const github = new GitHub ({
		// HACK: A read-only access token to allow higher rate limits.
		token: '319339f37f8f19b7b5ba92ebfcbdb965871440e0',
		auth: 'oauth'
	});
	return github.getRepo ("mono", "mono");
}

export function getPullRequestInfo (url: string, success: (info: Object) => void) : void {
	const id = pullRequestIdFromUrl (url);
	if (id === undefined)
		return;
	const repo = getMonoRepo ();
	repo.getPull (id, (err: Object, info: Object) => {
		if (info) {
			success (info);
		}
	});
}

function getCommitInfo (hash: string, success: (info: Object) => void) : void {
	const repo = getMonoRepo ();
	repo.getCommit (undefined, hash, (err: Object, info: Object) => {
		if (info) {
			success (info);
		}
	});
}

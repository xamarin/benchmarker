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
	"asphalt": [ "#889DB5", "#66819E", "#365271", "#2B3E50", "#1C2B39" ],
};
export var xamarinColorsOrder = [ "blue", "green", "violet", "red", "asphalt", "amber", "gray", "teal" ];

export enum DescriptionFormat {
    Compact,
    Full
};

type ConfigDescriptionProps = {
	config: Database.DBObject;
	format: DescriptionFormat;
};

export class ConfigDescription extends React.Component<ConfigDescriptionProps, void> {
	public render () : JSX.Element {
		var config = this.props.config;

		if (config === undefined)
			return <div></div>;

		var link = <a href={"config.html#name=" + config.get ('name')}>{config.get ('name')}</a>;
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

        if (this.props.format === DescriptionFormat.Compact)
            return link;

		return <div className="Description">
			<p>{link}</p>
			<dl>
				<dt>Environment Variables</dt><dd>{envVarsList}</dd>
				<dt>Mono Executable</dt><dd>{monoExecutable}</dd>
				<dt>Command-line Options</dt><dd>{optionsList}</dd>
			</dl>
		</div>;
	}
}

type MachineDescriptionProps = {
	machine: Database.DBObject;
	format: DescriptionFormat;
};

export class MachineDescription extends React.Component<MachineDescriptionProps, void> {
	public render () : JSX.Element {
		var machine = this.props.machine;

		if (machine === undefined)
			return <div></div>;
		var link = <a href={"machine.html#name=" + machine.get ('name')}>{machine.get ('name')}</a>;
		if (this.props.format === DescriptionFormat.Compact)
			return link;
		return <div className="Description">
			<p>
				{link}
				{' '}
				({machine.get ('isDedicated') ? "dedicated" : "non-dedicated"}
				{' '}
				{machine.get ('architecture')})
			</p>
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
			if (selection.machine === undefined || selection.config === undefined)
				return '';
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
		var aboutConfig;
		var aboutMachine;
		var aboutMachineConfig;
		if (this.props.selection.length !== 0) {
			aboutConfig = <ConfigDescription
				config={this.props.selection [0].config}
				format={DescriptionFormat.Compact} />;
			aboutMachine = <MachineDescription
				machine={this.props.selection [0].machine}
				format={DescriptionFormat.Compact} />;
			aboutMachineConfig = <p>{aboutConfig}@{aboutMachine}</p>;
		}
		let featuredTimelinesElement: JSX.Element;
		if (this.props.featuredTimelines !== undefined) {
			featuredTimelinesElement = <optgroup label="Featured">
				{featuredRSCs.map (renderEntry)}
			</optgroup>;
		}
		return <div className="CombinedConfigSelector">
			<label>Machine &amp; Config</label>
			<select size={10} value={selectedValue} onChange={(e: React.FormEvent) => this.combinationSelected (e)}>
				{featuredTimelinesElement}
				{Object.keys (machines).map ((m: string) => renderGroup (machines, m))}
			</select>
			{aboutMachineConfig}
			<div style={{ clear: 'both' }}></div>
		</div>;
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
	runSets: Array<Database.DBRunSet>;
}

type RunSetSelectorProps = {
	runSetCounts: Array<Database.RunSetCount>;
	selection: RunSetSelection;
	onChange: (selection: RunSetSelection) => void;
	multiple: boolean;
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
		var options: any = (event.target as HTMLSelectElement).options;
		var runSetIds = [];
		for (var i = 0; i < options.length; ++i)
			if (options [i].selected)
				runSetIds.push (parseInt (options [i].value));
		var runSets = runSetIds.map ((runSetId: number) => Database.findRunSet (this.state.runSets, runSetId));
		if (runSets !== undefined && runSets.length !== 0)
			this.props.onChange ({machine: runSets [0].machine, config: runSets [0].config, runSets: runSets});
	}

	private configsSelected (selection: Array<MachineConfigSelection>) : void {
		if (selection.length !== 1)
			return;
		this.props.onChange ({machine: selection [0].machine, config: selection [0].config, runSets: []});
	}

	public render () : JSX.Element {
		var selection = this.props.selection;
		var machine = selection.machine;
		var config = selection.config;
		var runSets = this.state.runSets;

		var runSetIds = undefined;

		if (selection.runSets !== undefined)
			runSetIds = selection.runSets.map ((rs: Database.DBRunSet) => rs.get ('id'));

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
				onChange={(s: Array<MachineConfigSelection>) => this.configsSelected (s)} />;
		var runSetsSelect = undefined;
		if (runSets === undefined && machine !== undefined && config !== undefined) {
			runSetsSelect = <div className="diagnostic">Loading&hellip;</div>;
		} else if (runSets === undefined) {
			runSetsSelect = <div className="diagnostic">Please select a machine and config.</div>;
		} else if (runSets.length === 0) {
			runSetsSelect = <div className="diagnostic">No run sets found for this machine and config.</div>;
		} else {
			runSetsSelect = <select
				multiple={this.props.multiple}
				size={10}
				value={runSetIds}
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
		});
	}

	public componentWillReceiveProps (nextProps: RunSetDescriptionProps) : void {
		this.setState ({ results: undefined, secondaryCommits: undefined, commitInfo: undefined });
		this.fetchResults (nextProps.runSet);
	}

	public render () : JSX.Element  {
		var runSet = this.props.runSet;
		var buildURL = runSet.get ('buildURL');
		var buildIcon: JSX.Element;
		var logURLs = runSet.get ('logURLs');
		var logLinks = [];
		var logLinkList: JSX.Element;
		var table: JSX.Element;
		var secondaryProductsList: Array<JSX.Element>;
		var crashedElem: JSX.Element;
		var timedOutElem: JSX.Element;

		if (buildURL !== undefined)
			buildIcon = makeBuildIcon (buildURL);

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
			secondaryProductsList = [
				<h1 key="secondaryProductsHeader">Secondary products</h1>,
				<ul key="secondaryProductsList" className='secondaryProducts'>{elements}</ul>,
			];
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
			var benchmarkNames = xp_utils.uniqStringArray
				(Object.keys (resultsByBenchmark).concat (crashedBenchmarks).concat (timedOutBenchmarks));
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

					var statusIcons = [];
					if (crashedBenchmarks.indexOf (name) !== -1)
						statusIcons.push (<span key="crashed" className="statusIcon crashed fa fa-exclamation-circle" title="Crashed"></span>);
					if (timedOutBenchmarks.indexOf (name) !== -1)
						statusIcons.push (<span key="timedOut" className="statusIcon timedOut fa fa-clock-o" title="Timed Out"></span>);
					if (statusIcons.length === 0)
						statusIcons.push (<span key="good" className="statusIcon good fa fa-check" title="Good"></span>);

					if (result === undefined) {
						return <tr key={"benchmark" + name} className="broken">
							<td key="name"><code>{name}</code></td>
							<td key="status" className="statusColumn">{statusIcons}</td>
							<td colSpan={metrics.length * 2} className="diagnostic">All runs in this run set timed out or crashed.</td>
						</tr>;
					}

					var disabled = result.disabled;

					var metricColumns = [];
					metrics.forEach ((m: string) => {
						var dataPoints = result.metrics [m] || [];
						dataPoints.sort ();
						var dataPointsString = dataPoints.join (", ");
						var variance = Outliers.outlierVariance (dataPoints);
						var degree = variance < 0.01 ? 'none'
							: variance < 0.10 ? 'slight'
							: variance < 0.50 ? 'moderate'
							: 'severe';
						metricColumns.push (<td key={"metricValues" + m}>{dataPointsString}</td>);
						metricColumns.push (<td key={"metricDegree" + m}>
								<div className="degree" title={degree}>
									<div className={degree}>&nbsp;</div>
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

		const product = runSet.commit ? runSet.commit.get ('product') : 'mono';
		const commitHash = runSet.get ('commit');
		const commitLink = githubCommitLink (product, commitHash);
		let commitName = undefined;
		let commitInfo: JSX.Element = undefined;

		if (this.state.commitInfo !== undefined) {
			var info = this.state.commitInfo;
			commitName = info ['message'];
			var newlineIndex = commitName.indexOf ('\n');
			if (newlineIndex >= 0)
				commitName = commitName.substring (0, newlineIndex);
			if (info ['author'] ['name'] === info ['committer'] ['name']) {
				commitInfo = <p>Authored by {info ['author'] ['name']}.</p>;
			} else {
				commitInfo = <p>Authored by {info ['author'] ['name']}, committed by {info ['committer'] ['name']}.</p>;
			}
		} else {
			commitName = commitHash.substring (0, 10);
		}

		const commitElement = <p><strong><a href={commitLink}>{commitName}</a></strong> {buildIcon}</p>;
		return <div className="Description">
			{commitElement}
			{commitInfo}
			{logLinkList}
			{secondaryProductsList}
			{table}
		</div>;
	}
}

export interface RunSetSummaryProps extends React.Props<RunSetSummary> {
	runSet: Database.DBRunSet;
	previousRunSet: Database.DBRunSet;
}

export class RunSetSummary extends React.Component<RunSetSummaryProps, void> {
	public render () : JSX.Element {
		var runSet = this.props.runSet;
		var commitHash = runSet.commit.get ('hash');
		var commitLink = githubCommitLink (runSet.commit.get ('product'), commitHash);

		var prev = this.props.previousRunSet;
		var prevItems;
		if (prev !== undefined) {
			var prevHash = prev.commit.get ('hash');
			var prevLink = githubCommitLink (prev.commit.get ('product'), prevHash);
			var compareLink = githubCompareLink (prevHash, commitHash);
			prevItems = [
				<dt key="previousName">Previous</dt>,
				<dd key="previousValue"><a href={prevLink}>{prevHash.substring (0, 10)}</a><br /><a href={compareLink}>Compare</a></dd>,
			];
		}

		var runSetLink = "runset.html#id=" + runSet.get ('id');
		return <div className="RunSetSummary">
			<div className="Description">
			<dl>
			<dt>Commit</dt>
			<dd><a href={commitLink}>{commitHash.substring (0, 10)}</a><br /><a href={runSetLink}>Details</a></dd>
			{prevItems}
			</dl>
			</div>
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
		var timelineLink = "index.html";
		if (this.props.comparisonRunSetIds !== undefined
			&& this.props.comparisonRunSetIds.length !== 0) {
			var selectionIds = this.props.comparisonRunSetIds.join ("+");
			compareLink = compareLink + "#ids=" + selectionIds;

			// FIXME: This would be nice to preserve, but if we do, then the
			// machine/config selection should also be preserved.

			// timelineLink = timelineLink + "#selection=" + selectionIds;
		}
		return <div className="Navigation">
			<div className="NavigationSection" />
			<div className="NavigationSection Center" >
				<a title="View a timeline of all benchmarks"
					className={classFor ('timeline')}
					href={timelineLink}>Timeline</a>
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

/*
 * "" => []
 * "a" => ["a"]
 * "a+" => ["a"]
 * "a+b" => ["a", "b"]
 */
export function splitLocationHashValues (values: string) : Array<string> {
	return values.split ('+').filter ((item: string) => item !== '');
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

	var ids = splitLocationHashValues (hash);
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
		if (dict [k] !== undefined)
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
		startFunc (splitLocationHashValues (kv [1]));
		return;
	}

	var ids = splitLocationHashValues (hash);
	if (ids.length === 0) {
		startFunc ([]);
		return;
	}

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
		auth: 'oauth',
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

export function relativeDate (then: Date) : JSX.Element {
	var now = new Date ();
	var ago = (now.getTime () - then.getTime ()) / 1000;
	var text;
	var s_m = 60;
	var s_h = s_m * 60;
	var s_d = s_h * 24;
	var s_wk = s_d * 7;
	var s_mo = s_wk * 4;
	var s_y = s_mo * 12;
	function agoify (x: number, unit: string) : string {
		x = Math.floor (x);
		unit = x === 1 ? unit : unit + 's';
		return [x, unit, 'ago'].join (' ');
	}
	if (ago < 0) {
		text = 'in the future';
	} else if (ago < s_m) {
		text = agoify (ago, 'second');
	} else if (ago < s_h) {
		text = agoify (ago / s_m, 'minute');
	} else if (ago < s_d) {
		text = agoify (ago / s_h, 'hour');
	} else if (ago < s_wk) {
		text = agoify (ago / s_d, 'day');
	} else if (ago < s_mo) {
		text = agoify (ago / s_wk, 'week');
	} else if (ago < s_y) {
		text = agoify (ago / s_mo, 'month');
	} else {
		text = then.toString ();
	}
	return <span className="pre" title={then.toString ()}>{text}</span>;
}

export function makeBuildIcon (url: string) : JSX.Element {
	return <a href={url} className="buildIcon fa fa-cogs" title="Build Logs"></a>;
}

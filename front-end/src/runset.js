/* @flow */

"use strict";

import * as xp_utils from './utils.js';
import * as xp_common from './common.js';
import {Parse} from 'parse';
import React from 'react';

class Controller extends xp_common.Controller {

	startupRunSetId: string | void;

	constructor (startupRunSetId) {
		super ();
		this.startupRunSetId = startupRunSetId;
	}

	allDataLoaded () {
		var selection;
		if (this.startupRunSetId === undefined) {
			selection = {};
		} else {
			var runSet = this.runSetForId (this.startupRunSetId);
			var machine = this.machineForId (runSet.get ('machine').id);
			selection = {machine: machine, config: runSet.get ('config'), runSet: runSet};
		}

		React.render (
			React.createElement (
				Page,
				{
					controller: this,
					initialSelection: selection,
					onChange: this.updateForRunSet.bind (this)
				}
			),
			document.getElementById ('runSetPage')
		);

		this.updateForRunSet (selection);
	}

	updateForRunSet (selection) {
		var runSet = selection.runSet;
		if (runSet === undefined)
			return;
		window.location.hash = runSet.id;
	}
}

class Page extends React.Component {
	constructor (props) {
		super (props);
		this.state = {selection: this.props.initialSelection};
	}

	setState (newState) {
		super.setState (newState);
		this.props.onChange (newState.selection);
	}

	handleChange (newSelection) {
		this.setState ({selection: newSelection});
	}

	render () {
		var detail;
		if (this.state.selection.runSet === undefined)
			detail = <div className='diagnostic'>Please select a run set.</div>;
		else
			detail = <RunSetDescription controller={this.props.controller} runSet={this.state.selection.runSet} />;

		return <div className="RunSetPage">
			<header>
				<xp_common.Navigation currentPage="" />
			</header>
			<article>
				<div className="panel">
					<xp_common.RunSetSelector
						controller={this.props.controller}
						selection={this.state.selection}
						onChange={this.handleChange.bind (this)} />
				</div>
				{detail}
			</article>
		</div>;
	}
}

class RunSetDescription extends React.Component {
	constructor (props) {
		super (props);
		this.invalidateState (props.runSet);
	}

	invalidateState (runSet) {
		this.state = {};

		xp_common.pageParseQuery (
			() => {
				var query = new Parse.Query (xp_common.Run);
				query.equalTo ('runSet', runSet);
				return query;
			},
			results => {
				if (runSet !== this.props.runSet)
					return;
				this.setState ({runs: results});
			},
			function (error) {
				alert ("error loading runs: " + error.toString ());
			});
	}

	componentWillReceiveProps (nextProps) {
		this.invalidateState (nextProps.runSet);
	}

	render () {
		var runSet = this.props.runSet;
		var buildURL = runSet.get ('buildURL');
		var buildLink;
		var logURLs = runSet.get ('logURLs');
		var logLinks = [];
		var logLinkList;
		var timedOutBenchmarks;
		var crashedBenchmarks;
		var table;

		if (buildURL !== undefined)
			buildLink = <a href={buildURL}>build</a>;

		if (logURLs !== undefined && Object.keys (logURLs).length !== 0) {
			for (var key in logURLs) {
				var url = logURLs [key];
				var anchor = document.createElement ('a');
				anchor.href = url;
				
				var shortUrl = <span>{anchor.hostname}/&hellip;{anchor.pathname.substring (anchor.pathname.lastIndexOf ('/'))}</span>;
				logLinks.push(<li><a href={url}><code>{key}</code> ({shortUrl})</a></li>);
			}
			if (logLinks.length === 0) {
				logLinkList = undefined;
			} else {
				logLinkList = <ul>{logLinks}</ul>;
			}
		}

		if (this.state.runs === undefined) {
			table = <div className='DiagnosticBlock'>Loading run data&hellip;</div>;
		} else {
			var runsByBenchmarkName = xp_utils.partitionArrayByString (this.state.runs, r => this.props.controller.benchmarkNameForId (r.get ('benchmark').id));
			var crashedBenchmarkIds = runSet.get ('crashedBenchmarks').map (b => b.id);
			var timedOutBenchmarkIds = runSet.get ('timedOutBenchmarks').map (b => b.id);
			var benchmarkNames = Object.keys (runsByBenchmarkName);
			benchmarkNames.sort ();
			table = <table>
				<tr>
					<th>Benchmark</th>
					<th>Status</th>
					<th>Elapsed Times (ms)</th>
					<th>Bias due to Outliers</th>
				</tr>
				{benchmarkNames.map (name => {
					var runs = runsByBenchmarkName [name];
					var benchmarkId = runs [0].get ('benchmark').id;
					var benchmark = this.props.controller.benchmarkForId (benchmarkId);
					var elapsed = runs.map (r => r.get ('elapsedMilliseconds'));
					elapsed.sort ();
					var elapsedString = elapsed.join (", ");
					var outlierVariance = xp_common.outlierVariance (elapsed);
					var statusIcons = [];
					if (crashedBenchmarkIds.indexOf (benchmarkId) !== -1)
						statusIcons.push (<span className="statusIcon crashed fa fa-exclamation-circle" title="Crashed"></span>);
					if (timedOutBenchmarkIds.indexOf (benchmarkId) !== -1)
						statusIcons.push (<span className="statusIcon timedOut fa fa-clock-o" title="Timed Out"></span>);
					if (statusIcons.length === 0)
						statusIcons.push (<span className="statusIcon good fa fa-check" title="Good"></span>);

					return <tr className={benchmark.get ('disabled') ? 'disabled' : ''}>
						<td><code>{name}</code>{benchmark.get ('disabled') ? ' (disabled)' : ''}</td>
						<td className="statusColumn">{statusIcons}</td>
						<td>{elapsedString}</td>
						<td>
							<div className="degree" title={outlierVariance}>
								<div className={outlierVariance}>&nbsp;</div>
							</div>
						</td>
					</tr>;
				})}
			</table>;
		}

		var commitHash = runSet.get ('commit').get ('hash');
		var commitLink = xp_common.githubCommitLink (commitHash);

		return <div className="Description">
			<h1><a href={commitLink}>{commitHash.substring (0, 10)}</a> ({buildLink}, <a href={'compare.html#' + runSet.id}>compare</a>)</h1>
			{logLinkList}
			{table}
		</div>;
	}
}

function started () {
	var startupRunSetId;
	if (window.location.hash)
		startupRunSetId = window.location.hash.substring (1);
	var controller = new Controller (startupRunSetId);
	controller.loadAsync ();
}

xp_common.start (started);

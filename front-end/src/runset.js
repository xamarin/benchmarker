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

		return <div>
			<xp_common.RunSetSelector
		controller={this.props.controller}
		selection={this.state.selection}
		onChange={this.handleChange.bind (this)} />
			{detail}
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
		var logLinks;
		var timedOutBenchmarks;
		var crashedBenchmarks;
		var table;

		if (buildURL !== undefined)
			buildLink = [<dt>Build</dt>, <dd><a href={buildURL}>Link</a></dd>];

		if (logURLs !== undefined && Object.keys (logURLs).length !== 0) {
			logLinks = [<dt>Logs</dt>];
			for (var key in logURLs) {
				var url = logURLs[key];
				logLinks.push(<dd>{key}: <a href={url}>{url}</a></dd>);
			}
		}

		var timedOutString = xp_common.joinBenchmarkNames (this.props.controller, runSet.get ('timedOutBenchmarks'), "");
		if (timedOutString !== "")
			timedOutBenchmarks = [<dt>Timed out</dt>, <dd>{timedOutString}</dd>];

		var crashedString = xp_common.joinBenchmarkNames (this.props.controller, runSet.get ('crashedBenchmarks'), "");
		if (crashedString !== "")
			crashedBenchmarks = [<dt>Crashed</dt>, <dd>{crashedString}</dd>];

		if (this.state.runs === undefined) {
			table = <div className='diagnostic'>Loading&hellip;</div>;
		} else {
			var runsByBenchmarkName = xp_utils.partitionArrayByString (this.state.runs, r => this.props.controller.benchmarkNameForId (r.get ('benchmark').id));
			var benchmarkNames = Object.keys (runsByBenchmarkName);
			benchmarkNames.sort ();
			table = <table>
				{benchmarkNames.map (name => {
					var runs = runsByBenchmarkName [name];
					var benchmark = this.props.controller.benchmarkForId (runs [0].get ('benchmark').id);
					var disabled = "";
					if (benchmark.get ('disabled'))
						disabled = " (disabled)";
					var elapsed = runs.map (r => r.get ('elapsedMilliseconds'));
					elapsed.sort ();
					var elapsedString = elapsed.join (", ");
					return <tr><td>{name + disabled}</td><td>{elapsedString}</td></tr>;
				})}
			</table>;
		}

		var commitHash = runSet.get ('commit').get ('hash');

		return <div className="Description">
			<p><a href={"index.html#" + runSet.id}>Compare</a></p>
			<dl>
				<dt>Commit</dt>
				<dd><a href={xp_common.githubCommitLink (commitHash)}>{commitHash}</a></dd>
				{buildLink}
				{logLinks}
				{timedOutBenchmarks}
				{crashedBenchmarks}
				<dt>Elapsed Times</dt>
				<dd>{table}</dd>
			</dl>
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

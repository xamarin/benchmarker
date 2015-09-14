/* @flow */

"use strict";

import * as xp_utils from './utils.js';
import * as xp_common from './common.js';
import * as xp_charts from './charts.js';
import * as Database from './database.js';
import React from 'react';

class Controller {
	startupRunSetIds: Array<string> | void;
	runSetCounts: Array<Object> | void;
	runSets: Array<Database.DBRunSet> | void;

	constructor (startupRunSetIds) {
		this.startupRunSetIds = startupRunSetIds;
	}

	loadAsync () {
		Database.fetchRunSetCounts (runSetCounts => {
			this.runSetCounts = runSetCounts;
			this.checkAllDataLoaded ();
		}, error => {
			alert ("error loading run set counts: " + error.toString ());
		});

		if (this.startupRunSetIds !== undefined) {
			Database.fetchRunSets (this.startupRunSetIds,
				runSets => {
					this.runSets = runSets;
					this.checkAllDataLoaded ();
				}, error => {
					alert ("error loading run sets: " + error.toString ());
				});
		}
	}

	checkAllDataLoaded () {
		if (this.runSetCounts === undefined)
			return;
		if (this.startupRunSetIds !== undefined && this.runSets === undefined)
			return;
		this.allDataLoaded ();
	}

	allDataLoaded () {
		var runSets = this.runSets;
		if (runSets === undefined)
			runSets = [];

		React.render (
			React.createElement (
				Page,
				{
					controller: this,
					initialRunSets: runSets,
					runSetCounts: this.runSetCounts,
					onChange: this.updateForSelection.bind (this)
				}
			),
			document.getElementById ('comparePage')
		);

		this.updateForSelection (runSets);
	}

	updateForSelection (runSets) {
		window.location.hash = xp_common.hashForRunSets (runSets);
	}
}

function runSetsFromSelections (selections) {
	return selections.map (s => s.runSet).filter (rs => rs !== undefined);
}

class Page extends React.Component {
	constructor (props) {
		super (props);
		var selections = props.initialRunSets.map (rs => { return { runSet: rs, machine: rs.machine, config: rs.config }; }).concat ([{}]);
		this.state = { selections: selections };
	}

	setState (newState) {
		super.setState (newState);
		this.props.onChange (runSetsFromSelections (newState.selections));
	}

	render () {
		var runSets = runSetsFromSelections (this.state.selections);
		runSets = xp_utils.uniqArrayByString (runSets, rs => rs.get ('id').toString ());

		var chart;
		if (runSets.length > 1) {
			chart = <xp_charts.ComparisonAMChart
				graphName="comparisonChart"
				controller={this.props.controller}
				runSets={runSets} />;
		} else {
			chart = <div className="DiagnosticBlock">Please select at least two run sets.</div>;
		}

		return <div className="ComparePage">
			<header>
				<xp_common.Navigation currentPage="compare" />
			</header>
			<article>
				<RunSetSelectorList
					runSetCounts={this.props.runSetCounts}
					selections={this.state.selections}
					onChange={this.setState.bind (this)} />
				{chart}
				<div style={{ clear: 'both' }}></div>
			</article>
		</div>;
	}
}

class RunSetSelectorList extends React.Component {
	handleChange (computeNewSelections) {
		var selections = computeNewSelections (this.props.selections);
		this.props.onChange ({ selections: selections });
	}

	changeSelector (index, newSelection) {
		var selections = xp_utils.updateArray (this.props.selections, index, newSelection);
		this.props.onChange ({ selections: selections });
	}

	addSelector () {
		var selections = this.props.selections.concat ([{}]);
		this.props.onChange ({ selections: selections });
	}

	removeSelector (i) {
		var selections = xp_utils.removeArrayElement (this.props.selections, i);
		this.props.onChange ({ selections: selections });
	}

	render () {
		function renderSelector (selection, index) {
			var runSet = selection.runSet;
			var machine = selection.machine;
			var config = selection.config;
			return <section>
				<xp_common.RunSetSelector
					runSetCounts={this.props.runSetCounts}
					selection={{runSet: runSet, machine: machine, config: config}}
					onChange={this.changeSelector.bind (this, index)} />
				<button onClick={this.removeSelector.bind (this, index)}>&minus;&ensp;Remove</button>
				<div style={{ clear: 'both' }}></div>
			</section>;
		}
		return <div className="RunSetSelectorList">
			{this.props.selections.map (renderSelector.bind (this))}
			<footer><button onClick={this.addSelector.bind (this)}>+&ensp;Add Run Set</button></footer>
			</div>;
	}
}

function started () {
	var startupRunSetIds;
	if (window.location.hash)
		startupRunSetIds = window.location.hash.substring (1).split ('+').map (s => parseInt (s));
	var controller = new Controller (startupRunSetIds);
	controller.loadAsync ();
}

xp_common.start (started);

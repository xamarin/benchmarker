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
	runSetEntries: Array<Object> | void;

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
			Database.fetchRunSets (runSetEntries => {
				this.runSetEntries = runSetEntries;
				this.checkAllDataLoaded ();
			})
		}
	}

	checkAllDataLoaded () {
		if (this.runSetCounts === undefined)
			return;
		if (this.startupRunSetIds !== undefined && this.runSetEntries === undefined)
			return;
		this.allDataLoaded ();
	}

	allDataLoaded () {
		var selections;

		if (this.runSetEntries === undefined)
			selections = [{}];
		else
			selections = this.runSetEntries.concat ([{}]);

		React.render (
			React.createElement (
				Page,
				{
					controller: this,
					initialSelections: selections,
					runSetCounts: this.runSetCounts,
					onChange: this.updateForSelection.bind (this)
				}
			),
			document.getElementById ('comparePage')
		);

		this.updateForSelection (selections);
	}

	updateForSelection (selections) {
		var runSets = selections.map (s => s.runSet).filter (rs => rs !== undefined);
		window.location.hash = xp_common.hashForRunSets (runSets);
	}
}

class Page extends React.Component {
	constructor (props) {
		super (props);
		this.state = {selections: this.props.initialSelections};
	}

	setState (newState) {
		super.setState (newState);
		this.props.onChange (newState.selections);
	}

	render () {
		var selections = this.state.selections;
		var runSets = selections.map (s => s.runSet).filter (rs => rs !== undefined);
		runSets = xp_utils.uniqArrayByString (runSets, rs => rs.get ('id').toString ());

		var chart;
		if (runSets.length > 1)
			chart = <xp_charts.ComparisonAMChart graphName="comparisonChart" controller={this.props.controller} runSets={runSets} />;
		else
			chart = <div className="DiagnosticBlock">Please select at least two run sets.</div>;

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
	handleChange (index, newSelection) {
		var selections = xp_utils.updateArray (this.props.selections, index, newSelection);
		this.props.onChange ({selections: selections});
	}

	addSelector () {
		this.props.onChange ({selections: this.props.selections.concat ([{}])});
	}

	removeSelector (i) {
		this.props.onChange ({selections: xp_utils.removeArrayElement (this.props.selections, i)});
	}

	render () {
		function renderSelector (selection, index) {
			return <section>
				<xp_common.RunSetSelector
					runSetCounts={this.props.runSetCounts}
					selection={selection}
					onChange={this.handleChange.bind (this, index)} />
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
		startupRunSetIds = window.location.hash.substring (1).split ('+').map (parseInt);
	var controller = new Controller (startupRunSetIds);
	controller.loadAsync ();
}

xp_common.start (started);

///<reference path="../typings/react/react.d.ts"/>
///<reference path="../typings/react-dom/react-dom.d.ts"/>

/* @flow */

"use strict";

import * as xp_utils from './utils.ts';
import * as xp_common from './common.tsx';
import * as xp_charts from './charts.tsx';
import * as Database from './database.ts';
import React = require ('react');
import ReactDOM = require ('react-dom');

class Controller {
	startupRunSetIds: Array<number>;
	runSetCounts: Array<Object>;
	runSets: Array<Database.DBRunSet>;

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

		if (this.startupRunSetIds.length > 0) {
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
		if (this.startupRunSetIds.length > 0 && this.runSets === undefined)
			return;
		this.allDataLoaded ();
	}

	allDataLoaded () {
		var runSets = this.runSets;
		if (runSets === undefined)
			runSets = [];

		ReactDOM.render (
			React.createElement (
				Page,
				{
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
		xp_common.setLocationForArray ("ids", runSets.map (rs => rs.get ('id')));
	}
}

function runSetsFromSelections (selections: Array<xp_common.RunSetSelection>): Array<Database.DBRunSet> {
	return selections.map (s => s.runSet).filter (rs => rs !== undefined);
}

type PageProps = {
	initialRunSets: Array<Database.DBRunSet>,
	runSetCounts: Array<Database.RunSetCount>,
	onChange: (runSets: Array<Database.DBRunSet>) => void
};

type PageState = {
	selections: Array<xp_common.RunSetSelection>
};

class Page extends React.Component<PageProps, PageState> {
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
			// FIXME: metric
			chart = <xp_charts.ComparisonAMChart
				runSetLabels={undefined}
				graphName="comparisonChart"
				runSets={runSets}
				metric="time" />;
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

type RunSetSelectorListProps = {
	runSetCounts: Array<Database.RunSetCount>,
	selections: Array<xp_common.RunSetSelection>,
	onChange: (newState: {selections: Array<xp_common.RunSetSelection>}) => void
};

class RunSetSelectorList extends React.Component<RunSetSelectorListProps, void> {
	changeSelector (index: number, newSelection: xp_common.RunSetSelection) {
		var selections = xp_utils.updateArray (this.props.selections, index, newSelection);
		this.props.onChange ({ selections: selections });
	}

	addSelector () {
		var selections = this.props.selections.concat ([{ runSet: undefined, machine: undefined, config: undefined }]);
		this.props.onChange ({ selections: selections });
	}

	removeSelector (i: number) {
		var selections = xp_utils.removeArrayElement (this.props.selections, i);
		this.props.onChange ({ selections: selections });
	}

	render () {
		var renderSelector = (selection: xp_common.RunSetSelection, index: number) => {
			var runSet = selection.runSet;
			var machine = selection.machine;
			var config = selection.config;
			return <section key={"selector" + index.toString ()}>
				<xp_common.RunSetSelector
					runSetCounts={this.props.runSetCounts}
					selection={{runSet: runSet, machine: machine, config: config}}
					onChange={this.changeSelector.bind (this, index)} />
				<button onClick={this.removeSelector.bind (this, index)}>&minus;&ensp;Remove</button>
				<div style={{ clear: 'both' }}></div>
			</section>;
		}
		return <div className="RunSetSelectorList">
			{this.props.selections.map (renderSelector)}
			<footer><button onClick={this.addSelector.bind (this)}>+&ensp;Add Run Set</button></footer>
			</div>;
	}
}

function start (startupRunSetIds) {
	var controller = new Controller (startupRunSetIds.map (id => { if (typeof id === "string") return parseInt (id); else return id; }));
	controller.loadAsync ();
}

xp_common.parseLocationHashForArray ('ids', start);

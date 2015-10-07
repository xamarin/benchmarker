/* @flow */

"use strict";

import * as xp_utils from './utils.js';
import * as xp_common from './common.js';
import * as Database from './database.js';
import React from 'react';

class Controller {
	startupRunSetId: number | void;
	runSetCounts: Array<Object>;
	runSet: Object | void;

	constructor (startupRunSetId) {
		this.startupRunSetId = startupRunSetId;
	}

	loadAsync () {
		Database.fetchRunSetCounts (runSetCounts => {
				this.runSetCounts = runSetCounts;
				this.checkAllDataLoaded ();
			}, error => {
				alert ("error loading run set counts: " + error.toString ());
			});

		if (this.startupRunSetId === undefined)
			return;
		Database.fetchRunSet (this.startupRunSetId,
			runSet => {
				if (runSet === undefined)
					this.startupRunSetId = undefined;
				else
					this.runSet = runSet;
				this.checkAllDataLoaded ();
			}, error => {
				alert ("error loading run set: " + error.toString ());
			});
	}

	checkAllDataLoaded () {
		if (this.runSetCounts === undefined)
			return;
		if (this.startupRunSetId !== undefined && this.runSet === undefined)
			return;
		this.allDataLoaded ();
	}

	allDataLoaded () {
		var selection = {};
		if (this.runSet !== undefined) {
			selection = {
				machine: this.runSet.machine,
				config: this.runSet.config,
				runSet: this.runSet
			};
		}

		React.render (
			React.createElement (
				Page,
				{
					controller: this,
					initialSelection: selection,
					runSetCounts: this.runSetCounts,
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
		xp_common.setLocationForDict ({ id: runSet.get ('id') });
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
			detail = <xp_common.RunSetDescription runSet={this.state.selection.runSet} />;

		return <div className="RunSetPage">
			<header>
				<xp_common.Navigation currentPage="" />
			</header>
			<article>
				<div className="panel">
					<xp_common.RunSetSelector
						selection={this.state.selection}
						runSetCounts={this.props.runSetCounts}
						onChange={this.handleChange.bind (this)} />
				</div>
				{detail}
			</article>
		</div>;
	}
}

function start (params) {
	var startupRunSetId = params ['id'];
	if (startupRunSetId === undefined) {
		alert ("Error: Please provide a run set ID.");
		return;
	}
	var controller = new Controller (startupRunSetId);
	controller.loadAsync ();
}

xp_common.parseLocationHashForDict (['id'], start);

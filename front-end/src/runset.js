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
			detail = <xp_common.RunSetDescription controller={this.props.controller} runSet={this.state.selection.runSet} />;

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

function started () {
	var startupRunSetId;
	if (window.location.hash)
		startupRunSetId = window.location.hash.substring (1);
	var controller = new Controller (startupRunSetId);
	controller.loadAsync ();
}

xp_common.start (started);

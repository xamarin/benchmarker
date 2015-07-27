/* @flow */

"use strict";

import * as xp_utils from './utils.js';
import * as xp_common from './common.js';
import React from 'react';

class Controller extends xp_common.Controller {

	startupRunSetIds: Array<string> | void;

	constructor (startupRunSetIds) {
		super ();
		this.startupRunSetIds = startupRunSetIds;
	}

	allDataLoaded () {
		var selections;

		if (this.startupRunSetIds === undefined) {
			selections = [{}];
		} else {
			selections = this.startupRunSetIds.map (id => {
				var runSet = this.runSetForId (id);
				var machine = this.machineForId (runSet.get ('machine').id);
				return {machine: machine, config: runSet.get ('config'), runSet: runSet};
			});
		}

		React.render (
			React.createElement (
				Page,
				{
					controller: this,
					initialSelections: selections,
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
		console.log ("rendering compare page");

		var selections = this.state.selections;
		var runSets = selections.map (s => s.runSet).filter (rs => rs !== undefined);
		runSets = xp_utils.uniqArrayByString (runSets, rs => rs.id);

		var chart;
		if (runSets.length > 1)
			chart = <xp_common.ComparisonChart controller={this.props.controller} runSets={runSets} />;
		else
			chart = <div className='diagnostic'>Please select at least two run sets.</div>;

		return <div>
			<RunSetSelectorList
		controller={this.props.controller}
		selections={this.state.selections}
		onChange={this.setState.bind (this)} />
			{chart}
		</div>;
	}
}

class RunSetSelectorList extends React.Component {
	handleChange (index, newSelection) {
		var selections = xp_utils.updateArray (this.props.selections, index, newSelection);
		this.props.onChange ({selections: selections});
	}

	addSelector () {
		this.props.onChange ({selections: this.props.selections.concat ({})});
	}

	removeSelector (i) {
		this.props.onChange ({selections: xp_utils.removeArrayElement (this.props.selections, i)});
	}

	render () {
		function renderSelector (selection, index) {
			return <section>
				<button onClick={this.removeSelector.bind (this, index)}>Remove</button>
				<xp_common.RunSetSelector
			controller={this.props.controller}
			selection={selection}
			onChange={this.handleChange.bind (this, index)} />
				</section>;
		}
		return <div className="RunSetSelectorList">
			{this.props.selections.map (renderSelector.bind (this))}
			<footer><button onClick={this.addSelector.bind (this)}>Add Run Set</button></footer>
			</div>;
	}
}

function started () {
	var startupRunSetIds;
	if (window.location.hash)
		startupRunSetIds = window.location.hash.substring (1).split ('+');
	var controller = new Controller (startupRunSetIds);
	controller.loadAsync ();
}

xp_common.start (started);

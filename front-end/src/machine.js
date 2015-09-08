/* @flow */

"use strict";

import * as xp_common from './common.js';
import {Parse} from 'parse';
import React from 'react';

class Controller extends xp_common.Controller {
	machineId: string | void;
	machine: Parse.Object | void;

	constructor (machineId) {
		super ();
		this.machineId = machineId;
	}

	loadAsync () {
		var query = new Parse.Query (xp_common.Machine);
		query.get (this.machineId, {
			success: obj => {
				this.machine = obj;
				this.allDataLoaded ();
			},
			error: error => {
				alert ("error loading machine: " + error.toString ());
			}});
	}

	allDataLoaded () {
		React.render (
			<div className="MachinePage">
				<xp_common.Navigation currentPage="" />
				<article>
					<xp_common.MachineDescription
						machine={this.machine} />
				</article>
			</div>,
			document.getElementById ('machinePage')
		);
	}

}

function started () {
	var machineId;
	if (window.location.hash)
		machineId = window.location.hash.substring (1);
	var controller = new Controller (machineId);
	controller.loadAsync ();
}

xp_common.start (started);

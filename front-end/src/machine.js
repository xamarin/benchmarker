/* @flow */

"use strict";

import * as xp_common from './common.js';
import * as xp_utils from './utils.js';
import React from 'react';

class Controller extends xp_common.Controller {

	machineId: string | void;

	constructor (machineId) {
		super ();
		this.machineId = machineId;
	}

	allDataLoaded () {
		React.render (
			<div className="MachinePage">
				<xp_common.Navigation currentPage="" />
				<xp_common.MachineDescription
					machine={xp_utils.find (
						this.allMachines,
						machine => machine.id === this.machineId)} />
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

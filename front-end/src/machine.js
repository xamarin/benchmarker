/* @flow */

"use strict";

import * as xp_common from './common.js';
import * as Database from './database.js';
import React from 'react';

class Controller {
	machineName: string | void;
	machine: Database.DBObject | void;

	constructor (machineName) {
		this.machineName = machineName;
	}

	loadAsync () {
		Database.fetch ('machine?name=eq.' + this.machineName, true,
		objs => {
			this.machine = objs [0];
			this.allDataLoaded ();
		}, error => {
			alert ("error loading machine: " + error.toString ());
		});
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

function start () {
	var machineId;
	if (window.location.hash)
		machineId = window.location.hash.substring (1);
	var controller = new Controller (machineId);
	controller.loadAsync ();
}

start ();

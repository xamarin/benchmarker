/* @flow */

"use strict";

import * as xp_common from './common.js';
import React from 'react';

class DBObject {
	data: Object;

	constructor (data) {
		this.data = data;
	}

	get (key) {
		return this.data [key.toLowerCase ()];
	}
}

function fetchDatabase (query, success, error) {
	var request = new XMLHttpRequest();
	var url = 'http://192.168.99.100:32773/' + query;

	request.onreadystatechange = function () {
		if (this.readyState !== 4)
			return;

		if (this.status !== 200) {
			error ("database fetch failed");
			return;
		}

        var results = JSON.parse (request.responseText);
		var objs = results.map (data => new DBObject (data));
        success (objs);
	};

	request.open('GET', url, true);
	request.send();
}

class Controller extends xp_common.Controller {
	machineName: string | void;
	machine: Parse.Object | void;

	constructor (machineName) {
		super ();
		this.machineName = machineName;
	}

	loadAsync () {
		fetchDatabase ('machine?name=eq.' + this.machineName, objs => {
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

function started () {
	var machineId;
	if (window.location.hash)
		machineId = window.location.hash.substring (1);
	var controller = new Controller (machineId);
	controller.loadAsync ();
}

xp_common.start (started);

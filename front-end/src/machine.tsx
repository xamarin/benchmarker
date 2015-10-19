///<reference path="../typings/react/react.d.ts"/>
///<reference path="../typings/react-dom/react-dom.d.ts"/>

/* @flow */

"use strict";

import * as xp_common from './common.tsx';
import * as Database from './database.ts';
import React = require ('react');
import ReactDOM = require ('react-dom');

class Controller {
	machineName: string;
	machine: Database.DBObject;

	constructor (machineName) {
		this.machineName = machineName;
	}

	loadAsync () {
		Database.fetchAndWrap ('machine?name=eq.' + this.machineName,
		objs => {
			this.machine = objs [0];
			this.allDataLoaded ();
		}, error => {
			alert ("error loading machine: " + error.toString ());
		});
	}

	allDataLoaded () {
		ReactDOM.render (
			<div className="MachinePage">
				<xp_common.Navigation currentPage="" />
				<article>
					<xp_common.MachineDescription
						omitHeader={false}
						machine={this.machine} />
				</article>
			</div>,
			document.getElementById ('machinePage')
		);
	}
}

function start (params) {
	var name = params ['name'];
	if (name === undefined) {
		alert ("Error: Please provide a machine name.");
		return;
	}
	var controller = new Controller (name);
	controller.loadAsync ();
}

xp_common.parseLocationHashForDict (['name'], start);

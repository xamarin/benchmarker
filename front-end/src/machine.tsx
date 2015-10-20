///<reference path="../typings/react/react.d.ts"/>
///<reference path="../typings/react-dom/react-dom.d.ts"/>

/* @flow */

"use strict";

import * as xp_common from './common.tsx';
import * as Database from './database.ts';
import React = require ('react');
import ReactDOM = require ('react-dom');

class Controller {
	private machineName: string;
	private machine: Database.DBObject;

	constructor (machineName: string) {
		this.machineName = machineName;
	}

	public loadAsync () : void {
		Database.fetchAndWrap ('machine?name=eq.' + this.machineName,
		(objs: Array<Database.DBObject>) => {
			this.machine = objs [0];
			this.allDataLoaded ();
		}, (error: Object) => {
			alert ("error loading machine: " + error.toString ());
		});
	}

	private allDataLoaded () : void {
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

function start (params: Object) : void {
	var name = params ['name'];
	if (name === undefined) {
		alert ("Error: Please provide a machine name.");
		return;
	}
	var controller = new Controller (name);
	controller.loadAsync ();
}

xp_common.parseLocationHashForDict (['name'], start);

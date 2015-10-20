///<reference path="../typings/react/react.d.ts"/>
///<reference path="../typings/react-dom/react-dom.d.ts"/>

/* @flow */

"use strict";

import * as xp_common from './common.tsx';
import * as Database from './database.ts';
import React = require ('react');
import ReactDOM = require ('react-dom');

class Controller {
	configName: string;
	config: Database.DBObject;

	constructor (configName: string) {
		this.configName = configName;
	}

	loadAsync () {
		Database.fetchAndWrap ('config?name=eq.' + this.configName,
		(objs: Array<Database.DBObject>) => {
			this.config = objs [0];
			this.allDataLoaded ();
		}, (error: Object) => {
			alert ("error loading config: " + error.toString ());
		});
	}

	allDataLoaded () {
		ReactDOM.render (
			<div className="ConfigPage">
				<xp_common.Navigation currentPage="" />
				<article>
					<xp_common.ConfigDescription
						omitHeader={false}
						config={this.config} />
				</article>
			</div>,
			document.getElementById ('configPage')
		);
	}
}

function start (params: Object) {
	var name = params ['name'];
	if (name === undefined) {
		alert ("Error: Please provide a config name.");
		return;
	}
	var controller = new Controller (name);
	controller.loadAsync ();
}

xp_common.parseLocationHashForDict (['name'], start);

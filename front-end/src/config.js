/* @flow */

"use strict";

import * as xp_common from './common.js';
import * as Database from './database.js';
import React from 'react';
import ReactDOM from 'react-dom';

class Controller {
	configName: string;
	config: Database.DBObject | void;

	constructor (configName) {
		this.configName = configName;
	}

	loadAsync () {
		Database.fetchAndWrap ('config?name=eq.' + this.configName,
		objs => {
			this.config = objs [0];
			this.allDataLoaded ();
		}, error => {
			alert ("error loading config: " + error.toString ());
		});
	}

	allDataLoaded () {
		ReactDOM.render (
			<div className="ConfigPage">
				<xp_common.Navigation currentPage="" />
				<article>
					<xp_common.ConfigDescription
						config={this.config} />
				</article>
			</div>,
			document.getElementById ('configPage')
		);
	}
}

function start (params) {
	var name = params ['name'];
	if (name === undefined) {
		alert ("Error: Please provide a config name.");
		return;
	}
	var controller = new Controller (name);
	controller.loadAsync ();
}

xp_common.parseLocationHashForDict (['name'], start);

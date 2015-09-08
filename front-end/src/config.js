/* @flow */

"use strict";

import * as xp_common from './common.js';
import * as Database from './database.js';
import React from 'react';

class Controller extends xp_common.Controller {
	configName: string | void;
	config: Parse.Object | void;

	constructor (configName) {
		super ();
		this.configName = configName;
	}

	loadAsync () {
		Database.fetch ('config?name=eq.' + this.configName, objs => {
			this.config = objs [0];
			this.allDataLoaded ();
		}, error => {
			alert ("error loading config: " + error.toString ());
		});
	}

	allDataLoaded () {
		React.render (
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

function started () {
	var configId;
	if (window.location.hash)
		configId = window.location.hash.substring (1);
	var controller = new Controller (configId);
	controller.loadAsync ();
}

xp_common.start (started);

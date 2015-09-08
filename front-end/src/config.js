/* @flow */

"use strict";

import * as xp_common from './common.js';
import {Parse} from 'parse';
import React from 'react';

class Controller extends xp_common.Controller {
	configId: string | void;
	config: Parse.Object | void;

	constructor (configId) {
		super ();
		this.configId = configId;
	}

	loadAsync () {
		var query = new Parse.Query (xp_common.Config);
		query.get (this.configId, {
			success: obj => {
				this.config = obj;
				this.allDataLoaded ();
			},
			error: error => {
				alert ("error loading config: " + error.toString ());
			}});
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

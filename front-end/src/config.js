"use strict";

import * as xp_common from './common.js';
import * as xp_utils from './utils.js';
import Parse from 'parse';
import React from 'react';

class Controller extends xp_common.Controller {

	constructor (configId) {
		super ();
		this.configId = configId;
	}

	allDataLoaded () {
		React.render (
			React.createElement (
				xp_common.ConfigDescription,
				{
					config: xp_utils.find (
						this.allConfigs,
						config => config.id === this.configId
					)
				}
			),
			document.getElementById ('configPage')
		);
	}

}

function started () {
	var configId;
	if (window.location.hash)
		configId = window.location.hash.substring (1)
	new Controller (configId);
}

xp_common.start (started);

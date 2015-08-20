/* @flow */

"use strict";

import * as xp_common from './common.js';
import * as xp_utils from './utils.js';
import * as xp_charts from './charts.js';
import {Parse} from 'parse';
import React from 'react';

class Controller extends xp_common.Controller {

	pullRequestId: string | void;

	constructor (pullRequestId) {
		super ();
		this.pullRequestId = pullRequestId;
	}

	allDataLoaded () {
        if (this.pullRequestId === undefined)
            return;
            
        var prRunSet = this.runSetForPullRequestId (this.pullRequestId);
        var pullRequest = prRunSet.get ('pullRequest');
        var baselineRunSet = pullRequest.get ('baselineRunSet');

        if (baselineRunSet === undefined || prRunSet === undefined) {
            console.log ("Error: Could not get baseline or test run set.", baselineRunSet, prRunSet);
            return;
        }

        var runSets = [baselineRunSet, prRunSet];

		React.render (
			<div className="PullRequestPage">
				<xp_common.Navigation currentPage="" />
                <xp_charts.ComparisonAMChart
                    graphName="comparisonChart"
                    controller={this}
                    runSets={runSets} />
			</div>,
			document.getElementById ('pullRequestPage')
		);
	}
}

function started () {
	var pullRequestId;
	if (window.location.hash)
		pullRequestId = window.location.hash.substring (1);
	var controller = new Controller (pullRequestId);
	controller.loadAsync ();
}

xp_common.start (started);

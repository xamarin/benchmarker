/* @flow */

"use strict";

import * as xp_common from './common.js';
import * as xp_utils from './utils.js';
import * as xp_charts from './charts.js';
import {Parse} from 'parse';
import React from 'react';
import GitHub from 'github-api';

class Controller extends xp_common.Controller {
	pullRequestId: string | void;
	pullRequest: Parse.Object | void;
	prRunSet: Parse.Object | void;

	constructor (pullRequestId) {
		super ();
		this.pullRequestId = pullRequestId;
	}

	loadAsync () {
		var pullRequestQuery = new Parse.Query (xp_common.PullRequest)
			.include ('baselineRunSet')
			.include ('baselineRunSet.commit')
		pullRequestQuery.get (this.pullRequestId, {
			success: obj => {
				this.pullRequest = obj;
				this.checkAllDataLoaded ();
			},
			error: error => {
				alert ("error loading pull request: " + error.toString ());
			}});

		var runSetQuery = new Parse.Query (xp_common.RunSet)
			.include ('machine')
			.include ('config')
			.include ('commit')
			.equalTo ('pullRequest', xp_common.PullRequest.createWithoutData (this.pullRequestId));
		runSetQuery.find ({
			success: objs => {
				this.prRunSet = objs [0];
				this.checkAllDataLoaded ();
			},
			error: error => {
				alert ("error loading run set: " + error.toString ());
			}});
	}

	checkAllDataLoaded () {
		if (this.pullRequest === undefined
			|| this.prRunSet === undefined)
			return;

		this.allDataLoaded ();
	}

	allDataLoaded () {
        if (this.pullRequestId === undefined)
            return;

        var prRunSet = this.prRunSet;
        var pullRequest = this.pullRequest;
        var baselineRunSet = pullRequest.get ('baselineRunSet');

        if (baselineRunSet === undefined || prRunSet === undefined) {
            console.log ("Error: Could not get baseline or test run set.", baselineRunSet, prRunSet);
            return;
        }

        var runSets = [baselineRunSet, prRunSet];

        var machine =  prRunSet.get ('machine');
        var config = prRunSet.get ('config');

		React.render (
			<div className="PullRequestPage">
				<xp_common.Navigation currentPage="" />
                <article>
					<div className="outer">
						<div className="inner">
							<PullRequestDescription
								pullRequest={pullRequest} />
							<xp_common.MachineDescription
								machine={machine}
								omitHeader={false} />
							<xp_common.ConfigDescription
								config={config}
								omitHeader={false} />
						</div>
					</div>
                    <xp_charts.ComparisonAMChart
                        graphName="comparisonChart"
                        controller={this}
                        runSets={runSets}
                        runSetLabels={["Baseline", "Pull request"]} />
                </article>
			</div>,
			document.getElementById ('pullRequestPage')
		);
	}
}

type PullRequestDescriptionProps = {
	pullRequest: Parse.Object;
};

type PullRequestDescriptionState = {
    gitHubInfo: Object | void;
};

class PullRequestDescription extends React.Component<PullRequestDescriptionProps, PullRequestDescriptionProps, PullRequestDescriptionState> {
    constructor (props) {
        super (props);
        this.state = { gitHubInfo: undefined };

        var pr = this.props.pullRequest;
        var match = pr.get ('URL').match (/^https?:\/\/github\.com\/mono\/mono\/pull\/(\d+)\/?$/);
        if (match !== null) {
            var github = new GitHub ({});
            var repo = github.getRepo ("mono", "mono");
            repo.getPull (match [1], (err, info) => {
                if (info !== undefined)
                    this.setState ({ gitHubInfo: info });
            });
        }
    }

	render () : Object {
		var pr = this.props.pullRequest;
        var info = this.state.gitHubInfo;

        var baselineRunSet = pr.get ('baselineRunSet');
        var baselineHash = baselineRunSet.get ('commit').get ('hash');

        var title = <span>Loading&hellip;</span>;
        if (info !== undefined && info.title !== undefined)
            title = info.title;

		var description = undefined;
		var commit = <a href={xp_common.githubCommitLink (baselineHash)}>{baselineHash.substring (0, 10)}</a>;
        if (info !== undefined && info.user !== undefined) {
			var user = <a href={info.user ["html_url"]}>{info.user.login}</a>;
			description = <p>Authored by {user}, based on {commit}.</p>
		} else {
			description = <p>Based on {commit}.</p>
		}

		return <div className="Description">
			<h1><a href={pr.get ('URL')}>{title}</a></h1>
			{description}
		</div>;
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

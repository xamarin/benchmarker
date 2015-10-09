/* @flow */

"use strict";

import * as xp_common from './common.js';
import * as xp_utils from './utils.js';
import * as xp_charts from './charts.js';
import * as Database from './database.js';
import React from 'react';
import ReactDOM from 'react-dom';
import GitHub from 'github-api';

class Controller {
	pullRequestId: string;
	dbRow: Object | void;

	constructor (pullRequestId) {
		this.pullRequestId = pullRequestId;
	}

	loadAsync () {
		Database.fetch ('pullrequest?pr_id=eq.' + this.pullRequestId,
		objs => {
			this.dbRow = objs [0];
			this.allDataLoaded ();
		}, error => {
			alert ("error loading config: " + error.toString ());
		});
	}

	allDataLoaded () {
        if (this.pullRequestId === undefined)
            return;
		if (this.dbRow === undefined)
			return;

        var prRunSet = new Database.DBObject (this.dbRow, 'prrs_');
        var pullRequest = new Database.DBObject (this.dbRow, 'pr_');
        var baselineRunSet = new Database.DBObject (this.dbRow, 'blrs_');
		var machine = new Database.DBObject (this.dbRow, 'm_');
		var config = new Database.DBObject (this.dbRow, 'cfg_');

        var runSets = [baselineRunSet, prRunSet];

		// FIXME: metric
		ReactDOM.render (
			<div className="PullRequestPage">
				<xp_common.Navigation currentPage="" />
                <article>
					<div className="outer">
						<div className="inner">
							<PullRequestDescription
								pullRequest={pullRequest}
								baselineRunSet={baselineRunSet} />
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
                        runSets={runSets}
						metric="time"
                        runSetLabels={["Baseline", "Pull request"]} />
					<div style={{ clear: 'both' }}></div>
					<h2>Pull Request Run Set</h2>
					<xp_common.RunSetDescription
						runSet={prRunSet} />
					<h2>Baseline Run Set</h2>
					<xp_common.RunSetDescription
						runSet={baselineRunSet} />
                </article>
			</div>,
			document.getElementById ('pullRequestPage')
		);
	}
}

type PullRequestDescriptionProps = {
	pullRequest: Database.DBObject;
	baselineRunSet: Database.DBObject;
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
		var baselineRunSet = this.props.baselineRunSet;
        var info = this.state.gitHubInfo;

        var baselineHash = baselineRunSet.get ('commit');

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

function start (params) {
	var pullRequestId = params ['id'];
	if (pullRequestId === undefined) {
		alert ("Error: Please provide a pull request ID.");
		return;
	}
	var controller = new Controller (pullRequestId);
	controller.loadAsync ();
}

xp_common.parseLocationHashForDict (['id'], start);

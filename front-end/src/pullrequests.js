/* @flow */

"use strict";

import * as xp_common from './common.js';
import * as xp_utils from './utils.js';
import * as Database from './database.js';
import React from 'react';

class Controller {

	pullRequests : Array<Object> | void;
	limit: number;
	offset: number;

	constructor () {
		this.limit = 10;
		this.offset = 0;
	}

	loadAsync () {
		Database.fetchWithHeaders (
			'pullrequest',
			{
/*
				'Range-Unit': 'items',
				'Range': this.offset.toString () + '-' + (this.offset + this.limit).toString ()
*/
			},
			objs => {
				this.pullRequests = objs;
				this.allDataLoaded ();
			},
			error => {
				alert ("error loading pull requests: " + error.toString ());
			}
		);
	}

	allDataLoaded () {
		console.log (this.pullRequests);
		React.render (
			React.createElement (
				Page,
				{
					controller: this,
					pullRequests: this.pullRequests
				}
			),
			document.getElementById ('pullRequestsPage')
		);
	}

}

class Page extends React.Component {

	constructor (props) {
		super (props);
		this.state = { infos: [] };
		this.props.pullRequests.forEach (
			pullRequest =>
				xp_common.getPullRequestInfo (
					pullRequest.pr_url,
					info => this.setState ({ infos: this.state.infos.concat ([[pullRequest.pr_id, info]]) })));
	}

	render () {
		function renderRow (pullRequest) {
			var info;
			var infos = this.state.infos;
			var id = pullRequest.pr_id;
			for (var i = 0; i < infos.length; ++i) {
				if (infos [i] [0] === id) {
					info = infos [i] [1];
					break;
				}
			}
			var title = info === undefined ? <span>Loading&hellip;</span> : info.title;
			console.log (pullRequest);
			var crashed = xp_utils.intersperse (
				', ',
				pullRequest.blrs_crashedbenchmarks
					.map (name => <code key={'crashed-' + name}>{name}</code>));
			var timedOut = xp_utils.intersperse (
				', ',
				pullRequest.blrs_timedoutbenchmarks
					.map (name => <code key={'timed-out-' + name}>{name}</code>));
			return <tr key={pullRequest.pr_id}>
				<td>#{xp_common.pullRequestIdFromUrl (pullRequest.pr_url)}</td>
				<td><a href={pullRequest.pr_url}>{title}</a></td>
				<td>{new Date (pullRequest.blc_commitdate).toString ()}</td>
				<td>{crashed}</td>
				<td>{timedOut}</td>
				<td>
					<a href={pullRequest.blrs_buildurl}>build</a>,{' '}
					<a href={'pullrequest.html#id=' + pullRequest.pr_id}>compare</a>
				</td>
			</tr>;
		}
		return <div className="PullRequestsPage">
			<header>
				<xp_common.Navigation currentPage="pullRequests" />
			</header>
			<article>
				<table>
					<thead>
						<tr>
							<th>ID</th>
							<th>Title</th>
							<th>Date</th>
							<th>Crashed</th>
							<th>Timed Out</th>
							<th></th>
						</tr>
					</thead>
					<tbody>
						{ this.props.pullRequests
							.sort ((a, b) => new Date (b.prrs_startedat) - new Date (a.prrs_startedat))
							.map (renderRow.bind (this)) }
					</tbody>
				</table>
				<div style={{ clear: 'both' }}></div>
			</article>
		</div>;
	}

}

function start () {
	(new Controller ()).loadAsync ();
}

start ();

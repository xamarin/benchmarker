///<reference path="../typings/react/react.d.ts"/>
///<reference path="../typings/react-dom/react-dom.d.ts"/>

/* @flow */

"use strict";

import * as xp_common from './common.tsx';
import * as xp_utils from './utils.ts';
import * as Database from './database.ts';
import React = require ('react');

class Controller {
	private pullRequests : Array<Object>;
	private limit: number;
	private offset: number;

	constructor () {
		this.limit = 10;
		this.offset = 0;
	}

	public loadAsync () : void {
		Database.fetchWithHeaders (
			'pullrequest',
			{
/*
				'Range-Unit': 'items',
				'Range': this.offset.toString () + '-' + (this.offset + this.limit).toString ()
*/
			},
			(objs: Array<Database.DBObject>) => {
				this.pullRequests = objs;
				this.allDataLoaded ();
			},
			(error: Object) => {
				alert ("error loading pull requests: " + error.toString ());
			}
		);
	}

	private allDataLoaded () : void {
		console.log (this.pullRequests);
		React.render (
			React.createElement (
				Page,
				{
					pullRequests: this.pullRequests
				}
			),
			document.getElementById ('pullRequestsPage')
		);
	}

}

type PageProps = {
	pullRequests: Array<Object>;
};

type PageState = {
	infos: Array<[number, Object]>;
};

class Page extends React.Component<PageProps, PageState> {

	constructor (props: PageProps) {
		super (props);
		this.state = { infos: [] };
		this.props.pullRequests.forEach (
			(pullRequest: Object) =>
				xp_common.getPullRequestInfo (
					pullRequest ['pr_url'],
					(info: Object) => this.setState ({ infos: this.state.infos.concat ([[pullRequest ['pr_id'], info]]) })));
	}

	public render () : JSX.Element {
		const renderRow = (pullRequest: Object) => {
			var info;
			var infos = this.state.infos;
			var id = pullRequest ['pr_id'];
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
				pullRequest ['blrs_crashedbenchmarks']
					.map ((name: string) => <code key={'crashed-' + name}>{name}</code>));
			var timedOut = xp_utils.intersperse (
				', ',
				pullRequest ['blrs_timedoutbenchmarks']
					.map ((name: string) => <code key={'timed-out-' + name}>{name}</code>));
			return <tr key={pullRequest ['pr_id']}>
				<td>#{xp_common.pullRequestIdFromUrl (pullRequest ['pr_url'])}</td>
				<td><a href={pullRequest ['pr_url']}>{title}</a></td>
				<td>{new Date (pullRequest ['blc_commitdate']).toString ()}</td>
				<td>{crashed}</td>
				<td>{timedOut}</td>
				<td>
					<a href={pullRequest ['blrs_buildurl']}>build</a>,{' '}
					<a href={'pullrequest.html#id=' + pullRequest ['pr_id']}>compare</a>
				</td>
			</tr>;
		};
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
							.sort ((a: Object, b: Object) => (new Date (b ['prrs_startedat']) as any) - (new Date (a ['prrs_startedat']) as any))
							.map (renderRow) }
					</tbody>
				</table>
				<div style={{ clear: 'both' }}></div>
			</article>
		</div>;
	}

}

function start () : void {
	(new Controller ()).loadAsync ();
}

start ();

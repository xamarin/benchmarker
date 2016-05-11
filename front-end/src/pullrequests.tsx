///<reference path="../typings/react/react.d.ts"/>
///<reference path="../typings/react-dom/react-dom.d.ts"/>
///<reference path="../typings/require.d.ts"/>

"use strict";

import * as xp_common from './common.tsx';
import * as xp_utils from './utils.ts';
import * as Database from './database.ts';
import React = require ('react');
import ReactDOM = require ('react-dom');

/* tslint:disable: no-var-requires */
require ('!style!css!less!./pullrequests.less');
/* tslint:enable: no-var-requires */

class Controller {
	private pullRequests: Array<Object>;
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
		ReactDOM.render (
			<Page pullRequests={this.pullRequests} />,
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
			var crashed = xp_utils.intersperse (
				', ',
				pullRequest ['blrs_crashedbenchmarks']
					.map ((name: string) => <code key={'crashed-' + name}>{name}</code>));
			var timedOut = xp_utils.intersperse (
				', ',
				pullRequest ['blrs_timedoutbenchmarks']
					.map ((name: string) => <code key={'timed-out-' + name}>{name}</code>));
			var idLink = <a href={pullRequest ['pr_url']}>#{xp_common.parsePullRequestUrl (pullRequest ['pr_url']) [2]}</a>;
			var compareLink = <a href={'pullrequest.html#id=' + pullRequest ['pr_id']} className="pre">{title}</a>;
			var relativeDate = xp_common.relativeDate (new Date (pullRequest ['blc_commitdate']));
			var buildIcon = xp_common.makeBuildIcon (pullRequest ['blrs_buildurl']);
			const configName = pullRequest ['cfg_name'];
			return <tr key={pullRequest ['pr_id']}>
				<td>{idLink}</td>
				<td>{relativeDate}</td>
				<td>{compareLink} {buildIcon}</td>
				<td>{configName}</td>
				<td>{crashed}</td>
				<td>{timedOut}</td>
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
							<th>PR ID</th>
							<th>Date</th>
							<th>Title</th>
							<th>Config</th>
							<th><span className="statusIcon crashed fa fa-exclamation-circle" title="Crashed"></span> Crashed</th>
							<th><span className="statusIcon timedOut fa fa-clock-o" title="Timed Out"></span> Timed Out</th>
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

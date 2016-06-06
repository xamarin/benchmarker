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

	public loadAsync () : void {
		Database.fetch (
			'pullrequest',
			(objs: Array<Object>) => {
				this.pullRequests = xp_utils.sortArrayNumericallyBy (
					objs, (obj) => -new Date (obj ['blrs_startedat']).getTime ());
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
	infos: Array<[Object, any]>;
};

class Page extends React.Component<PageProps, PageState> {

	constructor (props: PageProps) {
		super (props);
		this.state = { infos: [] };
		this.loadMore ();
		document.addEventListener('scroll', (e) => {
			var bottom = document.body.scrollTop + window.innerHeight;
			if (document.body.scrollHeight === bottom)
				this.loadMore ();
		});
	}

	private loadMore () : void {
		const rowsPerPage = 20;

		var infos = this.state.infos.length;
		var prs = this.props.pullRequests.length;
		if (infos === prs)
			return;
		var start = Math.min (infos, prs);
		var count = Math.min (rowsPerPage, prs - infos);
		this.props.pullRequests.slice (start, start + count).forEach (
			(pullRequest: Object) =>
				xp_common.getPullRequestInfo (
					pullRequest ['pr_url'],
					(info: Object) => this.setState ({
						infos: xp_utils.sortArrayNumericallyBy (
							this.state.infos.concat ([[pullRequest, info]]),
							(pair) => -new Date (pair [0] ['prrs_startedat']).getTime ()),
					})));
	}

	public render () : JSX.Element {
		const renderRow = (info: [Object, any]) => {
			var pullRequest = info [0];
			var title = info [1].title;
			var crashed = xp_utils.intersperse (
				', ',
				pullRequest ['blrs_crashedbenchmarks']
					.map ((name: string) => <code key={'crashed-' + name}>{name}</code>));
			var timedOut = xp_utils.intersperse (
				', ',
				pullRequest ['blrs_timedoutbenchmarks']
					.map ((name: string) => <code key={'timed-out-' + name}>{name}</code>));
			var idLink = <a href={pullRequest ['pr_url']}>#{xp_common.pullRequestIdFromUrl (pullRequest ['pr_url'])}</a>;
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
				<div className="TableWrapper"><table>
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
					<tbody>{ this.state.infos.map (renderRow) }</tbody>
				</table></div>
				<div style={{ clear: 'both' }}></div>
			</article>
		</div>;
	}

}

function start () : void {
	(new Controller ()).loadAsync ();
}

start ();

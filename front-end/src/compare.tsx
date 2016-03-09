///<reference path="../typings/react/react.d.ts"/>
///<reference path="../typings/react-dom/react-dom.d.ts"/>
///<reference path="../typings/require.d.ts"/>

"use strict";

import * as xp_utils from './utils.ts';
import * as xp_common from './common.tsx';
import * as xp_charts from './charts.tsx';
import * as Database from './database.ts';
import React = require ('react');
import ReactDOM = require ('react-dom');

/* tslint:disable: no-var-requires */
require ('!style!css!less!./compare.less');
/* tslint:enable: no-var-requires */

class Controller {
	private startupRunSetIds: Array<number>;
	private runSetCounts: Array<Database.RunSetCount>;
	private runSets: Array<Database.DBRunSet>;

	constructor (startupRunSetIds: Array<number>) {
		this.startupRunSetIds = startupRunSetIds;
	}

	public loadAsync () : void {
		Database.fetchRunSetCounts ((runSetCounts: Array<Database.RunSetCount>) => {
			this.runSetCounts = runSetCounts;
			this.checkAllDataLoaded ();
		}, (error: Object) => {
			alert ("error loading run set counts: " + error.toString ());
		});

		if (this.startupRunSetIds.length > 0) {
			Database.fetchRunSets (this.startupRunSetIds,
				(runSets: Array<Database.DBRunSet>) => {
					this.runSets = runSets;
					this.checkAllDataLoaded ();
				}, (error: Object) => {
					alert ("error loading run sets (" + this.startupRunSetIds.join (', ') + "):\n" + error.toString ());
				});
		}
	}

	private checkAllDataLoaded () : void {
		if (this.runSetCounts === undefined)
			return;
		if (this.startupRunSetIds.length > 0 && this.runSets === undefined)
			return;
		this.allDataLoaded ();
	}

	private allDataLoaded () : void {
		var runSets = this.runSets;
		if (runSets === undefined)
			runSets = [];

		ReactDOM.render (<Page
					initialRunSets={runSets}
					runSetCounts={this.runSetCounts}
					onChange={(rss: Array<Database.DBRunSet>) => this.updateForSelection (rss)} />,
			document.getElementById ('comparePage')
		);

		this.updateForSelection (runSets);
	}

	private updateForSelection (runSets: Array<Database.DBRunSet>) : void {
		xp_common.setLocationForArray ("ids", runSets.map ((rs: Database.DBRunSet) => rs.get ('id')));
	}
}

function runSetsFromSelections (selections: Array<xp_common.RunSetSelection>) : Array<Database.DBRunSet> {
	return [].concat.apply ([], selections.map ((s: xp_common.RunSetSelection) => s.runSets))
		.filter ((rs: Database.DBRunSet) => rs !== undefined);
}

interface PageProps {
	initialRunSets: Array<Database.DBRunSet>;
	runSetCounts: Array<Database.RunSetCount>;
	onChange: (runSets: Array<Database.DBRunSet>) => void;
}

interface PageState {
	selections: Array<xp_common.RunSetSelection>;
}

class Page extends React.Component<PageProps, PageState> {
	constructor (props: PageProps) {
		super (props);
		const emptySelection: xp_common.RunSetSelection = { runSets: undefined, machine: undefined, config: undefined };
		const runSetToSelection = (rs: Database.DBRunSet) => {
			return { runSets: [rs], machine: rs.machine, config: rs.config };
		};
		var selections = props.initialRunSets.map (runSetToSelection).concat ([emptySelection]);
		this.state = { selections: selections };
	}

	public render () : JSX.Element {
		var runSets = runSetsFromSelections (this.state.selections);
		runSets = xp_utils.uniqArrayByString (runSets, (rs: Database.DBRunSet) => rs.get ('id').toString ());

		var chart;
		if (runSets.length > 1) {
			// FIXME: metric
			chart = <xp_charts.ComparisonAMChart
				runSetLabels={undefined}
				graphName="comparisonChart"
				runSets={runSets}
				metric="time"
				selectedIndices={[]}/>;
		} else {
			chart = <div className="DiagnosticBlock">Please select at least two run sets.</div>;
		}

		var runSetIds = [].concat.apply ([],
			this.state.selections.map ((s: xp_common.RunSetSelection) =>
				s.runSets !== undefined
					? s.runSets.map ((rs: Database.DBRunSet) => rs.get ('id'))
					: []));

		return <div className="ComparePage">
			<header>
				<xp_common.Navigation
					currentPage="compare"
					comparisonRunSetIds={runSetIds} />
			</header>
			<article>
				<RunSetSelectorList
					runSetCounts={this.props.runSetCounts}
					selections={this.state.selections}
					onChange={(s: Array<xp_common.RunSetSelection>) => {
							this.setState ({ selections: s });
							this.props.onChange (runSetsFromSelections (s));
						}
					} />
				{chart}
				<div style={{ clear: 'both' }}></div>
                {runSets.map ((rs: Database.DBRunSet) => <xp_common.RunSetDescription runSet={rs} />)}
                <xp_common.RunSetMetricsTable runSets={runSets} />
			</article>
		</div>;
	}
}

type RunSetSelectorListProps = {
	runSetCounts: Array<Database.RunSetCount>,
	selections: Array<xp_common.RunSetSelection>,
	onChange: (newState: Array<xp_common.RunSetSelection>) => void
};

class RunSetSelectorList extends React.Component<RunSetSelectorListProps, void> {
	private changeSelector (index: number, newSelection: xp_common.RunSetSelection) : void {
		var selections = xp_utils.updateArray (this.props.selections, index, newSelection);
		this.props.onChange (selections );
	}

	private addSelector () : void {
		var selections = this.props.selections.concat ([{ runSets: undefined, machine: undefined, config: undefined }]);
		this.props.onChange (selections);
	}

	private removeSelector (i: number) : void {
		var selections = xp_utils.removeArrayElement (this.props.selections, i);
		this.props.onChange (selections);
	}

	public render () : JSX.Element {
		var renderSelector = (selection: xp_common.RunSetSelection, index: number) => {
			var runSets = selection.runSets;
			var machine = selection.machine;
			var config = selection.config;
			return <section key={"selector" + index.toString ()}>
				<xp_common.RunSetSelector
					multiple={true}
					runSetCounts={this.props.runSetCounts}
					selection={{runSets: runSets, machine: machine, config: config}}
					onChange={(s: xp_common.RunSetSelection) => this.changeSelector (index, s)} />
				<button onClick={(e: React.MouseEvent) => this.removeSelector (index)} className="delete">
					<span className="fa fa-minus"></span>&ensp;Remove
				</button>
				<div style={{ clear: 'both' }}></div>
			</section>;
		};
		return <div className="RunSetSelectorList">
			{this.props.selections.map (renderSelector)}
			<footer>
				<button onClick={(e: React.MouseEvent) => this.addSelector ()}>
					<span className="fa fa-plus"></span>&ensp;Add Run Set
				</button>
			</footer>
		</div>;
	}
}

function start (startupRunSetIds: Array<number | string>) : void {
	const parseIfNecessary = (id: number | string) => { if (typeof id === "string") return parseInt (id); else return id; };
	var controller = new Controller (startupRunSetIds.map (parseIfNecessary));
	controller.loadAsync ();
}

xp_common.parseLocationHashForArray ('ids', start);

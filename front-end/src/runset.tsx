///<reference path="../typings/react/react.d.ts"/>
///<reference path="../typings/react-dom/react-dom.d.ts"/>

/* @flow */

"use strict";

import * as xp_common from './common.tsx';
import * as Database from './database.ts';
import React = require ('react');
import ReactDOM = require ('react-dom');

class Controller {
	private startupRunSetId: number;
	private runSetCounts: Array<Database.RunSetCount>;
	private runSet: Database.DBRunSet;

	constructor (startupRunSetId: number) {
		this.startupRunSetId = startupRunSetId;
	}

	public loadAsync () : void {
		Database.fetchRunSetCounts ((runSetCounts: Array<Database.RunSetCount>) => {
				this.runSetCounts = runSetCounts;
				this.checkAllDataLoaded ();
			}, (error: Object) => {
				alert ("error loading run set counts: " + error.toString ());
			});

		if (this.startupRunSetId === undefined)
			return;
		Database.fetchRunSet (this.startupRunSetId,
			(runSet: Database.DBRunSet) => {
				if (runSet === undefined) {
					this.startupRunSetId = undefined;
				} else {
					this.runSet = runSet;
				}
				this.checkAllDataLoaded ();
			}, (error: Object) => {
				alert ("error loading run set: " + error.toString ());
			});
	}

	private checkAllDataLoaded () : void {
		if (this.runSetCounts === undefined)
			return;
		if (this.startupRunSetId !== undefined && this.runSet === undefined)
			return;
		this.allDataLoaded ();
	}

	private allDataLoaded () : void {
		var selection: xp_common.RunSetSelection = { machine: undefined, config: undefined, runSet: undefined };
		if (this.runSet !== undefined) {
			selection = {
				machine: this.runSet.machine,
				config: this.runSet.config,
				runSet: this.runSet
			};
		}

		ReactDOM.render (<Page
					initialSelection={selection}
					runSetCounts={this.runSetCounts}
					onChange={(s: xp_common.RunSetSelection) => this.updateForRunSet (s)} />,
			document.getElementById ('runSetPage')
		);

		this.updateForRunSet (selection);
	}

	private updateForRunSet (selection: xp_common.RunSetSelection) : void {
		var runSet = selection.runSet;
		if (runSet === undefined)
			return;
		xp_common.setLocationForDict ({ id: runSet.get ('id') });
	}
}

type PageProps = {
	initialSelection: xp_common.RunSetSelection;
	runSetCounts: Array<Database.RunSetCount>;
	onChange: (selection: xp_common.RunSetSelection) => void;
};

type PageState = {
	selection: xp_common.RunSetSelection;
};

class Page extends React.Component<PageProps, PageState> {
	constructor (props: PageProps) {
		super (props);
		this.state = {selection: this.props.initialSelection};
	}

	private handleChange (newSelection: xp_common.RunSetSelection) : void {
		this.setState ({selection: newSelection});
		this.props.onChange (newSelection);
	}

	public render () : JSX.Element {
		let detail: JSX.Element;
		const runSet = this.state.selection.runSet;
		let runSetIds: Array<number> = undefined;
		if (runSet === undefined) {
			detail = <div className='diagnostic'>Please select a run set.</div>;
		} else {
			detail = <xp_common.RunSetDescription
				runSet={runSet} />;
			runSetIds = [runSet.get ('id')];
		}

		return <div className="RunSetPage">
			<header>
				<xp_common.Navigation
					comparisonRunSetIds={runSetIds}
					currentPage="" />
			</header>
			<article>
				<div className="panel">
					<xp_common.RunSetSelector
						selection={this.state.selection}
						runSetCounts={this.props.runSetCounts}
						onChange={(s: xp_common.RunSetSelection) => this.handleChange (s)} />
				</div>
				{detail}
			</article>
		</div>;
	}
}

function start (params: Object) : void {
	var startupRunSetId = params ['id'];
	if (startupRunSetId === undefined) {
		alert ("Error: Please provide a run set ID.");
		return;
	}
	var controller = new Controller (startupRunSetId);
	controller.loadAsync ();
}

xp_common.parseLocationHashForDict (['id'], start);

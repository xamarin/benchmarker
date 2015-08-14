/* @flow */

/* global google */
/* global AmCharts */

"use strict";

import * as xp_common from './common.js';
import * as xp_utils from './utils.js';
import {Parse} from 'parse';
import React from 'react';

export function start (started: () => void) {
	google.load ('visualization', '1.0', {
		packages: ['corechart'],
		callback: googleChartsDidLoad
	});

    xp_common.start (started);
}

var googleChartsStateComponents = [];

function googleChartsDidLoad () {
	if (googleChartsStateComponents === undefined)
		return;
	var components = googleChartsStateComponents;
	googleChartsStateComponents = undefined;
	for (var i = 0; i < components.length; ++i) {
		var component = components [i];
		if (component === undefined)
			continue;
		component.googleChartsLoaded ();
	}
}

export function canUseGoogleCharts (): boolean {
	return googleChartsStateComponents === undefined;
}

type GoogleChartProps = {
	graphName: string;
	chartClass: any;
	height: number;
	table: Object;
	options: Object;
	selectListener: (chart: Object, event: Object) => void;
}

export class GoogleChart extends React.Component<GoogleChartProps, GoogleChartProps, void> {

	chart: void | Object;

	render () {
		return React.DOM.div({
			className: 'GoogleChart',
			id: this.props.graphName,
			style: {height: this.props.height}
		});
	}

	componentDidMount () {
		this.drawChart (this.props);
	}

	componentDidUpdate () {
		this.drawChart (this.props);
	}

	componentWillReceiveProps (nextProps : any) {
		if (this.shouldComponentUpdate (nextProps, undefined))
			return;
		console.log ("updating chart");
		this.updateChart (nextProps);
	}

	shouldComponentUpdate (nextProps : GoogleChartProps, nextState : void) : boolean {
		if (this.props.chartClass !== nextProps.chartClass)
			return true;
		if (this.props.graphName !== nextProps.graphName)
			return true;
		if (this.props.height !== nextProps.height)
			return true;
		// FIXME: what do we do with the selectListener?
		return false;
	}

	updateChart (props : GoogleChartProps) : void {
		var chart = this.chart;
		if (chart === undefined)
			return;
		chart.draw (props.table, props.options);
		if (props.selectListener !== undefined)
			google.visualization.events.addListener (chart, 'select', props.selectListener.bind (null, chart));
	}

	drawChart (props : GoogleChartProps) : void {
		var ChartClass = props.chartClass;
		this.chart = new ChartClass (document.getElementById (props.graphName));
		this.updateChart (props);
	}
}

export class GoogleChartsStateComponent<P, S> extends React.Component<P, P, S> {
	componentWillMount () {
		if (googleChartsStateComponents === undefined)
			return;

		googleChartsStateComponents.push (this);
	}

	componentWillUnmount () {
		if (googleChartsStateComponents === undefined)
			return;

		googleChartsStateComponents [googleChartsStateComponents.indexOf (this)] = undefined;
	}

	googleChartsLoaded () {
	}
}

type Range = [number, number, number, number];

function calculateRunsRange (runs: Array<Parse.Object>): Range {
	var min: number | void;
	var max: number | void;
	var sum = 0;
	for (var i = 0; i < runs.length; ++i) {
		var v = runs [i].get ('elapsedMilliseconds');
		if (min === undefined || v < min)
			min = v;
		if (max === undefined || v > max)
			max = v;
		sum += v;
	}
	var mean = sum / runs.length;
    sum = 0;
    for (i = 0; i < runs.length; ++i) {
        var v = runs [i].get ('elapsedMilliseconds');
        var diff = v - mean;
        sum += diff * diff;
    }
    var stddev = Math.sqrt (sum) / runs.length;
	if (min === undefined || max === undefined)
		min = max = 0;
	return [min, mean - stddev, mean + stddev, max];
}

function normalizeRange (mean: number, range: Range) : Range {
	return range.map (x => x / mean);
}

function dataArrayForRunSets (controller: xp_common.Controller, runSets: Array<Parse.Object>, runsByIndex : Array<Array<Parse.Object>>): (Array<Array<string | number>> | void) {
    for (var i = 0; i < runSets.length; ++i) {
        if (runsByIndex [i] === undefined)
            return;
    }

    console.log ("all runs loaded");

    var commonBenchmarkIds;

    for (i = 0; i < runSets.length; ++i) {
        var runs = runsByIndex [i];
        var benchmarkIds = xp_utils.uniqStringArray (runs.map (o => o.get ('benchmark').id));
        if (commonBenchmarkIds === undefined) {
            commonBenchmarkIds = benchmarkIds;
            continue;
        }
        commonBenchmarkIds = xp_utils.intersectArray (benchmarkIds, commonBenchmarkIds);
    }

    if (commonBenchmarkIds === undefined || commonBenchmarkIds.length === 0)
        return;

	commonBenchmarkIds = xp_utils.sortArrayLexicographicallyBy (commonBenchmarkIds, id => controller.benchmarkNameForId (id) || "");

    var dataArray = [];

    for (i = 0; i < commonBenchmarkIds.length; ++i) {
        var benchmarkId = commonBenchmarkIds [i];
        var row = [controller.benchmarkNameForId (benchmarkId)];
        var mean = undefined;
        for (var j = 0; j < runSets.length; ++j) {
            var filteredRuns = runsByIndex [j].filter (r => r.get ('benchmark').id === benchmarkId);
            var range = calculateRunsRange (filteredRuns);
            if (mean === undefined) {
                // FIXME: eventually we'll have more meaningful ranges
                mean = range [1];
            }
            row = row.concat (normalizeRange (mean, range));
        }
        dataArray.push (row);
    }

    return dataArray;
}

function runSetLabels (controller: xp_common.Controller, runSets: Array<Parse.Object>) : Array<string> {
    var commitIds = runSets.map (rs => rs.get ('commit').id);
    var commitHistogram = xp_utils.histogramOfStrings (commitIds);

    var includeCommit = commitHistogram.length > 1;

    var includeStartedAt = false;
    for (var i = 0; i < commitHistogram.length; ++i) {
        if (commitHistogram [i] [1] > 1)
            includeStartedAt = true;
    }

    var machineIds = runSets.map (rs => rs.get ('machine').id);
    var includeMachine = xp_utils.uniqStringArray (machineIds).length > 1;

    var configIds = runSets.map (rs => rs.get ('config').id);
    var includeConfigs = xp_utils.uniqStringArray (configIds).length > 1;

    var formatRunSet = runSet => {
        var str = "";
        if (includeCommit) {
            var commit = runSet.get ('commit');
            str = commit.get ('hash') + " (" + commit.get ('commitDate') + ")";
        }
        if (includeMachine) {
            var machine = controller.machineForId (runSet.get ('machine').id);
            if (str !== "")
                str = str + "\n";
            str = str + machine.get ('name');
        }
        if (includeConfigs) {
            var config = controller.configForId (runSet.get ('config').id);
            if (includeMachine)
                str = str + " / ";
            else if (str !== "")
                str = str + "\n";
            str = str + config.get ('name');
        }
        if (includeStartedAt) {
            if (str !== "")
                str = str + "\n";
            str = str + runSet.get ('startedAt');
        }
        return str;
    };

    return runSets.map (formatRunSet);
}


type ComparisonChartProps = {
	runSets: Array<Parse.Object>;
	controller: xp_common.Controller;
}

export class ComparisonChart extends GoogleChartsStateComponent<ComparisonChartProps, void> {

	runsByIndex : Array<Array<Parse.Object>>;
	table: Object;
	height: string;

	constructor (props : ComparisonChartProps) {
		console.log ("run set compare chart constructing");

		super (props);

		this.invalidateState (props.runSets);
	}

	invalidateState (runSets : Array<Parse.Object>) : void {
		this.runsByIndex = [];

		xp_common.pageParseQuery (
			() => {
				var query = new Parse.Query (xp_common.Run);
				query.containedIn ('runSet', runSets);
				return query;
			},
			results => {
				if (this.props.runSets !== runSets)
					return;

				var runSetIndexById = {};
				runSets.forEach ((rs, i) => {
					this.runsByIndex [i] = [];
					runSetIndexById [rs.id] = i;
				});

				results.forEach (r => {
					var i = runSetIndexById [r.get ('runSet').id];
					if (this.runsByIndex [i] === undefined)
						this.runsByIndex [i] = [];
					this.runsByIndex [i].push (r);
				});

				this.runsLoaded ();
			},
			function (error) {
				alert ("error loading runs: " + error.toString ());
			});
	}

	componentWillReceiveProps (nextProps : ComparisonChartProps) {
		this.invalidateState (nextProps.runSets);
	}

	googleChartsLoaded () {
		this.runsLoaded ();
	}

	runsLoaded () {
		var i;

		console.log ("run loaded");

		if (!canUseGoogleCharts ())
			return;

        var dataArray = dataArrayForRunSets (this.props.controller, this.props.runSets, this.runsByIndex);
        if (dataArray === undefined)
            return;

		var data = google.visualization.arrayToDataTable (dataArray, true);

		var labels = runSetLabels (this.props.controller, this.props.runSets);
		for (var i = 0; i < labels.length; ++i)
			data.setColumnLabel (1 + 4 * i, labels [i]);

		var height = (35 + (15 * this.props.runSets.length) * dataArray.length) + "px";

		this.table = data;
		this.height = height;
		this.forceUpdate ();
	}

	render () {
		if (this.table === undefined)
			return <div className='diagnostic'>Loading&hellip;</div>;

		var options = {
			orientation: 'vertical',
			chartArea: {height: '100%'},
			animation: {
				duration: 1000,
				easing: 'out',
			},
			hAxis: {
				gridlines: {
					color: 'transparent'
				},
				baseline: 1.0,
				textPosition: 'none'
			},
			vAxis: {
				gridlines: {
					color: 'transparent'
				}
			}
		};
		return <GoogleChart
			graphName="compareChart"
			className="ComparisonChart"
			chartClass={google.visualization.CandlestickChart}
			height={800 /*{this.height}*/}
			table={this.table}
			options={options} />;
	}
}

type AMChartProps = {
	graphName: string;
	height: number;
	options: Object;
	selectListener: (index: number) => void;
};

export class AMChart extends React.Component<AMChartProps, AMChartProps, void> {
	chart: Object;

	render () {
		return React.DOM.div({id: this.props.graphName, style: {height: this.props.height}});
	}

	componentDidMount () {
		console.log ("mounting chart");
		this.drawChart (this.props);
	}

	componentWillUnmount () {
		console.log ("unmounting chart");
		this.chart.clear ();
	}

	shouldComponentUpdate (nextProps : AMChartProps, nextState : void) : boolean {
		if (this.props.graphName !== nextProps.graphName)
			return true;
		if (this.props.height !== nextProps.height)
			return true;
		if (this.props.options !== nextProps.options)
			return true;
		// FIXME: what do we do with the selectListener?
		return false;
	}

	componentDidUpdate () {
		this.drawChart (this.props);
	}

	drawChart (props : AMChartProps) {
		console.log ("drawing");
		if (this.chart === undefined) {
			this.chart = AmCharts.makeChart (props.graphName, props.options);
			if (this.props.selectListener !== undefined)
				this.chart.addListener ('clickGraphItem', e => { this.props.selectListener (e.index); });
		} else {
			this.chart.validateData ();
		}
	}
}

type TimelineAMChartProps = {
	graphName: string;
	height: number;
	data: Object;
	selectListener: (index: number) => void;
};

export class TimelineAMChart extends React.Component<TimelineAMChartProps, TimelineAMChartProps, void> {
	render () {
		var timelineOptions = {
						"type": "serial",
						"theme": "default",
						"categoryAxis": {
							"axisThickness": 0,
							"gridThickness": 0,
							"labelsEnabled": false,
							"tickLength": 0
						},
						"chartScrollbar": {
							"graph": "average"
						},
						"trendLines": [],
						"graphs": [
							{
								"balloonText": "[[lowName]]",
								"bullet": "round",
								"bulletAlpha": 0,
								"lineColor": xp_common.xamarinColors.blue [2],
								"lineThickness": 0,
								"id": "low",
								"title": "low",
								"valueField": "low"
							},
							{
								"balloonText": "[[highName]]",
								"bullet": "round",
								"bulletAlpha": 0,
								"lineColor": xp_common.xamarinColors.blue [2],
								"fillAlphas": 0.13,
								"fillToGraph": "low",
								"fillColors": xp_common.xamarinColors.blue [2],
								"id": "high",
								"lineThickness": 0,
								"title": "high",
								"valueField": "high"
							},
							{
								"balloonText": "[[tooltip]]",
								"bullet": "round",
								"bulletSize": 4,
								"lineColor": xp_common.xamarinColors.blue [2],
								"lineColorField": "lineColor",
								"id": "geomean",
								"title": "geomean",
								"valueField": "geomean"
							}

						],
						"guides": [],
						"valueAxes": [
							{
								"baseValue": -13,
								"id": "time",
								"axisThickness": 0,
								"fontSize": 12,
								"gridAlpha": 0.07,
								"title": "",
								"titleFontSize": 0
							}
						],
						"allLabels": [],
						"balloon": {},
						"titles": [],
                        "dataProvider": this.props.data
					};

		return <AMChart
			graphName={this.props.graphName}
			height={this.props.height}
			options={timelineOptions}
			selectListener={this.props.selectListener} />;
	}
}

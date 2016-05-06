///<reference path="../typings/react/react.d.ts"/>
///<reference path="../typings/github-api/github-api.d.ts"/>
///<reference path="../typings/tinycolor/tinycolor.d.ts"/>

"use strict";

declare var process: any;

import * as xp_utils from './utils.ts';
import * as Database from './database.ts';
import * as Outliers from './outliers.ts';
import * as RunSets from './runsets.ts';
import React = require ('react');
import ReactDOM = require ('react-dom');
import GitHub = require ('github-api');
import Tinycolor = require ('tinycolor2');

/* tslint:disable: no-var-requires */
const helpGCPauses = require ('html!markdown!../help/gcPauses.md') as string;
/* tslint:enable: no-var-requires */

export var xamarinColors = {
	//        light2     light1     normal     dark1      dark2
	"blue": [ "#91E2F4", "#4FCAE6", "#1FAECE", "#3192B3", "#2C7797" ],
	"teal": [ "#A3EBE1", "#7AD5C9", "#44B8A8", "#38A495", "#278E80" ],
	"green": [ "#CFEFA7", "#B3E770", "#91CA47", "#6FA22E", "#5A8622" ],
	"violet": [ "#CEC0EC", "#B5A1E0", "#9378CD", "#7E68C2", "#614CA0" ],
	"red": [ "#F8C6BB", "#F69781", "#F56D4F", "#E2553D", "#BC3C26" ],
	"amber": [ "#F7E28B", "#F9D33C", "#F1C40F", "#F0B240", "#E7963B" ],
	"gray": [ "#ECF0F1", "#D1D9DD", "#ADB7BE", "#9AA4AB", "#76828A" ],
	"asphalt": [ "#889DB5", "#66819E", "#365271", "#2B3E50", "#1C2B39" ],
};
export var xamarinColorsOrder = [ "blue", "green", "violet", "red", "asphalt", "amber", "gray", "teal" ];

export function xamarinColorInSequence (i: number, brightness: number, lighten: boolean) : string {
    // FIXME: check range of i
    const original = xamarinColors [xamarinColorsOrder [i]][brightness];
    if (!lighten) {
        return original;
    }
    const color = Tinycolor (original);
    color.lighten (12);
    return color.toHexString ();
}

export enum DescriptionFormat {
    Compact,
    Full
};

type ConfigDescriptionProps = {
	config: Database.DBObject;
	format: DescriptionFormat;
};

export class ConfigDescription extends React.Component<ConfigDescriptionProps, void> {
	public render () : JSX.Element {
		var config = this.props.config;

		if (config === undefined)
			return <div></div>;

		var link = <a href={"config.html#name=" + config.get ('name')}>{config.get ('name')}</a>;
		var mono = config.get ('monoExecutable');
		var monoExecutable = mono === undefined
			? <span className="diagnostic">No mono executable specified.</span>
			: <code>{mono}</code>;
		var envVarsMap = config.get ('monoEnvironmentVariables') || {};
		var envVars = Object.keys (envVarsMap);
		var envVarsList = envVars.length === 0
			? <span className="diagnostic">No environment variables specified.</span>
			: <ul>
			{envVars.map ((name: string, i: number) => <li key={"var" + i.toString ()}><code>{name + "=" + envVarsMap [name]}</code></li>)}
		</ul>;
		var options = config.get ('monoOptions') || [];
		var optionsList = options.length === 0
			? <span className="diagnostic">No command-line options specified.</span>
			: <code>{options.join (' ')}</code>;

        if (this.props.format === DescriptionFormat.Compact)
            return link;

		return <div className="Description">
			<p>{link}</p>
			<dl>
				<dt>Environment Variables</dt><dd>{envVarsList}</dd>
				<dt>Mono Executable</dt><dd>{monoExecutable}</dd>
				<dt>Command-line Options</dt><dd>{optionsList}</dd>
			</dl>
		</div>;
	}
}

type MachineDescriptionProps = {
	machine: Database.DBObject;
	format: DescriptionFormat;
};

export class MachineDescription extends React.Component<MachineDescriptionProps, void> {
	public render () : JSX.Element {
		var machine = this.props.machine;

		if (machine === undefined)
			return <div></div>;
		var link = <a href={"machine.html#name=" + machine.get ('name')}>{machine.get ('name')}</a>;
		if (this.props.format === DescriptionFormat.Compact)
			return link;
		return <div className="Description">
			<p>
				{link}
				{' '}
				({machine.get ('isDedicated') ? "dedicated" : "non-dedicated"}
				{' '}
				{machine.get ('architecture')})
			</p>
		</div>;
	}
}

export interface MachineConfigSelection {
	machine: Database.DBObject;
	config: Database.DBObject;
	metric: string;
}

type ConfigSelectorProps = {
	includeMetric: boolean;
	runSetCounts: Array<Database.RunSetCount>;
	featuredTimelines: Array<Database.DBObject>;
	selection: Array<MachineConfigSelection>;
	onChange: (selection: Array<MachineConfigSelection>) => void;
};

interface CombinedConfigEntry {
	runSetCounts: Array<Database.RunSetCount>;
	displayString?: string;
}

export class CombinedConfigSelector extends React.Component<ConfigSelectorProps, void> {
	private runSetCounts () : Array<Database.RunSetCount> {
		if (this.props.includeMetric)
			return this.props.runSetCounts;
		return Database.combineRunSetCountsAcrossMetric (this.props.runSetCounts);
	}

	public render () : JSX.Element {
		var userStringForRSC = (r: Database.RunSetCount) => {
			var s = r.machine.get ('name') + " / " + r.config.get ('name');
			if (this.props.includeMetric)
				s = s + " / " + r.metric;
			return s;
		};

		const valueStringForSingleSelection = (selection: MachineConfigSelection) => {
			if (selection.machine === undefined || selection.config === undefined)
				return '';
			var s = selection.machine.get ('name') + '+' + selection.config.get ('name');
			if (this.props.includeMetric)
				s = s + '+' + selection.metric;
			return s;
		};

		const valueStringForSelection = (selection: Array<MachineConfigSelection>) => {
			if (selection.length === 0) {
				return undefined;
			}
			return selection.map (valueStringForSingleSelection).join ('+');
		};

		var sortedRunSetCounts = xp_utils.sortArrayLexicographicallyBy (
			this.runSetCounts (),
			(r: Database.RunSetCount) => userStringForRSC (r).toLowerCase ());

		type MachinesMap = { [name: string]: Array<Database.RunSetCount> };
		var machines: MachinesMap = {};
		for (var i = 0; i < sortedRunSetCounts.length; ++i) {
			const rsc = sortedRunSetCounts [i];
			const machineName = rsc.machine.get ('name');
			machines [machineName] = machines [machineName] || [];
			machines [machineName].push (rsc);
		}

		const featuredRSCs: Array<CombinedConfigEntry> = [];

		if (this.props.featuredTimelines !== undefined) {
			const ftlNames = xp_utils.uniqStringArray (this.props.featuredTimelines.map ((ftl: Database.DBObject) => ftl.get ('name')));
			ftlNames.sort ();
			ftlNames.forEach ((name: string) => {
				const ftls = this.props.featuredTimelines.filter ((ftl: Database.DBObject) => ftl.get ('name') === name);
				const rscs = ftls.map ((ftl: Database.DBObject) => {
					const machine = ftl.get ('machine');
					const config = ftl.get ('config');
					const metric = ftl.get ('metric');
					return Database.findRunSetCount (sortedRunSetCounts, machine, config, metric);
				});
				featuredRSCs.push ({ runSetCounts: rscs, displayString: name });
			});
		}

		const renderEntry = (entry: CombinedConfigEntry) => {
			const string = valueStringForSelection (entry.runSetCounts);
			let displayString = entry.displayString;
			if (displayString === undefined) {
				displayString = entry.runSetCounts [0].config.get ('name');
				if (this.props.includeMetric)
					displayString = displayString + " / " + entry.runSetCounts [0].metric;
			}
			const count = xp_utils.sum (entry.runSetCounts.map ((rsc: Database.RunSetCount) => rsc.count));
			displayString = displayString + " (" + count + ")";
			return <option
				value={string}
				key={string}>
				{displayString}
			</option>;
		};

		const renderGroup = (machinesMap: MachinesMap, name: string) => {
			const sorted = xp_utils.sortArrayNumericallyBy (
				machinesMap [name],
				(x: Database.RunSetCount) => -x.count);
			return <optgroup key={"group" + name} label={name}>
				{sorted.map ((rsc: Database.RunSetCount) => renderEntry ({ runSetCounts: [rsc] }))}
			</optgroup>;
		};

		const selectedValue = valueStringForSelection (this.props.selection);
		var aboutConfig;
		var aboutMachine;
		var aboutMachineConfig;
		if (this.props.selection.length !== 0) {
			aboutConfig = <ConfigDescription
				config={this.props.selection [0].config}
				format={DescriptionFormat.Compact} />;
			aboutMachine = <MachineDescription
				machine={this.props.selection [0].machine}
				format={DescriptionFormat.Compact} />;
			aboutMachineConfig = <p>{aboutConfig}@{aboutMachine}</p>;
		}
		let featuredTimelinesElement: JSX.Element;
		if (this.props.featuredTimelines !== undefined) {
			featuredTimelinesElement = <optgroup label="Featured">
				{featuredRSCs.map (renderEntry)}
			</optgroup>;
		}
		return <div className="CombinedConfigSelector">
			<label>Machine &amp; Config</label>
			<select size={10} value={selectedValue} onChange={(e: React.FormEvent) => this.combinationSelected (e)}>
				{featuredTimelinesElement}
				{Object.keys (machines).map ((m: string) => renderGroup (machines, m))}
			</select>
			{aboutMachineConfig}
			<div style={{ clear: 'both' }}></div>
		</div>;
	}

	private combinationSelected (event: React.FormEvent) : void {
		const target: any = event.target;
		const partitions = xp_utils.partition (target.value.split ('+'), 3);
		const rscs = partitions.map ((names: Array<string>) =>
			Database.findRunSetCount (this.runSetCounts (), names [0], names [1], names [2]));
		this.props.onChange (rscs);
	}
}

export type RunSetSelection = {
	machine: Database.DBObject;
	config: Database.DBObject;
	runSets: Array<Database.DBRunSet>;
}

type RunSetSelectorProps = {
	runSetCounts: Array<Database.RunSetCount>;
	selection: RunSetSelection;
	onChange: (selection: RunSetSelection) => void;
	multiple: boolean;
};

type RunSetSelectorState = {
	runSets: Array<Database.DBRunSet>;
};

export class RunSetSelector extends React.Component<RunSetSelectorProps, RunSetSelectorState> {
	constructor (props: RunSetSelectorProps) {
		super (props);
		this.state = { runSets: undefined };
	}

	public componentWillMount () : void {
		this.fetchData (this.props);
	}

	public componentWillReceiveProps (nextProps: RunSetSelectorProps) : void {
		function getName (obj: Database.DBObject) : string {
			if (obj === undefined)
				return undefined;
			return obj.get ('name');
		}

		if (this.props.runSetCounts === nextProps.runSetCounts &&
				getName (this.props.selection.machine) === getName (nextProps.selection.machine) &&
				getName (this.props.selection.config) === getName (nextProps.selection.config)) {
			return;
		}
		this.setState ({ runSets: undefined });
		this.fetchData (nextProps);
	}

	private fetchData (props: RunSetSelectorProps) : void {
		var machine = props.selection.machine;
		var config = props.selection.config;

		if (machine === undefined || config === undefined)
			return;

		Database.fetchRunSetsForMachineAndConfig (machine, config, (runSets: Array<Database.DBRunSet>) => {
			this.setState ({ runSets: runSets });
		}, (error: Object) => {
			alert ("error loading run sets: " + error.toString ());
		});
	}

	private runSetSelected (event: React.FormEvent) : void {
		if (this.state.runSets === undefined)
			return;
		var options: any = (event.target as HTMLSelectElement).options;
		var runSetIds = [];
		for (var i = 0; i < options.length; ++i)
			if (options [i].selected)
				runSetIds.push (parseInt (options [i].value));
		var runSets = runSetIds.map ((runSetId: number) => Database.findRunSet (this.state.runSets, runSetId));
		if (runSets !== undefined && runSets.length !== 0)
			this.props.onChange ({machine: runSets [0].machine, config: runSets [0].config, runSets: runSets});
	}

	private configsSelected (selection: Array<MachineConfigSelection>) : void {
		if (selection.length !== 1)
			return;
		this.props.onChange ({machine: selection [0].machine, config: selection [0].config, runSets: []});
	}

	public render () : JSX.Element {
		var selection = this.props.selection;
		var machine = selection.machine;
		var config = selection.config;
		var runSets = this.state.runSets;

		var runSetIds = undefined;

		if (selection.runSets !== undefined)
			runSetIds = selection.runSets.map ((rs: Database.DBRunSet) => rs.get ('id'));

		const openRunSetDescription = (id: number) => {
			return window.open ('runset.html#id=' + id);
		};

		const renderRunSet = (runSet: Database.DBRunSet) => {
			const id = runSet.get ('id');
			const isPR = !!runSet.get ('pullrequest');
			const prString = isPR ? " - PR" : ""; 
			return <option value={id} key={id} onDoubleClick={() => openRunSetDescription (id)}>
				{xp_utils.formatDate (runSet.commit.get ('commitDate'))} - {runSet.commit.get ('hash').substring (0, 10)}{prString}
			</option>;
		};

		const machineConfigSelections = (machine !== undefined && config !== undefined)
			? [{ machine: selection.machine, config: selection.config, metric: undefined}]
			: [];
		var configSelector =
			<CombinedConfigSelector
				featuredTimelines={undefined}
				selection={ machineConfigSelections }
				includeMetric={false}
				runSetCounts={this.props.runSetCounts}
				onChange={(s: Array<MachineConfigSelection>) => this.configsSelected (s)} />;
		var runSetsSelect = undefined;
		if (runSets === undefined && machine !== undefined && config !== undefined) {
			runSetsSelect = <div className="diagnostic">Loading&hellip;</div>;
		} else if (runSets === undefined) {
			runSetsSelect = <div className="diagnostic">Please select a machine and config.</div>;
		} else if (runSets.length === 0) {
			runSetsSelect = <div className="diagnostic">No run sets found for this machine and config.</div>;
		} else {
			runSetsSelect = <select
				multiple={this.props.multiple}
				size={10}
				value={runSetIds}
				onChange={(e: React.FormEvent) => this.runSetSelected (e)}>
				{runSets.map (renderRunSet)}
			</select>;
		}
		return <div className="RunSetSelector">
			{configSelector}
			<label>Run Set</label>
			{runSetsSelect}
			</div>;
	}
}

function descriptiveMetricName (metric: string) : string {
	switch (metric) {
	case 'time':
		return "Elapsed Times (ms)";
	case 'instructions':
		return "Instruction count";
	case 'memory-integral':
		return "Memory usage (MB * Giga Instructions)";
    case 'acceptable-time-slices':
        return "Percent acceptable time slices";
	default:
		return "Unknown metric";
	}
}

function colorForPauseTime (time: number) : string {
    const low = 50;
    const high = 500;
    if (time < low) {
        return 'hsl(135, 100%, 50%)';
    } else if (time > high) {
        return '#F00';
    }
    const degree = 135 * (1 - (time - low) / (high - low));
    return 'hsl(' + Math.round (degree) + ', 100%, 50%)';
}

type PauseTimelineProps = {
	pauses: Array<RunSets.GCPause>;
};

class PauseTimeline extends React.Component<PauseTimelineProps, void> {
    public componentDidMount () : void {
        this.paint (this.getContext ());
    }

    public componentDidUpdate () : void {
        const context = this.getContext ();
        context.clearRect (0, 0, 500, 20);
        this.paint (context);
    }

    public render () : JSX.Element {
        return <canvas className='PauseTimeline' />;
    }

    private getContext () : CanvasRenderingContext2D {
        return (ReactDOM.findDOMNode (this) as HTMLCanvasElement).getContext ('2d');
    }

    private paint (context: CanvasRenderingContext2D) : void {
        context['webkitImageSmoothingEnabled'] = true;
        context['imageSmoothingEnabled'] = true;

        const element = ReactDOM.findDOMNode (this) as HTMLCanvasElement;
        element.width = element.clientWidth;
        element.height = element.clientHeight;

        const width = element.width;
        const height = element.height;

        const n = this.props.pauses.length;
        if (n === 0) {
            return;
        }

        const end = this.props.pauses [n - 1].start + this.props.pauses [n - 1].duration;

        context.fillStyle = '#FFF';
        context.fillRect (0, 0, width, height);

        context.fillStyle = '#F00';
        for (let i = 0; i < n; i++) {
            const start = this.props.pauses [i].start;
            const time = this.props.pauses [i].duration;
            const x = start / end * width;
            const w = time / end * width;

            context.fillStyle = colorForPauseTime (time);
            context.fillRect (x, 0, w, height);
        }
    }
}

type LongestPausesProps = {
	pauses: Array<RunSets.GCPause>;
	n: number;
};

class LongestPauses extends React.Component<LongestPausesProps, void> {
	public render () : JSX.Element {
		const pauses = this.props.pauses.slice ();
		pauses.sort ((a, b: RunSets.GCPause) => b.duration - a.duration);
		const longest = pauses.slice (0, this.props.n);
		return <span>{longest.map ((p: RunSets.GCPause) => p.duration.toPrecision (4)).join (", ")}</span>;
	}
}

type RunSetMetricsTableProps = {
	runSets: Array<Database.DBRunSet>;
};

type RunSetMetricsTableState = {
	runSetData: RunSets.Data;
};

export class RunSetMetricsTable extends React.Component<RunSetMetricsTableProps, RunSetMetricsTableState> {
	constructor (props: RunSetMetricsTableProps) {
		super (props);
		this.state = { runSetData: this.makeRunSetData (props.runSets) };
	}

	private makeRunSetData (runSets: Array<Database.DBRunSet>) : RunSets.Data {
		return new RunSets.Data (runSets, (data: RunSets.Data) => {
			this.forceUpdate ();
		}, (error: Object) => {
			alert ("Could not fetch run set data: " + error.toString ());
		});
	}

	public componentWillReceiveProps (nextProps: RunSetMetricsTableProps) : void {
		this.state.runSetData.abortFetch ();
		this.setState ({ runSetData: this.makeRunSetData (nextProps.runSets) });
    }

    private rowsForRunSetAndBenchmark (
            benchmark: string,
            results: RunSets.BenchmarkResults,
            metrics: Array<string>
        ) : Array<Array<JSX.Element>> {
        const metricRows: Array<Array<JSX.Element>> = [];
        metrics.forEach ((m: string) => {
            let dataPointsString: string;
            let degreeElement: JSX.Element;
            if (RunSets.metricIsAggregate (m)) {
                const value = results.aggregate [m];
                if (value === undefined) {
                    return;
                }
                dataPointsString = value.toString ();
            } else {
                const dataPoints = results.individual [m];
                if (dataPoints === undefined) {
                    return;
                }
                dataPoints.sort ();
                dataPointsString = dataPoints.join (", ");
                var variance = Outliers.outlierVariance (dataPoints);
                var degree = variance < 0.01 ? 'none'
                    : variance < 0.10 ? 'slight'
                    : variance < 0.50 ? 'moderate'
                    : 'severe';
                    degreeElement = <td key={"metricDegree" + benchmark + m}>
                        <div className="degree" title={degree}>
                            <div className={degree}>&nbsp;</div>
                        </div>
                    </td>;
            }
            const metricColumns: Array<JSX.Element> = [];
            metricColumns.push (<td key={"metricNames" + benchmark + m}>{descriptiveMetricName (m)}</td>);
            metricColumns.push (<td key={"metricValues" + benchmark + m}>{dataPointsString}</td>);
            if (degreeElement !== undefined) {
                metricColumns.push (degreeElement);
            }
            metricRows.push (metricColumns);
        });
        const numPauseRows = results.gcPauses.length;
        results.gcPauses.forEach ((pauses: Array<RunSets.GCPause>, i: number) => {
            const position = (i === 0) ? 'First' : (i === numPauseRows - 1) ? 'Last' : 'Middle';
            let nameColumn: JSX.Element;
            let resultColumn: JSX.Element;
            if (i === 0) {
                nameColumn = <td
                        key={"metricName" + benchmark + "gcPauses"}
                        rowSpan={numPauseRows}>
                        GC Pauses
                    </td>;
            }
            if (pauses.length > 0) {
                resultColumn = <td className={position + 'InList'}>
						<PauseTimeline pauses={pauses} />
						Longest: <LongestPauses pauses={pauses} n={10} />
					</td>;
            }
            metricRows.push ([nameColumn, resultColumn]);
        });
        return metricRows;
    }

	public render () : JSX.Element  {
		if (!this.state.runSetData.hasResults ()) {
			return <div key="table" className='DiagnosticBlock'>Loading run data&hellip;</div>;
        }

        let benchmarkNames = this.state.runSetData.benchmarks ();
        this.props.runSets.forEach ((runSet: Database.DBRunSet) => {
            benchmarkNames = benchmarkNames.concat (runSet.crashedBenchmarks ()).concat (runSet.timedOutBenchmarks ());
        });
        benchmarkNames = xp_utils.uniqStringArray (benchmarkNames);
        benchmarkNames.sort ();

        const metrics = this.state.runSetData.metrics ();
        const tableRows: Array<JSX.Element> = [];
        benchmarkNames.forEach ((benchmark: string) => {
            const benchmarkRows: Array<Array<JSX.Element>> = [];

            this.props.runSets.forEach ((runSet: Database.DBRunSet, runSetIndex: number) => {
                const crashedBenchmarks = (runSet.get ('crashedBenchmarks') || []) as Array<string>;
                const timedOutBenchmarks = (runSet.get ('timedOutBenchmarks') || []) as Array<string>;

                const statusIcons: Array<JSX.Element> = [];
                if (crashedBenchmarks.indexOf (benchmark) !== -1)
                    statusIcons.push (<span key="crashed" className="statusIcon crashed fa fa-exclamation-circle" title="Crashed"></span>);
                if (timedOutBenchmarks.indexOf (benchmark) !== -1)
                    statusIcons.push (<span key="timedOut" className="statusIcon timedOut fa fa-clock-o" title="Timed Out"></span>);
                if (statusIcons.length === 0)
                    statusIcons.push (<span key="good" className="statusIcon good fa fa-check" title="Good"></span>);
                const statusStyle = { 'backgroundColor': xamarinColorInSequence (runSetIndex, 0, true) };

                const results = this.state.runSetData.resultsForRunSetAndBenchmark (runSet, benchmark);

                if (results === undefined) {
                    benchmarkRows.push ([
                            // FIXME: don't duplicate this element (see below)
                            <td key={"status" + benchmark + runSet.get ('id')} style={statusStyle} className="statusColumn">
                                {statusIcons}
                            </td>,
                            <td colSpan={3} className="diagnostic">All runs in this run set timed out or crashed.</td>,
                        ]);
                    return;
                }

                const metricRows = this.rowsForRunSetAndBenchmark (benchmark, results, metrics);

                metricRows.forEach ((row: Array<JSX.Element>, i: number) => {
                    let statusElement: JSX.Element;
                    if (i === 0) {
                        statusElement = <td
                                key={"status" + benchmark + runSet.get ('id')}
                                style={statusStyle}
                                rowSpan={metricRows.length}
                                className="statusColumn">
                                {statusIcons}{results.disabled ? ' (disabled)' : ''}
                            </td>;
                    }
                    benchmarkRows.push ([statusElement].concat (row));
                });
            });

            benchmarkRows.forEach ((row: Array<JSX.Element>, i: number) => {
                let benchmarkElement: JSX.Element;
                if (i === 0) {
                    benchmarkElement = <td
                            key={"name" + benchmark}
                            rowSpan={benchmarkRows.length}>
                            <code>{benchmark}</code>
                        </td>;
                }
                // FIXME: This key is wrong.  The key should come from the metric row. 
                tableRows.push (<tr key={"benchmark" + benchmark + i}>
                    {benchmarkElement}
                    {row}
                </tr>);
            });
        });

        return <table>
            <thead>
            <tr key="header">
                <th key="name">Benchmark</th>
                <th key="status">Status</th>
                <th key="metric">Metric</th>
                <th key="results">Results</th>
                <th key="bias">Bias due to Outliers</th>
            </tr>
            </thead>
            <tbody>
                {tableRows}
            </tbody>
        </table>;
    }
}

interface RunSetDescriptionProps extends React.Props<RunSetDescription> {
	runSet: Database.DBRunSet;
    backgroundColor?: string;
}

type RunSetDescriptionState = {
	secondaryCommits: Array<Object>;
	commitInfo: Object;
};

export class RunSetDescription extends React.Component<RunSetDescriptionProps, RunSetDescriptionState> {
	constructor (props: RunSetDescriptionProps) {
		super (props);
		this.state = { secondaryCommits: undefined, commitInfo: undefined };
		this.fetchResults (props.runSet);
	}

	private fetchResults (runSet: Database.DBRunSet) : void {
		var secondaryCommits = runSet.get ('secondaryCommits');
		if (secondaryCommits !== undefined && secondaryCommits.length > 0) {
			Database.fetch ('commit?hash=in.' + secondaryCommits.join (','),
				(objs: Array<Object>) => {
					if (runSet !== this.props.runSet)
						return;
					this.setState ({ secondaryCommits: objs } as any);
				}, (error: Object) => {
					alert ("error loading commits: " + error.toString ());
				});
		}

		getCommitInfo (runSet.get ('commit'), (info: Object) => {
			this.setState ({ commitInfo: info } as any);
		});
	}

	public componentWillReceiveProps (nextProps: RunSetDescriptionProps) : void {
		this.setState ({ secondaryCommits: undefined, commitInfo: undefined });
		this.fetchResults (nextProps.runSet);
	}

	public render () : JSX.Element  {
		var runSet = this.props.runSet;
		var buildURL = runSet.get ('buildURL');
		var buildIcon: JSX.Element;
		var logURLs = runSet.get ('logURLs');
		var logLinks = [];
		var logLinkList: JSX.Element;
		var table: JSX.Element;
		var secondaryProductsList: Array<JSX.Element>;
		var crashedElem: JSX.Element;
		var timedOutElem: JSX.Element;

		if (buildURL !== undefined)
			buildIcon = makeBuildIcon (buildURL);

		if (logURLs !== undefined && Object.keys (logURLs).length !== 0) {
			for (var key of Object.keys (logURLs)) {
				var url = logURLs [key];
				var anchor = document.createElement ('a');
				anchor.href = url;

				var shortUrl = <span>{anchor.hostname}/&hellip;{anchor.pathname.substring (anchor.pathname.lastIndexOf ('/'))}</span>;
				logLinks.push(<li key={"link" + key}><a href={url}><code>{key}</code> ({shortUrl})</a></li>);
			}
			if (logLinks.length === 0) {
				logLinkList = undefined;
			} else {
				logLinkList = <ul key="logLinks">{logLinks}</ul>;
			}
		}

		var secondaryCommits = runSet.get ('secondaryCommits') as Array<string>;
		if (secondaryCommits !== undefined && secondaryCommits.length > 0) {
			var elements;
			if (this.state.secondaryCommits === undefined) {
				elements = secondaryCommits.map ((c: string) => {
					var short = c.substring (0, 10);
					return <li key={"commit" + c}>{short}</li>;
				});
			} else {
				elements = this.state.secondaryCommits.map ((c: string) => {
					var short = c ['hash'].substring(0, 10);
					var link = githubCommitLink (c ['product'], c ['hash']);
					return <li key={"commit" + c ['hash']}><a href={link}>{c ['product']} {short}</a></li>;
				});
			}
			secondaryProductsList = [
				<h1 key="secondaryProductsHeader">Secondary products</h1>,
				<ul key="secondaryProductsList" className='secondaryProducts'>{elements}</ul>,
			];
		}

		const product = runSet.commit ? runSet.commit.get ('product') : 'mono';
		const commitHash = runSet.get ('commit');
		const commitLink = githubCommitLink (product, commitHash);
		let commitName = undefined;
		let commitInfo: JSX.Element = undefined;

		if (this.state.commitInfo !== undefined) {
			var info = this.state.commitInfo;
			commitName = info ['message'];
			var newlineIndex = commitName.indexOf ('\n');
			if (newlineIndex >= 0)
				commitName = commitName.substring (0, newlineIndex);
			if (info ['author'] ['name'] === info ['committer'] ['name']) {
				commitInfo = <p>Authored by {info ['author'] ['name']}.</p>;
			} else {
				commitInfo = <p>Authored by {info ['author'] ['name']}, committed by {info ['committer'] ['name']}.</p>;
			}
		} else {
			commitName = commitHash.substring (0, 10);
		}

		const commitElement = <p><strong><a href={commitLink}>{commitName}</a></strong> {buildIcon}</p>;
		return <div className="Description" style={{ 'backgroundColor': this.props.backgroundColor }} >
			{commitElement}
			{commitInfo}
			{logLinkList}
			{secondaryProductsList}
		</div>;
	}
}

type RunSetDescriptionsProps = {
	runSets: Array<Database.DBRunSet>;
};

export class RunSetDescriptions extends React.Component<RunSetDescriptionsProps, void> {
    public render () : JSX.Element {
        return <div>
            {this.props.runSets.map ((runSet: Database.DBRunSet, i: number) => {
                return <RunSetDescription
					key={i.toString ()}
					runSet={runSet}
					backgroundColor={xamarinColorInSequence (i, 0, true)} />;
            })}
        </div>;
    }
}

export interface RunSetSummaryProps extends React.Props<RunSetSummary> {
	runSet: Database.DBRunSet;
	previousRunSet: Database.DBRunSet;
}

export class RunSetSummary extends React.Component<RunSetSummaryProps, void> {
	public render () : JSX.Element {
		var runSet = this.props.runSet;
		var commitHash = runSet.commit.get ('hash');
		var commitLink = githubCommitLink (runSet.commit.get ('product'), commitHash);

		var prev = this.props.previousRunSet;
		var prevItems;
		if (prev !== undefined) {
			var prevHash = prev.commit.get ('hash');
			var prevLink = githubCommitLink (prev.commit.get ('product'), prevHash);
			var compareLink = githubCompareLink (prevHash, commitHash);
			prevItems = [
				<dt key="previousName">Previous</dt>,
				<dd key="previousValue"><a href={prevLink}>{prevHash.substring (0, 10)}</a><br /><a href={compareLink}>Compare</a></dd>,
			];
		}

		var runSetLink = "runset.html#id=" + runSet.get ('id');
		return <div className="RunSetSummary">
			<div className="Description">
			<dl>
			<dt>Commit</dt>
			<dd><a href={commitLink}>{commitHash.substring (0, 10)}</a><br /><a href={runSetLink}>Details</a></dd>
			{prevItems}
			</dl>
			</div>
			</div>;
	}
}

export function githubCommitLink (product: string, commit: string) : string {
	var repo = "";
	switch (product) {
		case 'mono':
			repo = 'mono/mono';
			break;
		case 'monodroid':
			repo = 'xamarin/monodroid';
			break;
		case 'benchmarker':
			repo = 'xamarin/benchmarker';
			break;
		default:
			alert("Unknown product " + product);
			return "";
	}
	return "https://github.com/" + repo + "/commit/" + commit;
}

export function githubCompareLink (base: string, compare: string) : string {
	return "https://github.com/mono/mono/compare/" + base + "..." + compare;
}

interface NavigationProps {
	currentPage: string;
	comparisonRunSetIds?: Array<number>;
}

export class Navigation extends React.Component<NavigationProps, void> {
	private openDeployment () : boolean {
		var lastSlashIndex = window.location.href.search ("/[^/]+$");
		var path = window.location.href.substring (lastSlashIndex + 1);
		var deploymentLink = "http://xamarin.github.io/benchmarker/front-end/" + path;
		window.open (deploymentLink);
		return false;
	}

	public render () : JSX.Element {
		var deploymentLink;
		if (process.env.NODE_ENV !== 'production') {
			deploymentLink =
				<a title="Go to the deployed page"
					className="deselected deployment"
					onClick={() => this.openDeployment ()}>Deployment</a>;
		}

		var classFor = (page: string) =>
			this.props.currentPage === page ? 'selected' : 'deselected';
		var compareLink = "compare.html";
		var timelineLink = "index.html";
		if (this.props.comparisonRunSetIds !== undefined
			&& this.props.comparisonRunSetIds.length !== 0) {
			var selectionIds = this.props.comparisonRunSetIds.join ("+");
			compareLink = compareLink + "#ids=" + selectionIds;

			// FIXME: This would be nice to preserve, but if we do, then the
			// machine/config selection should also be preserved.

			// timelineLink = timelineLink + "#selection=" + selectionIds;
		}
		return <div className="Navigation">
			<div className="NavigationSection" />
			<div className="NavigationSection Center" >
				<a title="View a timeline of all benchmarks"
					className={classFor ('timeline')}
					href={timelineLink}>Timeline</a>
				<a title="Compare the results of multiple run sets"
					className={classFor ('compare')}
					href={compareLink}>Compare</a>
				<a title="View benchmark results for pull requests"
					className={classFor ('pullRequests')}
					href="pullrequests.html">Pull Requests</a>
				<a title="View GC pause times"
					className={classFor ('pauseTimes')}
					href="pausetimes.html">Pause Times</a>
			</div>
			<div className="NavigationSection Right" >
				{deploymentLink}
			</div>
		</div>;
	}
}

/*
 * "" => []
 * "a" => ["a"]
 * "a+" => ["a"]
 * "a+b" => ["a", "b"]
 */
export function splitLocationHashValues (values: string) : Array<string> {
	return values.split ('+').filter ((item: string) => item !== '');
}

export function parseLocationHashForDict (items: Array<string>, startFunc: (keyMap: Object) => void) : void {
	var hash = window.location.hash.substring (1);

	if (hash.length === 0) {
		startFunc ({});
		return;
	}

	var components = hash.split ('&');
	var parsed = {};
	var error = false;
	for (let i = 0; i < components.length; ++i) {
		var kv = components [i].split ('=');
		if (kv.length !== 2) {
			error = true;
			break;
		}
		if (xp_utils.findIndex (items, (n: string) => n === kv [0]) < 0) {
			alert ("Warning: Parameter " + kv [0] + " not supported.");
			continue;
		}
		parsed [kv [0]] = kv [1];
	}

	if (!error) {
		startFunc (parsed);
		return;
	}

	var ids = splitLocationHashValues (hash);
	Database.fetchParseObjectIds (ids, (keys: Array<number | string>) => {
		var keyMap = {};
		keys.forEach ((k: number | string, i: number) => keyMap [items [i]] = k);
		startFunc (keyMap);
	}, (err: Object) => {
		alert ("Error: " + err.toString ());
	});
}

export function setLocationForDict (dict: Object) : void {
	var components = [];
	var keys = Object.keys (dict);
	for (var i = 0; i < keys.length; ++i) {
		var k = keys [i];
		if (dict [k] !== undefined)
			components.push (k + "=" + dict [k].toString ());
	}
	window.location.hash = components.join ("&");
}

export function parseLocationHashForArray (key: string, startFunc: (keyArray: Array<string | number>) => void) : void {
	var hash = window.location.hash.substring (1);

	if (hash.length === 0) {
		startFunc ([]);
		return;
	}

	var kv = hash.split ('=');
	if (kv.length === 2 && kv [0] === key) {
		startFunc (splitLocationHashValues (kv [1]));
		return;
	}

	var ids = splitLocationHashValues (hash);
	if (ids.length === 0) {
		startFunc ([]);
		return;
	}

	Database.fetchParseObjectIds (ids, startFunc,
		(error: Object) => {
			alert ("Error: " + error.toString ());
		});
}

export function setLocationForArray (key: string, ids: Array<string>) : void {
	window.location.hash = key + "=" + ids.join ("+");
}

export function pullRequestIdFromUrl (url: string) : number {
	var match = url.match (/^https?:\/\/github\.com\/mono\/mono\/pull\/(\d+)\/?$/);
	if (match === null)
		return undefined;
	return Number (match [1]);
}

function getMonoRepo () : Repo {
	const github = new GitHub ({
		// HACK: A read-only access token to allow higher rate limits.
		token: '319339f37f8f19b7b5ba92ebfcbdb965871440e0',
		auth: 'oauth',
	});
	return github.getRepo ("mono", "mono");
}

export function getPullRequestInfo (url: string, success: (info: Object) => void) : void {
	const id = pullRequestIdFromUrl (url);
	if (id === undefined)
		return;
	const repo = getMonoRepo ();
	repo.getPull (id, (err: Object, info: Object) => {
		if (info) {
			success (info);
		}
	});
}

function getCommitInfo (hash: string, success: (info: Object) => void) : void {
	const repo = getMonoRepo ();
	repo.getCommit (undefined, hash, (err: Object, info: Object) => {
		if (info) {
			success (info);
		}
	});
}

export function relativeDate (then: Date) : JSX.Element {
	var now = new Date ();
	var ago = (now.getTime () - then.getTime ()) / 1000;
	var text;
	var s_m = 60;
	var s_h = s_m * 60;
	var s_d = s_h * 24;
	var s_wk = s_d * 7;
	var s_mo = s_wk * 4;
	var s_y = s_mo * 12;
	function agoify (x: number, unit: string) : string {
		x = Math.floor (x);
		unit = x === 1 ? unit : unit + 's';
		return [x, unit, 'ago'].join (' ');
	}
	if (ago < 0) {
		text = 'in the future';
	} else if (ago < s_m) {
		text = agoify (ago, 'second');
	} else if (ago < s_h) {
		text = agoify (ago / s_m, 'minute');
	} else if (ago < s_d) {
		text = agoify (ago / s_h, 'hour');
	} else if (ago < s_wk) {
		text = agoify (ago / s_d, 'day');
	} else if (ago < s_mo) {
		text = agoify (ago / s_wk, 'week');
	} else if (ago < s_y) {
		text = agoify (ago / s_mo, 'month');
	} else {
		text = then.toString ();
	}
	return <span className="pre" title={then.toString ()}>{text}</span>;
}

export function makeBuildIcon (url: string) : JSX.Element {
	return <a href={url} className="buildIcon fa fa-cogs" title="Build Logs"></a>;
}

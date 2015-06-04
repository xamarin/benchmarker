/* global React */
/* global Parse */
/* global google */
/* global xp_common */

var xp_timeline = (function () {
	"use strict";

	var exports = {};

	function started () {
		let machineId, configId;
		if (window.location.hash) {
			let ids = window.location.hash.substring (1).split ('+');
			if (ids.length === 2) {
				machineId = ids [0];
				configId = ids [1];
			}
		}
		var controller = new Controller (machineId, configId);
	}

	exports.start = xp_common.start.bind (null, started);

	class Controller extends xp_common.Controller {

		initialMachineId: string;
		initialConfigId: string;

		constructor (machineId, configId) {
			super ();
			this.initialMachineId = machineId;
			this.initialConfigId = configId;
		}

		allDataLoaded () {
			let initialSelection = {};
			if (this.initialMachineId !== undefined)
				initialSelection.machine = this.machineForId (this.initialMachineId);
			if (this.initialConfigId !== undefined)
				initialSelection.config = this.configForId (this.initialConfigId);

			React.render (
				React.createElement (
					Page,
					{
						controller: this,
						initialSelection: initialSelection,
						onChange: this.updateForSelection.bind (this)
					}
				),
				document.getElementById ('timelinePage')
			);

			this.updateForSelection (initialSelection);
		}

		updateForSelection (selection) {
			let machine = selection.machine;
			let config = selection.config;
			if (machine === undefined || config === undefined)
				return;
			window.location.hash = machine.id + '+' + config.id;
		}

	}

	class Page extends React.Component {

		constructor (props) {
			super (props);
			this.state = {
				machine: this.props.initialSelection.machine,
				config: this.props.initialSelection.config
			};
		}

		setState (newState) {
			super.setState (newState);
			this.props.onChange (newState);
		}

		render () {

			let chart;

			if (this.state.machine === undefined || this.state.config === undefined)
				chart = <div className="diagnostic">Please select a machine and config.</div>;
			else
				chart = <Chart
					controller={this.props.controller}
					machine={this.state.machine}
					config={this.state.config} />;

			return <div>
				<xp_common.ConfigSelector
					controller={this.props.controller}
					machine={this.state.machine}
					config={this.state.config}
					onChange={this.setState.bind (this)} />
				{chart}
			</div>;

		}

	}

	class Chart extends xp_common.GoogleChartsStateComponent {

		constructor (props) {
			super (props);
			this.invalidateState (props.machine, props.config);
		}

		invalidateState (machine, config) {

			this.state = {};

			var runSetQuery = new Parse.Query (xp_common.RunSet);
			runSetQuery
				.equalTo ('machine', machine)
				.equalTo ('config', config);
			var runQuery = new Parse.Query (xp_common.Run);
			runQuery
				.matchesQuery ('runSet', runSetQuery)
				.limit (1000)
				.find ({
					success: results => {
						if (machine !== this.props.machine || config !== this.props.config)
							return;

						this.allRuns = results;
						this.runsLoaded ();
					},
					error: function (error) {
						alert ("error loading runs: " + error);
					}
				});

		}

		componentWillReceiveProps (nextProps) {
			this.invalidateState (nextProps.machine, nextProps.config);
		}

		render () {

			if (this.state.table === undefined)
				return <div className="diagnostic">Loading&hellip;</div>;

			let options = {
				vAxis: {
					minValue: 0,
					viewWindow: {
						min: 0,
					},
					gridlines: {
						color: 'transparent'
					},
					baseline: 1.0
				},
				hAxis: {
					gridlines: {
						color: 'transparent'
					},
					textPosition: 'none'
				},
				intervals: {
					style: 'area',
				}
			};

			return <xp_common.GoogleChart
				graphName='timelineChart'
				chartClass={google.visualization.LineChart}
				height={600}
				table={this.state.table}
				options={options} />;

		}

		googleChartsLoaded () {
			this.runsLoaded ();
		}

		runsLoaded () {
			var machine = this.props.machine;
			var config = this.props.config;
			var allRuns = this.allRuns;

			if (this.allRuns === undefined)
				return;

			if (!xp_common.canUseGoogleCharts ())
				return;

			let allBenchmarks = this.props.controller.allBenchmarks;
			let runSets = this.props.controller.runSetsForMachineAndConfig (machine, config);
			runSets.sort ((a, b) => {
				var aDate = a.get ('commit').get ('commitDate');
				var bDate = b.get ('commit').get ('commitDate');
				if (aDate.getTime () !== bDate.getTime ())
					return aDate - bDate;
				return a.get ('startedAt') - b.get ('startedAt');
			});

			/* A table of run data. The rows are indexed by benchmark index, the
			 * columns by sorted run set index.
			 */
			let runTable = [];

			/* Get a row index from a benchmark ID. */
			let benchmarkIndicesById = {};
			for (let i = 0; i < allBenchmarks.length; ++i) {
				runTable.push ([]);
				benchmarkIndicesById [allBenchmarks [i].id] = i;
			}

			/* Get a column index from a run set ID. */
			let runSetIndicesById = {};
			for (let i = 0; i < runSets.length; ++i) {
				for (let j = 0; j < allBenchmarks.length; ++j)
					runTable [j].push ([]);
				runSetIndicesById [runSets [i].id] = i;
			}

			/* Partition allRuns by benchmark and run set. */
			for (let i = 0; i < allRuns.length; ++i) {
				let run = allRuns [i];
				let runIndex = runSetIndicesById [run.get ('runSet').id];
				let benchmarkIndex = benchmarkIndicesById [run.get ('benchmark').id];
				runTable [benchmarkIndex] [runIndex].push (run);
			}

			/* Compute the mean elapsed time for each. */
			for (let i = 0; i < allBenchmarks.length; ++i) {
				for (let j = 0; j < runSets.length; ++j) {
					let runs = runTable [i] [j];
					let sum = runs
						.map (run => run.get ('elapsedMilliseconds'))
						.reduce ((sumSoFar, time) => sumSoFar + time, 0);
					runTable [i] [j] = sum / runs.length;
				}
			}

			/* Compute the average time for a benchmark, and normalize times by
			 * it, i.e., in a given run set, a given benchmark took some
			 * proportion of the average time for that benchmark.
			 */
			for (let i = 0; i < allBenchmarks.length; ++i) {
				let filtered = runTable [i].filter (x => !isNaN (x));
				let normal = filtered.reduce ((sum, time) => sum + time, 0) / filtered.length;
				runTable [i] = runTable [i].map (time => time / normal);
			}

			var table = new google.visualization.DataTable ();

			table.addColumn ({type: 'number', label: "Run Set Index"});
			table.addColumn ({type: 'number', label: "Elapsed Time"});
			table.addColumn ({type: 'number', role: 'interval'});
			table.addColumn ({type: 'number', role: 'interval'});

			for (let j = 0; j < runSets.length; ++j) {
				let sum = 0;
				let count = 0;
				let min, max;
				for (let i = 0; i < allBenchmarks.length; ++i) {
					let val = runTable [i] [j];
					if (isNaN (val))
						continue;
					sum += val;
					if (min === undefined || val < min)
						min = val;
					if (max === undefined || val > max)
						max = val;
					++count;
				}
				table.addRow ([j, sum / count, min, max]);
			}

			this.setState ({table: table});

		}

	}

	return exports;

}) ();

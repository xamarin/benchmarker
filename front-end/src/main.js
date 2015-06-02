/* @flow */

var xamarinCompareStart;
var xamarinTimelineStart;

(function () {
	var utils = xamarin_utils;

	var ParseBenchmark;
	var ParseConfig;
	var ParseMachine;
	var ParseRun;
	var ParseRunSet;

	class PerformanceController {

		allBenchmarks: Array<Object>;
		allMachines: Array<Object>;
		allRunSets: Array<Object>;
		allConfigs: Array<Object>;

		constructor () {

			var machineQuery = new Parse.Query (ParseMachine);
			machineQuery.find ({
				success: this.machinesLoaded.bind (this),
				error: function (error) {
					alert ("error loading machines");
				}
			});

			var runSetQuery = new Parse.Query (ParseRunSet);
			runSetQuery.find ({
				success: this.runSetsLoaded.bind (this),
				error: function (error) {
					alert ("error loading run sets");
				}
			});

			var configQuery = new Parse.Query (ParseConfig);
			configQuery.find ({
				success: this.configsLoaded.bind (this),
				error: function (error) {
					alert ("error loading configs");
				}
			});

			var benchmarkQuery = new Parse.Query (ParseBenchmark);
			benchmarkQuery.find ({
				success: results => {
					this.allBenchmarks = results;
					this.checkAllDataLoaded ();
				},
				error: function (error) {
					alert ("error loading benchmarks");
				}
			});
		}

		allDataLoaded () {
		}

		machinesLoaded (results) {
			console.log ("machines loaded: " + results.length);
			this.allMachines = results;
			this.checkAllDataLoaded ();
		}

		configsLoaded (results) {
			this.allConfigs = results;
			this.checkAllDataLoaded ();
		}

		runSetsLoaded (results) {
			console.log ("run sets loaded: " + results.length);
			this.allRunSets = results;
			this.checkAllDataLoaded ();
		}

		benchmarkNameForId (id) {
			for (var i = 0; i < this.allBenchmarks.length; ++i) {
				if (this.allBenchmarks [i].id == id)
					return this.allBenchmarks [i].get ('name');
			}
		}

		machineForId (id) {
			return utils.find (this.allMachines, m => m.id === id);
		}

		configForId (id) {
			return utils.find (this.allConfigs, m => m.id === id);
		}

		runSetForId (id) {
			return utils.find (this.allRunSets, rs => rs.id === id);
		}

		runSetsForMachineAndConfig (machine, config) {
			return this.allRunSets.filter (rs => rs.get ('machine').id === machine.id &&
										   rs.get ('config').id === config.id);
		}

		checkAllDataLoaded () {
			if (this.allMachines === undefined
				|| this.allRunSets === undefined
				|| this.allBenchmarks === undefined
				|| this.allConfigs === undefined)
				return;

			this.allDataLoaded ();
		}

	}

	class TimelineController extends PerformanceController {

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
					TimelineSelector,
					{
						controller: this,
						initialSelection: initialSelection,
						onChange: this.updateForSelection.bind (this)
					}
				),
				document.getElementById ('timelineSelector')
			);

			this.updateForSelection (initialSelection);
		}

		updateForSelection (selection) {
			let machine = selection.machine;
			let config = selection.config;
			if (machine === undefined || config === undefined)
				return;

			var runSetQuery = new Parse.Query (ParseRunSet);
			runSetQuery
				.equalTo ('machine', machine)
				.equalTo ('config', config);
			var runQuery = new Parse.Query (ParseRun);
			runQuery
				.matchesQuery ('runSet', runSetQuery)
				.limit (1000)
				.find ({
					success: this.runsLoaded.bind (this, machine, config),
					error: function (error) {
						alert ("error loading runs");
					}
				});

			window.location.hash = machine.id + '+' + config.id;
		}

		runsLoaded (machine, config, runs) {

			let runSets = this.runSetsForMachineAndConfig (machine, config);
			runSets.sort (function (a, b) {
				return a.get ('startedAt') - b.get ('startedAt');
			});

			/* A table of run data. The rows are indexed by benchmark index, the
			 * columns by sorted run set index.
			 */
			let runTable = [];

			/* Get a row index from a benchmark ID. */
			let benchmarkIndicesById = {};
			for (let i = 0; i < this.allBenchmarks.length; ++i) {
				runTable.push ([]);
				benchmarkIndicesById [this.allBenchmarks [i].id] = i;
			}

			/* Get a column index from a run set ID. */
			let runSetIndicesById = {};
			for (let i = 0; i < runSets.length; ++i) {
				for (let j = 0; j < this.allBenchmarks.length; ++j)
					runTable [j].push ([]);
				runSetIndicesById [runSets [i].id] = i;
			}

			/* Partition runs by benchmark and run set. */
			for (let i = 0; i < runs.length; ++i) {
				let run = runs [i];
				let runIndex = runSetIndicesById [run.get ('runSet').id];
				let benchmarkIndex = benchmarkIndicesById [run.get ('benchmark').id];
				runTable [benchmarkIndex] [runIndex].push (run);
			}

			/* Compute the mean elapsed time for each. */
			for (let i = 0; i < this.allBenchmarks.length; ++i) {
				for (let j = 0; j < runSets.length; ++j) {
					let runs = runTable [i] [j];
					let sum = runs
						.map (run => run.get ('elapsedMilliseconds'))
						.reduce ((sum, time) => sum + time, 0);
					runTable [i] [j] = sum / runs.length;
				}
			}

			/* Compute the average time for a benchmark, and normalize times by
			 * it, i.e., in a given run set, a given benchmark took some
			 * proportion of the average time for that benchmark.
			 */
			for (let i = 0; i < this.allBenchmarks.length; ++i) {
				for (let j = 0; j < runSets.length; ++j) {
					let normal = runTable [i]
						.filter (x => !isNaN (x))
						.reduce ((sum, time) => sum + time, 0)
						/ runTable [i].length;
					runTable [i] = runTable [i].map (time => time / normal);
				}
			}

			var table = new google.visualization.DataTable ();

			/* FIXME: We probably don't actually want to use the date
			 * as the x axis.
			 */
			table.addColumn ({type: 'date', label: "Run Set"});
			table.addColumn ({type: 'number', label: "Elapsed Time"});
			table.addColumn ({type: 'number', role: 'interval'});
			table.addColumn ({type: 'number', role: 'interval'});

			for (let j = 0; j < runSets.length; ++j) {
				let sum = 0;
				let count = 0;
				let min, max;
				for (let i = 0; i < this.allBenchmarks.length; ++i) {
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
				table.addRow ([runSets [j].get ('startedAt'), sum / count, min, max]);
			}

			let options = {
				vAxis: {
					minValue: 0,
					viewWindow: {
						min: 0,
					},
				},
				intervals: {
					style: 'area',
				},
			};
			let chart = new google.visualization.LineChart (document.getElementById ('timelineChart'));
			chart.draw (table, options);

		}

	}

	class TimelineSelector extends React.Component {

		constructor (props) {
			super (props);
			this.state = props.initialSelection;
		}

		render () {
			return <ConfigSelector
				controller={this.props.controller}
				machine={this.state.machine}
				config={this.state.config}
				onChange={this.handleChange.bind (this)} />;
		}

		handleChange (newState) {
			this.setState (newState);
			this.props.onChange (newState);
		}

	}

	class CompareController extends PerformanceController {

		constructor (startupRunSetIds) {
			super ();
			this.startupRunSetIds = startupRunSetIds;
		}

		allDataLoaded () {
			var selections;

			if (this.startupRunSetIds === undefined) {
				selections = [{}];
			} else {
				selections = this.startupRunSetIds.map (id => {
					let runSet = this.runSetForId (id);
					let machine = this.machineForId (runSet.get ('machine').id);
					return {machine: machine, config: runSet.get ('config'), runSet: runSet};
				});
			}

			React.render (React.createElement (RunSetSelectorList, {controller: this,
																	initialSelections: selections,
																	onChange: this.updateForSelection.bind (this)}),
						  document.getElementById ('runSetSelectors'));

			this.updateForSelection (selections);
		}

		updateForSelection (selection) {
			var runSets = [];
			for (var i = 0; i < selection.length; ++i) {
				var rs = selection [i].runSet;
				if (rs === undefined)
					continue;
				runSets.push (rs);
			}

			if (runSets.length > 1)
				new RunSetComparator (this, runSets);

			window.location.hash = hashForRunSets (runSets);
		}
	}

	class RunSetSelectorList extends React.Component {
		constructor (props) {
			super (props);
			this.state = {selections: this.props.initialSelections};
		}

		handleChange (index, newSelection) {
			var selections = utils.updateArray (this.state.selections, index, newSelection);
			this.setState ({selections: selections});
		}

		addSelector () {
			this.setState ({selections: this.state.selections.concat ({})});
		}

		removeSelector (i) {
			this.setState ({selections: utils.removeArrayElement (this.state.selections, i)});
		}

		setState (newState) {
			super.setState (newState);
			this.props.onChange (newState.selections);
		}

		render () {
			function renderSelector (selection, index) {
				return <section>
					<button onClick={this.removeSelector.bind (this, index)}>Remove</button>
					<RunSetSelector
						controller={this.props.controller}
						selection={selection}
						onChange={this.handleChange.bind (this, index)} />
				</section>;
			}
			return <div className="RunSetSelectorList">
				{this.state.selections.map (renderSelector.bind (this))}
				<footer><button onClick={this.addSelector.bind (this)}>Add Run Set</button></footer>
			</div>;
		}
	}

	class ConfigSelector extends React.Component {
		render () {
			function renderMachineOption (machine) {
				return <option value={machine.id} key={machine.id}>{machine.get ('name')}</option>;
			}
			function renderConfigOption (config) {
				return <option value={config.id} key={config.id}>{config.get ('name')}</option>;
			}
			let machineId;
			if (this.props.machine !== undefined)
				machineId = this.props.machine.id;
			let configId;
			if (this.props.config !== undefined)
				configId = this.props.config.id;
			return <div className="ConfigSelector">
				<select size="6" value={machineId} onChange={this.machineSelected.bind (this)}>
					{this.props.controller.allMachines.map (renderMachineOption)}
				</select>
				<select size="6" value={configId} onChange={this.configSelected.bind (this)}>
					{this.props.controller.allConfigs.map (renderConfigOption)}
				</select>
			</div>;
		}

		machineSelected (event) {
			let machine = this.props.controller.machineForId (event.target.value);
			this.props.onChange ({machine: machine, config: this.props.config});
		}

		configSelected (event) {
			let config = this.props.controller.configForId (event.target.value);
			this.props.onChange ({machine: this.props.machine, config: config});
		}

	}

	class RunSetSelector extends React.Component {

		runSetSelected (event) {
			let selection = this.props.selection;
			let runSetId = event.target.value;
			console.log ("run set selected: " + runSetId);
			let runSet = this.props.controller.runSetForId (runSetId);
			this.props.onChange ({machine: selection.machine, config: selection.config, runSet: runSet});
		}

		render () {
			let selection = this.props.selection;
			console.log (selection);

			let machineId, runSetId, filteredRunSets;

			if (selection.machine !== undefined)
				machineId = selection.machine.id;

			if (selection.runSet !== undefined)
				runSetId = selection.runSet.id;

			if (selection.machine !== undefined && selection.config !== undefined)
				filteredRunSets = this.props.controller.runSetsForMachineAndConfig (selection.machine, selection.config);
			else
				filteredRunSets = [];

			console.log (filteredRunSets);

			function renderRunSetOption (rs) {
				return <option value={rs.id} key={rs.id}>{rs.get ('startedAt').toString ()}</option>;
			}

			let config = selection.config === undefined
				? undefined
				: this.props.controller.configForId (selection.config.id);

			let configSelector =
				<ConfigSelector
					controller={this.props.controller}
					machine={selection.machine}
					config={config}
					onChange={this.props.onChange} />;
			let runSetsSelect = filteredRunSets.length === 0
				? <select size="6" disabled="true">
					<option className="diagnostic">Please select a machine and config.</option>
				</select>
				: <select
					size="6"
					selectedIndex="-1"
					value={runSetId}
					onChange={this.runSetSelected.bind (this)}>
					{filteredRunSets.map (renderRunSetOption)}
				</select>;

			console.log ("runSetId is " + runSetId);

			return <div className="RunSetSelector">
				{configSelector}
				{runSetsSelect}
				<ConfigDescription config={config} />
			</div>;
		}

		getRunSet () {
			return this.state.runSet;
		}
	}

	class ConfigDescription extends React.Component {
		render () {
			let config = this.props.config;

			if (config === undefined)
				return <div className="ConfigDescription"></div>;

			let mono = config.get ('monoExecutable');
			let monoExecutable = mono === undefined
				? <span className="diagnostic">No mono executable specified.</span>
				: <code>{mono}</code>;
			let envVarsMap = config.get ('monoEnvironmentVariables') || {};
			let envVars = Object.keys (envVarsMap);
			let envVarsList = envVars.length === 0
				? <span className="diagnostic">No environment variables specified.</span>
				: <ul>
					{envVars.map (name => <li><code>{name + "=" + envVarsMap [name]}</code></li>)}
				</ul>;
			let options = config.get ('monoOptions') || [];
			let optionsList = options.length === 0
				? <span className="diagnostic">No command-line options specified.</span>
				: <code>{options.join (' ')}</code>;

			return <div className="ConfigDescription">
				<hr />
				<dl>
					<dt>Mono Executable</dt>
					<dd>{monoExecutable}</dd>
					<dt>Environment Variables</dt>
					<dd>{envVarsList}</dd>
					<dt>Command-line Options</dt>
					<dd>{optionsList}</dd>
				</dl>
			</div>;
		}
	}

	class RunSetComparator {
		constructor (controller, runSets) {
			this.controller = controller;

			this.runSets = runSets;
			this.runsByIndex = [];
			for (let i = 0; i < this.runSets.length; ++i) {
				var rs = this.runSets [i];
				var query = new Parse.Query (ParseRun);
				query.equalTo ('runSet', rs);
				query.find ({
					success: results => {
						this.runsByIndex [i] = results;
						this.runsLoaded ();
					},
					error: function (error) {
						alert ("error loading runs");
					}
				});
			}
		}

		runsLoaded () {
			for (var i = 0; i < this.runSets.length; ++i) {
				if (this.runsByIndex [i] === undefined)
					return;
			}

			var commonBenchmarkIds;

			for (var i = 0; i < this.runSets.length; ++i) {
				var runs = this.runsByIndex [i];
				var benchmarkIds = utils.uniqArray (runs.map (o => o.get ('benchmark').id));
				if (commonBenchmarkIds === undefined) {
					commonBenchmarkIds = benchmarkIds;
					continue;
				}
				commonBenchmarkIds = utils.intersectArray (benchmarkIds, commonBenchmarkIds);
			}

			var dataArray = [];

			for (var i = 0; i < commonBenchmarkIds.length; ++i) {
				var benchmarkId = commonBenchmarkIds [i]
				var row = [this.controller.benchmarkNameForId (benchmarkId)];
				var mean = undefined;
				for (var j = 0; j < this.runSets.length; ++j) {
					var runs = this.runsByIndex [j].filter (r => r.get ('benchmark').id === benchmarkId);
					var range = calculateRunsRange (runs);
					if (mean === undefined) {
						// FIXME: eventually we'll have more meaningful ranges
						mean = range [1];
					}
					row = row.concat (normalizeRange (mean, range));
				}
				dataArray.push (row);
			}

			var data = google.visualization.arrayToDataTable (dataArray, true);
			for (var i = 0; i < this.runSets.length; ++i)
				data.setColumnLabel (1 + 4 * i, this.runSets [i].get ('startedAt'));

			var options = { orientation: 'vertical' };

			var div = document.getElementById ('comparisonChart');
			div.style.height = (35 + (15 * this.runSets.length) * commonBenchmarkIds.length) + "px";

			var chart = new google.visualization.CandlestickChart (div);
			chart.draw (data, options);
		}
	}

	function calculateRunsRange (runs) {
		var min, max;
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
		return [min, mean, mean, max];
	}

	function normalizeRange (mean, range) {
		return range.map (x => x / mean);
	}

	function hashForRunSets (runSets) {
		var ids = runSets.map (o => o.id);
		return ids.join ('+');
	}

	function start (started) {
		google.load ('visualization', '1.0', {'packages':['corechart']});
		// FIXME: do this at some point
		//google.setOnLoadCallback (drawChart);

		Parse.initialize('7khPUBga9c7L1YryD1se1bp6VRzKKJESc0baS9ES', 'qnBBT97Mttqsvq3g9zghnBVn2iiHLAQvTzekUigm');

		ParseBenchmark = Parse.Object.extend ('Benchmark');
		ParseConfig = Parse.Object.extend ('Config');
		ParseMachine = Parse.Object.extend ('Machine');
		ParseRun = Parse.Object.extend ('Run');
		ParseRunSet = Parse.Object.extend ('RunSet');

		started ();
	}

	function compareStarted () {
		var startupRunSetIds;
		if (window.location.hash)
			startupRunSetIds = window.location.hash.substring (1).split ('+');
		new CompareController (startupRunSetIds);
	}

	function timelineStarted () {
		let machineId, configId;
		if (window.location.hash) {
			let ids = window.location.hash.substring (1).split ('+');
			if (ids.length == 2) {
				machineId = ids [0];
				configId = ids [1];
			}
		}
		new TimelineController (machineId, configId);
	}

	xamarinCompareStart = start.bind (null, compareStarted);
	xamarinTimelineStart = start.bind (null, timelineStarted);
}) ();

/* @flow */

var xamarinPerformanceStart;

(function () {
	var utils = xamarin_utils;

	var ParseBenchmark;
	var ParseConfig;
	var ParseMachine;
	var ParseRun;
	var ParseRunSet;

	class CompareController {
		constructor (startupRunSetIds) {
			this.startupRunSetIds = startupRunSetIds;

			this.runSetSelectors = [];

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

		checkAllDataLoaded () {
			if (this.allMachines === undefined
				|| this.allRunSets === undefined
				|| this.allBenchmarks === undefined
				|| this.allConfigs === undefined)
				return;

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
				return <div>
					<RunSetSelector
						controller={this.props.controller}
						selection={selection}
						onChange={this.handleChange.bind (this, index)} />
					<button onClick={this.removeSelector.bind (this, index)}>Delete</button>
					</div>;
			}
			return <div>
				{this.state.selections.map (renderSelector.bind (this))}
				<button onClick={this.addSelector.bind (this)}>Add run set!</button>
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
			return <div>
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

			var configSelector =
				<ConfigSelector
					controller={this.props.controller}
					machine={selection.machine}
					config={selection.config}
					onChange={this.props.onChange} />;
			let runSetsSelect =
				<select
					size="6"
					selectedIndex="-1"
					value={runSetId}
					onChange={this.runSetSelected.bind (this)}>
					{filteredRunSets.map (renderRunSetOption)}
				</select>;

			console.log ("runSetId is " + runSetId);

			return <div>
				{configSelector}
				{runSetsSelect}
				<RunSetDescription runSet={this.props.selection.runSet} />
			</div>;
		}

		getRunSet () {
			return this.state.runSet;
		}
	}

	class RunSetDescription extends React.Component {
		render () {
			let runSet = this.props.runSet;

			if (runSet === undefined)
				return <div style={{display: "inline-block"}}>?</div>;

			let mono = runSet.get ('monoExecutable') || "";
			let envVars = runSet.get ('monoEnvironmentVariables') || {};
			let options = runSet.get ('monoOptions') || [];

			return <div style={{display: "inline-block"}}>
				{mono}<br/>
				{
					Object.keys (envVars).map (name => <div>{name + "=" + envVars [name]}</div>)
				}
				{options.toString ()}
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

	function start () {
		google.load ('visualization', '1.0', {'packages':['corechart']});
		// FIXME: do this at some point
		//google.setOnLoadCallback (drawChart);

		Parse.initialize('7khPUBga9c7L1YryD1se1bp6VRzKKJESc0baS9ES', 'qnBBT97Mttqsvq3g9zghnBVn2iiHLAQvTzekUigm');

		ParseBenchmark = Parse.Object.extend ('Benchmark');
		ParseConfig = Parse.Object.extend ('Config');
		ParseMachine = Parse.Object.extend ('Machine');
		ParseRun = Parse.Object.extend ('Run');
		ParseRunSet = Parse.Object.extend ('RunSet');

		var startupRunSetIds;
		if (window.location.hash)
			startupRunSetIds = window.location.hash.substring (1).split ('+');

		new CompareController (startupRunSetIds);
	}

	xamarinPerformanceStart = start;
}) ();

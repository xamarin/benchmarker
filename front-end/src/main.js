/* @flow */

var xamarinPerformanceStart;

(function () {
    var ParseMachine;
    var ParseRunSet;
    var ParseRun;
    var ParseBenchmark;

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

	runSetsLoaded (results) {
	    console.log ("run sets loaded: " + results.length);
	    this.allRunSets = results;
	    this.allConfigNames = uniqArray (this.allRunSets.map (o => o.get ('configName')));
	    this.checkAllDataLoaded ();
	}

	checkAllDataLoaded () {
	    if (this.allMachines === undefined || this.allRunSets === undefined || this.allBenchmarks === undefined)
		return;

	    React.render (React.createElement (RunSetSelectorList, {controller: this, startupRunSetIds: this.startupRunSetIds}),
			  document.getElementById ('runSetSelectors'));
	}

	benchmarkNameForId (id) {
	    for (var i = 0; i < this.allBenchmarks.length; ++i) {
		if (this.allBenchmarks [i].id == id)
		    return this.allBenchmarks [i].get ('name');
	    }
	}

	machineForId (id) {
	    return find (this.allMachines, m => m.id === id);
	}

	runSetForId (id) {
	    return find (this.allRunSets, rs => rs.id === id);
	}

	runSetsForMachineAndConfig (machine, configName) {
	    return this.allRunSets.filter (rs => rs.get ('machine').id === machine.id &&
					   rs.get ('configName') === configName);
	}

	addNewRunSetSelector (runSetId) {
	    this.runSetSelectors.push (new RunSetSelector (this, runSetId));
	}

	runSetChanged (runSetSelector) {
	    /* If the user selected from the last selector, add a new one. */
	    if (runSetSelector === this.runSetSelectors [this.runSetSelectors.length - 1])
		this.addNewRunSetSelector ();

	    var runSets = [];
	    for (var i = 0; i < this.runSetSelectors.length; ++i) {
		var rs = this.runSetSelectors [i].getRunSet ();
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

	    if (this.props.startupRunSetIds === undefined) {
		this.state = {selections: [{}]};
	    } else {
		this.state = {selections: this.props.startupRunSetIds.map (id => {
		    let runSet = this.props.controller.runSetForId (id);
		    let machine = this.props.controller.machineForId (runSet.get ('machine').id);
		    return {machine: machine, configName: runSet.get ('configName'), runSet: runSet};
		})};
	    }
	}

	handleChange (index, newSelection) {
	    this.setState ({selections: updateArray (this.state.selections, index, newSelection)});
	}

	addSelector () {
	    this.setState ({selections: this.state.selections.concat ({})});
	}

	render () {
	    return <div>
		{this.state.selections.map ((selection, i) =>
					    <RunSetSelector controller={this.props.controller} selection={selection} onChange={this.handleChange.bind (this, i)} />)}
		<button onClick={this.addSelector.bind (this)}>Add run set!</button>
		</div>;
	}
    }

    class RunSetSelector extends React.Component {
	machineSelected (event) {
	    let selection = this.props.selection;
	    let machineId = event.target.value;
	    console.log ("machine selected: " + machineId);
	    let machine = this.props.controller.machineForId (machineId);
	    this.props.onChange ({machine: machine, configName: selection.configName});
	}

	configSelected (event) {
	    let selection = this.props.selection;
	    let configName = event.target.value;
	    console.log ("config selected: " + configName);
	    this.props.onChange ({machine: selection.machine, configName: configName});
	}

	runSetSelected (event) {
	    let selection = this.props.selection;
	    let runSetId = event.target.value;
	    console.log ("run set selected: " + runSetId);
	    let runSet = this.props.controller.runSetForId (runSetId);
	    this.props.onChange ({machine: selection.machine, configName: selection.configName, runSet: runSet});
	}

	render () {
	    let selection = this.props.selection;
	    console.log (selection);

	    let machineId, runSetId, filteredRunSets;

	    if (selection.machine !== undefined)
		machineId = selection.machine.id;

	    if (selection.runSet !== undefined)
		runSetId = selection.runSet.id;

	    if (selection.machine !== undefined && selection.configName !== undefined)
		filteredRunSets = this.props.controller.runSetsForMachineAndConfig (selection.machine, selection.configName);
	    else
		filteredRunSets = [];

	    console.log (filteredRunSets);

	    let machineSelect = <select size="6" value={machineId} onChange={this.machineSelected.bind (this)}>
		{
		    this.props.controller.allMachines.map (m => <option value={m.id} key={m.id}>{m.get ('name')}</option>)
		}
		</select>;
	    let configSelect = <select size="6" value={selection.configName} onChange={this.configSelected.bind (this)}>
		{
		    this.props.controller.allConfigNames.map (c => <option value={c} key={c}>{c}</option>)
		}
		</select>;
	    let runSetsSelect = <select size="6" selectedIndex="-1" value={runSetId} onChange={this.runSetSelected.bind (this)}>
		{
		    filteredRunSets.map (rs => <option value={rs.id} key={rs.id}>{rs.get ('startedAt').toString ()}</option>)
		}
		</select>;

	    console.log ("runSetId is " + runSetId);

	    return <div>
		{machineSelect}
	    	{configSelect}
	    	{runSetsSelect}
	    	<RunSetDescription runSet={this.props.selection.runSet} />
		</div>;
	}

	    /*
	addUI (runSet) {
	    this.machineSelect.addEventListener ('change', this.updateRunSets.bind (this));
	    this.configSelect.addEventListener ('change', this.updateRunSets.bind (this));
	    this.runSetSelect.addEventListener ('change', this.runSetSelected.bind (this));

	    if (runSet !== undefined)
		this.runSetSelected ();
	}

	updateRunSets () {
	    var machineIndex = this.machineSelect.selectedIndex;
	    var configIndex = this.configSelect.selectedIndex;

	    if (machineIndex < 0 || configIndex < 0)
		return;


	    populateSelect (this.runSetSelect, );
	}

	runSetSelected () {
	    this.updateDescription ();
	    this.props.controller.runSetChanged (this);
	}
	    */

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
		var benchmarkIds = uniqArray (runs.map (o => o.get ('benchmark').id));
		if (commonBenchmarkIds === undefined) {
		    commonBenchmarkIds = benchmarkIds;
		    continue;
		}
		commonBenchmarkIds = intersectArray (benchmarkIds, commonBenchmarkIds);
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

    function makeSelect () {
	var select = document.createElement ('select');
	select.setAttribute ('size', 6);
	return select;
    }

    function findIndex (arr, f) {
	for (var i = 0; i < arr.length; ++i) {
	    if (f (arr [i]))
		return i;
	}
	return -1;
    }

    function find (arr, f) {
	return arr [findIndex (arr, f)];
    }

    function uniqArray (arr) {
	var hash = {};
	for (var i = 0; i < arr.length; ++i) {
	    hash [arr [i]] = true;
	}
	var newArr = [];
	for (var o in hash) {
	    newArr.push (o);
	}
	return newArr;
    }

    function intersectArray (arr, brr) {
	var crr = [];
	for (var i = 0; i < arr.length; ++i) {
	    var a = arr [i];
	    if (brr.indexOf (a) >= 0)
		crr.push (a);
	}
	return crr;
    }

    function updateArray (arr, i, v) {
	var newArr = arr.slice ();
	newArr [i] = v;
	return newArr;
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

    function deleteChildren (elem) {
	while (elem.firstChild !== null)
	    elem.removeChild (elem.firstChild);
    }

    function populateSelect (select, rows) {
	deleteChildren (select);

	for (var i = 0; i < rows.length; ++i) {
	    var name = rows [i];
	    var option = document.createElement ('option');
	    var text = document.createTextNode (name);
	    option.appendChild (text);
	    select.appendChild (option);
	}
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

	ParseMachine = Parse.Object.extend ('Machine');
	ParseRunSet = Parse.Object.extend ('RunSet');
	ParseRun = Parse.Object.extend ('Run');
	ParseBenchmark = Parse.Object.extend ('Benchmark');

	var startupRunSetIds;
	if (window.location.hash)
	    startupRunSetIds = window.location.hash.substring (1).split ('+');

	new CompareController (startupRunSetIds);
    }

    xamarinPerformanceStart = start;
}) ();
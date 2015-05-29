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

	    let runSetIdToLoad;
	    if (this.startupRunSetIds !== undefined)
		runSetIdToLoad = this.startupRunSetIds [0];

	    React.render (React.createElement (RunSetSelector, {controller: this, runSetIdToLoad: runSetIdToLoad}),
			  document.getElementById ('runSetSelectors'));

	    /*
	    if (this.startupRunSetIds !== undefined)
		this.startupRunSetIds.forEach (this.addNewRunSetSelector.bind (this));
	    else
		this.addNewRunSetSelector ();
	    */
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

    class RunSetSelector extends React.Component {
	constructor (props) {
	    super (props);

	    this.state = {loading: this.props.runSetIdToLoad !== undefined};
	    console.log (this.state);
	}

	componentDidMount () {
	    if (this.props.runSetIdToLoad === undefined)
		return;

	    var query = new Parse.Query (ParseRunSet);
	    query.get (this.props.runSetIdToLoad, {
		success: this.runSetLoaded.bind (this),
		error: function (object, error) {
		    alert ("error loading run set " + runSetId);
		}
	    });
	}

	runSetLoaded (runSet) {
	    console.log ("run set loaded");

	    let machine = runSet.get ('machine');
	    machine = this.props.controller.machineForId (machine.id);

	    let configName = runSet.get ('configName');

	    this.setState ({loading: false,
			    machine: machine,
			    configName: configName,
			    runSet: runSet});
	}

	machineSelected (event) {
	    let machineId = event.target.value;
	    console.log ("machine selected: " + machineId);
	    let machine = this.props.controller.machineForId (machineId);
	    console.log (machine);
	    this.setState ({machine: machine, runSet: undefined});
	}

	configSelected (event) {
	    let configName = event.target.value;
	    console.log ("config selected: " + configName);
	    this.setState ({configName: configName, runSet: undefined});
	}

	runSetSelected (event) {
	    let runSetId = event.target.value;
	    console.log ("run set selected: " + runSetId);
	    let runSet = this.props.controller.runSetForId (runSetId);
	    this.setState ({runSet: runSet});
	}

	render () {
	    console.log (this.state);

	    if (this.state.loading)
		return <div>loading</div>;

	    let machineId, runSetId, filteredRunSets;

	    if (this.state.machine !== undefined)
		machineId = this.state.machine.id;

	    if (this.state.runSet !== undefined)
		runSetId = this.state.runSet.id;

	    if (this.state.machine !== undefined && this.state.configName !== undefined)
		filteredRunSets = this.props.controller.runSetsForMachineAndConfig (this.state.machine, this.state.configName);
	    else
		filteredRunSets = [];

	    console.log (filteredRunSets);

	    let machineSelect = <select size="6" value={machineId} onChange={this.machineSelected.bind (this)}>
		{
		    this.props.controller.allMachines.map (m => <option value={m.id}>{m.get ('name')}</option>)
		}
		</select>;
	    let configSelect = <select size="6" value={this.state.configName} onChange={this.configSelected.bind (this)}>
		{
		    this.props.controller.allConfigNames.map (c => <option value={c}>{c}</option>)
		}
		</select>;
	    let runSetsSelect = <select size="6" selectedIndex="-1" value={runSetId} onChange={this.runSetSelected.bind (this)}>
		{
		    filteredRunSets.map (rs => <option value={rs.id}>{rs.get ('startedAt').toString ()}</option>)
		}
		</select>;

	    console.log ("runSetId is " + runSetId);

	    return <div>
		{machineSelect}
	    	{configSelect}
	    	{runSetsSelect}
	    	{this.renderRunSetDescription ()}
		</div>;
	}

	renderRunSetDescription () {
	    let runSet = this.state.runSet;

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

	/*
	*/
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

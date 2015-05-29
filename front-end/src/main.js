/* @flow */

var xamarinPerformanceStart;

(function () {
    var startupRunSetIds;

    var ParseMachine;
    var ParseRunSet;
    var ParseRun;
    var ParseBenchmark;

    var allMachines;
    var allRunSets;
    var allConfigNames;
    var allBenchmarks;

    var runSetSelectors = [];

    class RunSetSelector {
	constructor (runSetId) {
	    this.containerDiv = document.createElement ('div');

	    var selectorsDiv = document.getElementById ('runSetSelectors');
	    selectorsDiv.appendChild (this.containerDiv);

	    if (runSetId === undefined) {
		this.addUI ();
	    } else {
		var query = new Parse.Query (ParseRunSet);
		query.get (runSetId, {
		    success: this.addUI.bind (this),
		    error: function (object, error) {
			alert ("error loading run set " + runSetId);
		    }
		});
	    }
	}

	addUI (runSet) {
	    this.machineSelect = makeSelect ();
	    this.configSelect = makeSelect ();
	    this.runSetSelect = makeSelect ();

	    this.descriptionDiv = document.createElement ('div');
	    this.descriptionDiv.style.display = 'inline-block';

	    this.containerDiv.appendChild (this.machineSelect);
	    this.containerDiv.appendChild (this.configSelect);
	    this.containerDiv.appendChild (this.runSetSelect);
	    this.containerDiv.appendChild (this.descriptionDiv);

	    var names = allMachines.map (o => o.get ('name'));
	    populateSelect (this.machineSelect, names);
	    populateSelect (this.configSelect, allConfigNames);

	    if (runSet !== undefined) {
		var machineId = runSet.get ('machine').id;
		var machineIndex = findIndex (allMachines, m => m.id === machineId);
		this.machineSelect.selectedIndex = machineIndex;

		var configName = runSet.get ('configName');
		var configIndex = allConfigNames.indexOf (configName);
		this.configSelect.selectedIndex = configIndex;

		this.updateRunSets ();

		var runSetId = runSet.id;
		var runSetIndex = findIndex (this.filteredRunSets, rs => rs.id === runSetId);
		this.runSetSelect.selectedIndex = runSetIndex;
	    }

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

	    var machine = allMachines [machineIndex];
	    var configName = allConfigNames [configIndex];

	    this.filteredRunSets = allRunSets.filter (rs => rs.get ('machine').id === machine.id &&
						      rs.get ('configName') === configName);

	    populateSelect (this.runSetSelect, this.filteredRunSets.map (o => o.get ('startedAt')));
	}

	runSetSelected () {
	    if (this === runSetSelectors [runSetSelectors.length - 1])
		addNewRunSetSelector ();

	    this.updateDescription ();
	    runSetsChanged ();
	}

	getRunSet () {
	    if (this.runSetSelect === undefined)
		return undefined;

	    var runSetIndex = this.runSetSelect.selectedIndex;

	    if (runSetIndex < 0)
		return undefined;

	    return this.filteredRunSets [runSetIndex];
	}

	updateDescription () {
	    var runSet = this.getRunSet ();
	    deleteChildren (this.descriptionDiv);

	    var mono = runSet.get ('monoExecutable');
	    if (mono !== undefined) {
		this.descriptionDiv.appendChild (document.createTextNode (mono));
		this.descriptionDiv.appendChild (document.createElement ('br'));
	    }

	    var envVars = runSet.get ('monoEnvironmentVariables');
	    for (var name in envVars) {
		this.descriptionDiv.appendChild (document.createTextNode (name + "=" + envVars [name]));
		this.descriptionDiv.appendChild (document.createElement ('br'));
	    }

	    var options = runSet.get ('monoOptions');
	    if (options !== undefined) {
		this.descriptionDiv.appendChild (document.createTextNode (options.toString ()));
	    }
	}
    }

    class RunSetComparator {
	constructor (runSets) {
	    this.runSets = runSets;
	    this.runsByIndex = [];
	    for (var i = 0; i < this.runSets.length; ++i) {
		var rs = this.runSets [i];
		var query = new Parse.Query (ParseRun);
		query.equalTo ('runSet', rs);
		query.find ({
		    success: function (index, results) {
			this.runsByIndex [index] = results;
			this.runsLoaded ();
		    }.bind (this, i),
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
		var row = [benchmarkNameForId (benchmarkId)];
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

    function benchmarkNameForId (id) {
	for (var i = 0; i < allBenchmarks.length; ++i) {
	    if (allBenchmarks [i].id == id)
		return allBenchmarks [i].get ('name');
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

    function addNewRunSetSelector (runSetId) {
	runSetSelectors.push (new RunSetSelector (runSetId));
    }

    function checkAllDataLoaded () {
	if (allMachines === undefined || allRunSets === undefined || allBenchmarks === undefined)
	    return;

	if (startupRunSetIds !== undefined)
	    startupRunSetIds.forEach (addNewRunSetSelector);
	else
	    addNewRunSetSelector ();
    }

    function hashForRunSets (runSets) {
	var ids = runSets.map (o => o.id);
	return ids.join ('+');
    }

    function runSetsChanged () {
	var runSets = [];
	for (var i = 0; i < runSetSelectors.length; ++i) {
	    var rs = runSetSelectors [i].getRunSet ();
	    if (rs === undefined)
		continue;
	    runSets.push (rs);
	}

	if (runSets.length > 1) {
	    new RunSetComparator (runSets);
	}

	window.location.hash = hashForRunSets (runSets);
    }

    function machinesLoaded (results) {
	console.log ("machines loaded: " + results.length);
	allMachines = results;
	checkAllDataLoaded ();
    }

    function runSetsLoaded (results) {
	console.log ("run sets loaded: " + results.length);
	allRunSets = results;
	allConfigNames = uniqArray (allRunSets.map (o => o.get ('configName')));
	checkAllDataLoaded ();
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

	var machineQuery = new Parse.Query (ParseMachine);
	machineQuery.find ({
	    success: machinesLoaded,
	    error: function (error) {
		alert ("error loading machines");
	    }
	});

	var runSetQuery = new Parse.Query (ParseRunSet);
	runSetQuery.find ({
	    success: runSetsLoaded,
	    error: function (error) {
		alert ("error loading run sets");
	    }
	});

	var benchmarkQuery = new Parse.Query (ParseBenchmark);
	benchmarkQuery.find ({
	    success: function (results) {
		allBenchmarks = results;
		checkAllDataLoaded ();
	    },
	    error: function (error) {
		alert ("error loading benchmarks");
	    }
	});

	if (window.location.hash)
	    startupRunSetIds = window.location.hash.substring (1).split ('+');
    }

    xamarinPerformanceStart = start;
}) ();

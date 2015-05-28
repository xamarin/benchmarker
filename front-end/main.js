/* @flow */

var xamarinPerformanceStart;

(function () {
    var ParseMachine;
    var ParseRunSet;
    var ParseRun;
    var ParseBenchmark;

    var allMachines;
    var allRunSets;
    var allConfigNames;
    var allBenchmarks;

    var runSetSelectors = [];

    var RunSetSelector = function () {
	this.machineSelect = makeSelect ();
	this.configSelect = makeSelect ();
	this.runSetSelect = makeSelect ();

	var div = document.createElement ('div');
	div.appendChild (this.machineSelect);
	div.appendChild (this.configSelect);
	div.appendChild (this.runSetSelect);

	var selectorsDiv = document.getElementById ('runSetSelectors');
	selectorsDiv.appendChild (div);

	var names = allMachines.map (function (o) { return o.get ('name'); });
	populateSelect (this.machineSelect, names);
	this.machineSelect.addEventListener ('change', this.updateRunSets.bind (this));

	populateSelect (this.configSelect, allConfigNames);
	this.configSelect.addEventListener ('change', this.updateRunSets.bind (this));

	this.runSetSelect.addEventListener ('change', this.runSetSelected.bind (this));
    };

    RunSetSelector.prototype.updateRunSets = function updateRunSets () {
	var machineIndex = this.machineSelect.selectedIndex;
	var configIndex = this.configSelect.selectedIndex;

	if (machineIndex < 0 || configIndex < 0)
	    return;

	var machine = allMachines [machineIndex];
	var configName = allConfigNames [configIndex];

	this.filteredRunSets = allRunSets.filter (function (rs) {
	    return rs.get ('machine').id === machine.id && rs.get ('configName') === configName;
	});

	populateSelect (this.runSetSelect, this.filteredRunSets.map (function (o) { return o.get ('startedAt'); }));
    };

    RunSetSelector.prototype.runSetSelected = function runSetSelected () {
	if (this === runSetSelectors [runSetSelectors.length - 1])
	    addNewRunSetSelector ();

	runSetsChanged ();
    };

    RunSetSelector.prototype.getRunSet = function getRunSet () {
	var runSetIndex = this.runSetSelect.selectedIndex;

	if (runSetIndex < 0)
	    return undefined;

	return this.filteredRunSets [runSetIndex];
    };

    var RunSetComparator = function (runSets) {
	var self = this;
	this.runSets = runSets;
	this.runsByIndex = [];
	for (var i = 0; i < this.runSets.length; ++i) {
	    var rs = this.runSets [i];
	    var query = new Parse.Query (ParseRun);
	    query.equalTo ('runSet', rs);
	    query.find ({
		success: function (index, results) {
		    console.log ("loaded runs for " + index);
		    this.runsByIndex [index] = results;
		    this.runsLoaded ();
		}.bind (this, i),
		error: function (error) {
		    alert ("error loading runs");
		}
	    });
	}
    };

    RunSetComparator.prototype.runsLoaded = function runsLoaded () {
	for (var i = 0; i < this.runSets.length; ++i) {
	    if (this.runsByIndex [i] === undefined)
		return;
	}

	var commonBenchmarkIds;

	for (var i = 0; i < this.runSets.length; ++i) {
	    var runs = this.runsByIndex [i];
	    var benchmarkIds = uniqArray (runs.map (function (o) { return o.get ('benchmark').id; }));
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
		var runs = this.runsByIndex [j].filter (function (r) { return r.get ('benchmark').id === benchmarkId; });
		var range = calculateRunsRange (runs);
		if (mean === undefined) {
		    // FIXME: eventually we'll have more meaningful ranges
		    mean = range [1];
		}
		row = row.concat (normalizeRange (mean, range));
	    }
	    dataArray.push (row);
	}

	console.log (dataArray);

	var data = google.visualization.arrayToDataTable (dataArray, true);
	for (var i = 0; i < this.runSets.length; ++i)
	    data.setColumnLabel (1 + 4 * i, this.runSets [i].get ('startedAt'));

	var options = { orientation: 'vertical' };

	var div = document.getElementById ('comparisonChart');
	div.style.height = (35 + (15 * this.runSets.length) * commonBenchmarkIds.length) + "px";

	var chart = new google.visualization.CandlestickChart (div);
	chart.draw (data, options);
    };

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
	return range.map (function (x) { return x / mean; });
    }

    function populateSelect (select, rows) {
	while (select.firstChild !== null)
	    select.removeChild (select.firstChild);

	for (var i = 0; i < rows.length; ++i) {
	    var name = rows [i];
	    var option = document.createElement ('option');
	    var text = document.createTextNode (name);
	    option.appendChild (text);
	    select.appendChild (option);
	}
    }

    function addNewRunSetSelector () {
	runSetSelectors.push (new RunSetSelector ());
    }

    function checkAllDataLoaded () {
	if (allMachines === undefined || allRunSets === undefined || allBenchmarks === undefined)
	    return;

	addNewRunSetSelector ();
    }

    function runSetsChanged () {
	var runSets = [];
	for (var i = 0; i < runSetSelectors.length; ++i) {
	    var rs = runSetSelectors [i].getRunSet ();
	    if (rs === undefined)
		continue;
	    runSets.push (rs);
	}
	console.log ("run sets selected: " + runSets.length);
	if (runSets.length > 1) {
	    new RunSetComparator (runSets);
	}
    }

    function machinesLoaded (results) {
	console.log ("machines loaded: " + results.length);
	allMachines = results;
	checkAllDataLoaded ();
    }

    function runSetsLoaded (results) {
	console.log ("run sets loaded: " + results.length);
	allRunSets = results;
	allConfigNames = uniqArray (allRunSets.map (function (o) { return o.get ('configName'); }));
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
    }

    xamarinPerformanceStart = start;
}) ();

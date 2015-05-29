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

	    if (this.startupRunSetIds !== undefined)
		this.startupRunSetIds.forEach (this.addNewRunSetSelector.bind (this));
	    else
		this.addNewRunSetSelector ();
	}

	benchmarkNameForId (id) {
	    for (var i = 0; i < this.allBenchmarks.length; ++i) {
		if (this.allBenchmarks [i].id == id)
		    return this.allBenchmarks [i].get ('name');
	    }
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

    class RunSetSelector {
	constructor (controller, runSetId) {
	    this.controller = controller;

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

	    var names = this.controller.allMachines.map (o => o.get ('name'));
	    populateSelect (this.machineSelect, names);
	    populateSelect (this.configSelect, this.controller.allConfigNames);

	    if (runSet !== undefined) {
		var machineId = runSet.get ('machine').id;
		var machineIndex = findIndex (this.controller.allMachines, m => m.id === machineId);
		this.machineSelect.selectedIndex = machineIndex;

		var configName = runSet.get ('configName');
		var configIndex = this.controller.allConfigNames.indexOf (configName);
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

	    var machine = this.controller.allMachines [machineIndex];
	    var configName = this.controller.allConfigNames [configIndex];

	    this.filteredRunSets = this.controller.runSetsForMachineAndConfig (machine, configName);

	    populateSelect (this.runSetSelect, this.filteredRunSets.map (o => o.get ('startedAt')));
	}

	runSetSelected () {
	    this.updateDescription ();
	    this.controller.runSetChanged (this);
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

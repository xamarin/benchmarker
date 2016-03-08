"use strict";

import * as xp_utils from './utils.ts';

export class DBObject {
	private data: Object;
	private prefix: string;

	constructor (data: Object, prefix: string = '') {
		this.data = data;
		this.prefix = prefix;
	}

	public get (key: string) : any {
		var result = this.data [this.prefix + key.toLowerCase ()];
		if (result === null)
			return undefined;
		return result;
	}
}

export class DBRunSet extends DBObject {
	public machine: DBObject;
	public config: DBObject;
	public commit: DBObject;

	constructor (data: Object, prefix: string, machine: DBObject, config: DBObject, commit: DBObject) {
		super (data, prefix);
		this.machine = machine;
		this.config = config;
		this.commit = commit;
	}
}

type ErrorFunc = (err: any) => void;

export function fetch (query: string, success: (results: Array<Object>) => void, error: ErrorFunc) : XMLHttpRequest {
	return fetchWithHeaders (query, {}, success, error);
}

const serverUrl = 'http://performancebot.mono-project.com:81/';

export function fetchWithHeaders (
        query: string,
        headers: Object, success: (results: Array<Object>) => void,
        error: ErrorFunc,
        timeout: number = 1000) : XMLHttpRequest {
	const request = new XMLHttpRequest();
	const url = serverUrl + query;

	request.onreadystatechange = function () : void {
		if (this.readyState !== 4)
			return;

		if (!(this.status >= 200 && this.status < 300)) {
			console.log ("database fetch failed (" + this.status.toString () + ") - retrying after " + timeout + "ms");
            setTimeout(() => fetchWithHeaders (query, headers, success, error, timeout * 2), timeout);
			return;
		}

		success (JSON.parse (request.responseText));
	};

	request.open('GET', url, true);
	Object.keys (headers).forEach ((header: string) => request.setRequestHeader (header, headers [header]));
	request.send();
	return request;
}

export function fetchAndWrap (query: string, success: (results: Array<DBObject>) => void, error: ErrorFunc) : XMLHttpRequest {
	return fetch (query, (results: Array<Object>) => {
		var objs = results.map ((data: Object) => new DBObject (data));
		success (objs);
	}, error);
}

export interface RunSetCount {
	machine: DBObject;
	config: DBObject;
	metric: string;
	ids: Array<number>;
	count: number;
}

export function fetchRunSetCounts (success: (results: Array<RunSetCount>) => void , error: ErrorFunc) : XMLHttpRequest {
	return fetch ('runsetcount',
		(objs: Array<Object>) => {
			var results = objs.map ((r: Object) => {
				var machine = new DBObject (r, 'm_');
				var config = new DBObject (r, 'cfg_');
				var ids = r ['ids'];
				var metric = r ['metric'];
				return { machine: machine, config: config, metric: metric, ids: ids, count: ids.length };
			});
			success (results);
		}, error);
}

export function combineRunSetCountsAcrossMetric (runSetCounts: Array<RunSetCount>) : Array<RunSetCount> {
	var dict = {};
	runSetCounts.forEach ((rsc: RunSetCount) => {
		var key = rsc.machine.get ('name') + '+' + rsc.config.get ('name');
		if (!(key in dict))
			dict [key] = [];
		dict [key] = dict [key].concat ([rsc]);
	});
	var newList = [];
	Object.keys (dict).forEach ((k: string) => {
		var rscs = dict [k];
		var rsc = xp_utils.shallowClone (rscs [0]);
		for (var i = 1; i < rscs.length; ++i)
			rsc.ids = rsc.ids.concat (rscs [i].ids);
		rsc.metric = undefined;
		rsc.ids = xp_utils.uniqStringArray (rsc.ids);
		rsc.count = rsc.ids.length;
		newList.push (rsc);
	});
	return newList;
}

export function findRunSetCount (runSetCounts: Array<RunSetCount>, machineName: string, configName: string, metric: string) : RunSetCount {
	return xp_utils.find (runSetCounts, (rsc: RunSetCount) => {
		return rsc.machine.get ('name') === machineName &&
			rsc.config.get ('name') === configName &&
			rsc.metric === metric;
	});
}

export type BenchmarkValues = { [benchmark: string]: number };
export type Summary = { runSet: DBRunSet, averages: BenchmarkValues, variances: BenchmarkValues };

export function fetchSummaries (
		machine: DBObject,
		config: DBObject,
		metric: string,
		success: (results: Array<Summary>) => void,
		error: ErrorFunc) : XMLHttpRequest {
	const machineName = machine.get ('name');
	const configName = config.get ('name');
	return fetch ('summary?metric=eq.' + metric + '&rs_pullrequest=is.null&rs_machine=eq.' + machineName + '&rs_config=eq.' + configName,
		(objs: Array<Object>) => {
			var results = [];
			objs.forEach ((r: Object) => {
				r ['c_commitdate'] = new Date (r ['c_commitdate']);
				r ['rs_startedat'] = new Date (r ['rs_startedat']);
				results.push ({
					runSet: new DBRunSet (r, 'rs_', new DBObject (r, 'm_'), new DBObject (r, 'cfg_'), new DBObject (r, 'c_')),
					averages: r ['averages'],
					variances: r ['variances'],
				});
			});
			success (results);
		}, error);
}

function fixDates (r: Object) : void {
	r ['c_commitdate'] = new Date (r ['c_commitdate']);
	r ['rs_startedat'] = new Date (r ['rs_startedat']);
}

function processRunSetEntries (objs: Array<Object>) : Array<DBRunSet> {
	var results = [];
	objs.forEach ((r: Object) => {
		fixDates (r);
		results.push (new DBRunSet (r, 'rs_', new DBObject (r, 'm_'), new DBObject (r, 'cfg_'), new DBObject (r, 'c_')));
	});
	return results;
}

export function fetchRunSetsForMachineAndConfig (
		machine: DBObject,
		config: DBObject,
		success: (results: Array<DBRunSet>) => void,
		error: ErrorFunc) : XMLHttpRequest {
	return fetch ('runset?order=c_commitdate.desc&rs_machine=eq.' + machine.get ('name') + '&rs_config=eq.' + config.get ('name'),
		(objs: Array<Object>) => success (processRunSetEntries (objs)), error);
}

export function findRunSet (runSets: Array<DBRunSet>, id: number) : DBRunSet {
	return xp_utils.find (runSets, (rs: DBRunSet) => rs.get ('id') === id);
}

export function fetchRunSet (id: number, success: (rs: DBRunSet) => void, error: ErrorFunc) : XMLHttpRequest {
	return fetch ('runset?rs_id=eq.' + id,
		(objs: Array<Object>) => {
			if (objs.length === 0) {
				success (undefined);
			} else {
				success (processRunSetEntries (objs) [0]);
			}
		}, error);
}

export function fetchRunSets (ids: Array<number>, success: (results: Array<DBRunSet>) => void, error: ErrorFunc) : XMLHttpRequest {
	return fetch ('runset?rs_id=in.' + ids.join (','),
		(objs: Array<Object>) => success (processRunSetEntries (objs)), error);
}

export function fetchParseObjectIds (
		parseIds: Array<string>,
		success: (results: Array<number | string>) => void,
		error: ErrorFunc
	) : XMLHttpRequest {
	return fetch ('parseobjectid?parseid=in.' + parseIds.join (','),
		(objs: Array<Object>) => {
			var ids = [];
			var i;
			for (i = 0; i < objs.length; ++i) {
				var o = objs [i];
				var j = xp_utils.findIndex (parseIds, (id: string) => id === o ['parseid']);
				ids [j] = o ['integerkey'] || o ['varcharkey'];
			}
			for (i = 0; i < parseIds.length; ++i) {
				if (!ids [i]) {
					error ("Not all Parse IDs found.");
					return;
				}
			}
			success (ids);
		}, error);
}

export function fetchFeaturedTimelines (success: (results: Array<DBObject>) => void, error: ErrorFunc) : XMLHttpRequest {
	return fetchAndWrap ('featuredtimelines?order=name', success, error);
}

export interface ArrayResults {
	runSet: DBRunSet;
	resultArrays: Array<Array<number>>;
}

export function fetchResultArrays (
		machineName: string,
		configName: string,
		benchmarkName: string,
		metric: string,
		success: (result: Array<ArrayResults>) => void,
		error: ErrorFunc
	) : XMLHttpRequest {
	const query = 'resultarrays?rs_machine=eq.' + machineName +
		'&rs_config=eq.' + configName +
		'&benchmark=eq.' + benchmarkName +
		'&metric=eq.' + metric;
	return fetch (query,
		(objs: Array<Object>) => {
			const partitions = xp_utils.partitionArrayByString (objs, (o: Object) => o ['rs_id'].toString ());
			const results: Array<ArrayResults> = [];
			for (let id of Object.keys (partitions)) {
				const os = partitions [id];
				fixDates (os [0]);
				const runSet = new DBRunSet (os [0], 'rs_', undefined, undefined, new DBObject (os [0], 'c_'));
				const arrays = os.map ((o: Object) => o ['resultarray']);
				results.push ({ runSet: runSet, resultArrays: arrays });
			}
			success (results);
		}, error);
}

export function fetchResultArrayBenchmarks (
		machineName: string,
		configName: string,
		metric: string,
		success: (result: Array<string>) => void,
		error: ErrorFunc
	) : XMLHttpRequest {
	const query = 'resultarraybenchmarks?metric=eq.' + metric + '&machine=eq.' + machineName + '&config=eq.' + configName;
	return fetch (query,
		(objs: Array<Object>) => {
			const benchmarks: Array<string> = objs.map ((o: Object) => o ['benchmark']);
			benchmarks.sort ();
			success (benchmarks);
		}, error);
}

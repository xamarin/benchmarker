/* @flow */

"use strict";

import * as xp_utils from './utils.js';

export class DBObject {
	data: Object;
	prefix: string;

	constructor (data: Object, prefix: string = '') {
		this.data = data;
		this.prefix = prefix;
	}

	get (key: string): any {
		var result = this.data [this.prefix + key.toLowerCase ()];
		if (result === null)
			return undefined;
		return result;
	}
}

export class DBRunSet extends DBObject {
	machine: DBObject;
	config: DBObject;
	commit: DBObject;

	constructor (data: Object, prefix: string, machine: DBObject, config: DBObject, commit: DBObject) {
		super (data, prefix);
		this.machine = machine;
		this.config = config;
		this.commit = commit;
	}
}

type ErrorFunc = (err: any) => void;

export function fetch (query: string, success: (results: Array<Object>) => void, error: ErrorFunc): void {
	return fetchWithHeaders (query, {}, success, error);
}

export function fetchWithHeaders (query: string, headers: Object, success: (results: Array<Object>) => void, error: ErrorFunc): void {
	var request = new XMLHttpRequest();
	var url = 'http://performancebot.mono-project.com:81/' + query;

	request.onreadystatechange = function () {
		if (this.readyState !== 4)
			return;

		if (!(this.status >= 200 && this.status < 300)) {
			error ("database fetch failed (" + this.status.toString () + ")");
			return;
		}

		success (JSON.parse (request.responseText));
	};

	request.open('GET', url, true);
	Object.keys (headers).forEach (header => request.setRequestHeader (header, headers [header]));
	request.send();
}

export function fetchAndWrap (query: string, success: (results: Array<DBObject>) => void, error: ErrorFunc): void {
	fetch (query, results => {
		var objs = results.map (data => new DBObject (data));
		success (objs);
	}, error);
}

export type RunSetCount = {
	machine: DBObject,
	config: DBObject,
	metric: string,
	ids: Array<number>,
	count: number
};

export function fetchRunSetCounts (success: (results: Array<RunSetCount>) => void , error: ErrorFunc): void {
	fetch ('runsetcount',
		objs => {
			var results = objs.map (r => {
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
	runSetCounts.forEach (rsc => {
		var key = rsc.machine.get ('name') + '+' + rsc.config.get ('name');
		if (!(key in dict))
			dict [key] = [];
		dict [key] = dict [key].concat ([rsc]);
	});
	var newList = [];
	Object.keys (dict).forEach (k => {
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

export function findRunSetCount (runSetCounts: Array<RunSetCount>, machineName: string, configName: string, metric: string): RunSetCount | void {
	return xp_utils.find (runSetCounts, rsc => {
		return rsc.machine.get ('name') === machineName &&
			rsc.config.get ('name') === configName &&
			rsc.metric === metric;
	});
}

export type BenchmarkValues = { [benchmark: string]: number };
type Summary = { runSet: DBRunSet, averages: BenchmarkValues, variances: BenchmarkValues };

export function fetchSummaries (machine: DBObject, config: DBObject, metric: string, success: (results: Array<Summary>) => void, error: ErrorFunc): void {
	fetch ('summary?metric=eq.' + metric + '&rs_pullrequest=is.null&rs_machine=eq.' + machine.get ('name') + '&rs_config=eq.' + config.get ('name'),
		objs => {
			var results = [];
			objs.forEach (r => {
				r ['c_commitdate'] = new Date (r ['c_commitdate']);
				r ['rs_startedat'] = new Date (r ['rs_startedat']);
				results.push ({
					runSet: new DBRunSet (r, 'rs_', new DBObject (r, 'm_'), new DBObject (r, 'cfg_'), new DBObject (r, 'c_')),
					averages: r ['averages'],
					variances: r ['variances']
				});
			});
			success (results);
		}, error);
}

function processRunSetEntries (objs) {
	var results = [];
	objs.forEach (r => {
		r ['c_commitdate'] = new Date (r ['c_commitdate']);
		r ['rs_startedat'] = new Date (r ['rs_startedat']);
		results.push (new DBRunSet (r, 'rs_', new DBObject (r, 'm_'), new DBObject (r, 'cfg_'), new DBObject (r, 'c_')));
	});
	return results;
}

export function fetchRunSetsForMachineAndConfig (machine: DBObject, config: DBObject, success: (results: Array<DBRunSet>) => void, error: ErrorFunc): void {
	fetch ('runset?order=c_commitdate.desc&rs_machine=eq.' + machine.get ('name') + '&rs_config=eq.' + config.get ('name'),
		objs => success (processRunSetEntries (objs)), error);
}

export function findRunSet (runSets: Array<DBRunSet>, id: number): DBRunSet | void {
	return xp_utils.find (runSets, rs => rs.get ('id') == id);
}

export function fetchRunSet (id: number, success: (rs: DBRunSet | void) => void, error: ErrorFunc) {
	fetch ('runset?rs_id=eq.' + id,
		objs => {
			if (objs.length === 0)
				success (undefined);
			else
				success (processRunSetEntries (objs) [0]);
		}, error);
}

export function fetchRunSets (ids: Array<number>, success: (results: Array<DBRunSet>) => void, error: ErrorFunc): void {
	fetch ('runset?rs_id=in.' + ids.join (','),
		objs => success (processRunSetEntries (objs)), error);
}

export function fetchParseObjectIds (parseIds: Array<string>, success: (results: Array<number | string>) => void, error: ErrorFunc) {
	fetch ('parseobjectid?parseid=in.' + parseIds.join (','),
		objs => {
			var ids = [];
			var i;
			for (i = 0; i < objs.length; ++i) {
				var o = objs [i];
				var j = xp_utils.findIndex (parseIds, id => id === o ['parseid']);
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

export function fetchFeaturedTimelines (success: (results: Array<DBObject>) => void, error: ErrorFunc) {
	fetchAndWrap ('featuredtimelines?order=name', success, error);
}

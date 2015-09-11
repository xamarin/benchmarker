/* @flow */

"use strict";

import * as xp_utils from './utils.js';

export class DBObject {
	data: Object;
	prefix: string;

	constructor (data, prefix = '') {
		this.data = data;
		this.prefix = prefix;
	}

	get (key) {
		var result = this.data [this.prefix + key.toLowerCase ()];
		if (result === null)
			return undefined;
		return result;
	}
}

export function fetch (query, wrap, success, error) {
	var request = new XMLHttpRequest();
	var url = 'http://192.168.99.100:32777/' + query;

	request.onreadystatechange = function () {
		if (this.readyState !== 4)
			return;

		if (this.status !== 200) {
			error ("database fetch failed");
			return;
		}

        var results = JSON.parse (request.responseText);
		if (!wrap) {
			success (results);
			return;
		}

		var objs = results.map (data => new DBObject (data));
        success (objs);
	};

	request.open('GET', url, true);
	request.send();
}

export function fetchRunSetCounts (success, error) {
	fetch ('runsetcount', false,
		objs => {
			var results = objs.map (r => {
				var machine = new DBObject (r, 'm_');
				var config = new DBObject (r, 'cfg_');
				var ids = r ['ids'];
				return { machine: machine, config: config, ids: ids, count: ids.length };
			});
			success (results);
		}, error);
}

export function findRunSetCount (runSetCounts, machineName, configName) {
	return xp_utils.find (runSetCounts, rsc => {
		return rsc.machine.get ('name') === machineName &&
			rsc.config.get ('name') === configName;
	});
}

export function fetchSummaries (metric, machine, config, success, error) {
	fetch ('summary?metric=eq.' + metric + '&rs_pullrequest=is.null&rs_machine=eq.' + machine.get ('name') + '&rs_config=eq.' + config.get ('name'), false,
		objs => {
			var results = [];
			objs.forEach (r => {
				r ['c_commitdate'] = new Date (r ['c_commitdate']);
				r ['rs_startedat'] = new Date (r ['rs_startedat']);
				results.push ({
					runSet: new DBObject (r, 'rs_'),
					commit: new DBObject (r, 'c_'),
					averages: r ['averages'],
					variances: r ['variances']
				});
			});
			success (results);
		}, error);
}

function processRunSet (row) {
	row ['c_commitdate'] = new Date (row ['c_commitdate']);
	row ['rs_startedat'] = new Date (row ['rs_startedat']);
	return {
		runSet: new DBObject (row, 'rs_'),
		commit: new DBObject (row, 'c_'),
		machine: new DBObject (row, 'm_'),
		config: new DBObject (row, 'cfg_')
	};
}

export function fetchRunSets (machine, config, success, error) {
	fetch ('runset?rs_machine=eq.' + machine.get ('name') + '&rs_config=eq.' + config.get ('name'), false,
		objs => {
			var results = [];
			objs.forEach (r => { results.push (processRunSet (r)) });
			success (results);
		}, error);
}

export function findRunSet (runSetEntries, id) {
	return xp_utils.find (runSetEntries, e => e.runSet.get ('id') == id);
}

export function fetchRunSet (id, success, error) {
	fetch ('runset?rs_id=eq.' + id, false,
		objs => {
			if (objs.length === 0)
				success (undefined);
			else
				success (processRunSet (objs [0]));
		}, error);
}

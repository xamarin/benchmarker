/* @flow */

"use strict";

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
	var url = 'http://192.168.99.100:32776/' + query;

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

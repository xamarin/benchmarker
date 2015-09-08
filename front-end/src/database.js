/* @flow */

"use strict";

export class DBObject {
	data: Object;

	constructor (data) {
		this.data = data;
	}

	get (key) {
		return this.data [key.toLowerCase ()];
	}
}

export function fetch (query, success, error) {
	var request = new XMLHttpRequest();
	var url = 'http://192.168.99.100:32773/' + query;

	request.onreadystatechange = function () {
		if (this.readyState !== 4)
			return;

		if (this.status !== 200) {
			error ("database fetch failed");
			return;
		}

        var results = JSON.parse (request.responseText);
		var objs = results.map (data => new DBObject (data));
        success (objs);
	};

	request.open('GET', url, true);
	request.send();
}

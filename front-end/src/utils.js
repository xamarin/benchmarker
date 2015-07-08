/* @flow */

"use strict";

export function findIndex<T> (arr : Array<T>, f: (v: T) => boolean) : number {
    for (var i = 0; i < arr.length; ++i) {
		if (f (arr [i]))
			return i;
    }
    return -1;
}

export function find<T> (arr: Array<T>, f: (v: T) => boolean) : T {
    return arr [findIndex (arr, f)];
}

export function uniqStringArray (arr: Array<string>) : Array<string> {
    var hash = {};
    for (var i = 0; i < arr.length; ++i) {
		hash [arr [i]] = true;
    }
	return Object.keys (hash);
}

export function uniqArrayByString<T> (arr: Array<T>, keyFunc: (v: T) => string) : Array<T> {
    var hash = {};
    for (var i = 0; i < arr.length; ++i) {
		hash [keyFunc (arr [i])] = arr [i];
    }
	return Object.keys (hash).map (k => hash [k]);
}

export function histogramByString<T> (arr: Array<T>, keyFunc: (v: T) => string) : Array<[T, number]> {
    var valueHash = {};
    var countHash = {};
    for (var i = 0; i < arr.length; ++i) {
        var value = arr [i];
        var key = keyFunc (value);
        if (valueHash [key] !== undefined) {
            countHash [key] += 1;
        } else {
            valueHash [key] = value;
            countHash [key] = 1;
        }
    }
    return Object.keys (valueHash).map (k => [valueHash [k], countHash [k]]);
}

export function histogramOfStrings (arr: Array<string>) : Array<[string, number]> {
    return histogramByString (arr, x => x);
}

export function intersectArray<T> (arr: Array<T>, brr: Array<T>) : Array<T> {
    var crr = [];
    for (var i = 0; i < arr.length; ++i) {
		var a = arr [i];
		if (brr.indexOf (a) >= 0)
			crr.push (a);
    }
    return crr;
}

export function removeArrayElement<T> (arr: Array<T>, i: number) : Array<T> {
    return arr.slice (0, i).concat (arr.slice (i + 1));
}

export function updateArray<T> (arr: Array<T>, i: number, v: T) : Array<T> {
    var newArr = arr.slice ();
    newArr [i] = v;
    return newArr;
}

export function partitionArrayByString<T> (arr: Array<T>, keyFunc: (v: T) => string) : { [key: string]: Array<T> } {
	var result = {};
	for (var i = 0; i < arr.length; ++i) {
		var val = arr [i];
		var key = keyFunc (val);
		var vals = result [key];
		if (vals === undefined)
			vals = result [key] = [];
		vals.push (val);
	}
	return result;
}

export function sortArrayBy<T> (arr: Array<T>, keyFunc: (v: T) => Object) : Array<T> {
	var copy = arr.slice (0);
	copy.sort ((a, b) => {
		var ka = keyFunc (a);
		var kb = keyFunc (b);
		if (ka < kb)
			return -1;
		if (kb < ka)
			return 1;
		return 0;
	});
	return copy;
}

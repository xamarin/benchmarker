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

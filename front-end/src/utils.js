/* @flow */

"use strict";

export function findIndex<T> (arr : Array<T>, f: (v: T) => boolean) : number {
    for (var i = 0; i < arr.length; ++i) {
		if (f (arr [i]))
			return i;
    }
    return -1;
}

export function find<T> (arr: Array<T>, f: (v: T) => boolean) : T | void {
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

export function sortArrayLexicographicallyBy<T> (arr: Array<T>, keyFunc: (v: T) => string) : Array<T> {
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

export function sortArrayNumericallyBy<T> (arr: Array<T>, keyFunc: (v: T) => number) : Array<T> {
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

function padLeft (s: string, c: string, len: number) : string {
    while (s.length < len)
        s = c + s;
    return s;
}

export function formatDate (date: Date) : string {
    var monthNames = [
        "Jan", "Feb", "Mar", "Apr",
        "May", "Jun", "Jul", "Aug",
        "Sep", "Oct", "Nov", "Dec"
    ];

    var day = date.getDate ().toString ();
    var monthIndex = date.getMonth ();
    var year = date.getFullYear ().toString ();
    var hour = date.getHours ().toString ();
    var minute = date.getMinutes ().toString ();

    var dayFormatted = padLeft (day, "0", 2) + " " + monthNames[monthIndex] + " " + year;
    var timeFormatted = padLeft (hour, "0", 2) + ":" + padLeft (minute, "0", 2);

    return dayFormatted + " " + timeFormatted;
}

// http://stackoverflow.com/questions/1068834/object-comparison-in-javascript
export function deepEquals () : boolean {
    var leftChain = [], rightChain = [];

    function compare2Objects (x, y) {
        var p;

        // remember that NaN === NaN returns false
        // and isNaN(undefined) returns true
        if (isNaN(x) && isNaN(y) && typeof x === 'number' && typeof y === 'number') {
            return true;
        }

        // Compare primitives and functions.
        // Check if both arguments link to the same object.
        // Especially useful on step when comparing prototypes
        if (x === y) {
            return true;
        }

        // Works in case when functions are created in constructor.
        // Comparing dates is a common scenario. Another built-ins?
        // We can even handle functions passed across iframes
        if ((typeof x === 'function' && typeof y === 'function') ||
                (x instanceof Date && y instanceof Date) ||
                (x instanceof RegExp && y instanceof RegExp) ||
                (x instanceof String && y instanceof String) ||
                (x instanceof Number && y instanceof Number)) {
            return x.toString() === y.toString();
        }

        // At last checking prototypes as good a we can
        if (!(x instanceof Object && y instanceof Object)) {
            return false;
        }

        if (x.isPrototypeOf(y) || y.isPrototypeOf(x)) {
            return false;
        }

        if (x.constructor !== y.constructor) {
            return false;
        }

        if (x.prototype !== y.prototype) {
            return false;
        }

        // Check for infinitive linking loops
        if (leftChain.indexOf(x) > -1 || rightChain.indexOf(y) > -1) {
            return false;
        }

        // Quick checking of one object beeing a subset of another.
        // todo: cache the structure of arguments[0] for performance
        for (p in y) {
            if (y.hasOwnProperty(p) !== x.hasOwnProperty(p)) {
                return false;
            }
            else if (typeof y[p] !== typeof x[p]) {
                return false;
            }
        }

        for (p in x) {
            if (y.hasOwnProperty(p) !== x.hasOwnProperty(p)) {
                return false;
            }
            else if (typeof y[p] !== typeof x[p]) {
                return false;
            }

            switch (typeof (x[p])) {
            case 'object':
            case 'function':
                leftChain.push(x);
                rightChain.push(y);

                if (!compare2Objects (x[p], y[p])) {
                    return false;
                }

                leftChain.pop();
                rightChain.pop();
                break;

            default:
                if (x[p] !== y[p]) {
                    return false;
                }
                break;
            }
        }

        return true;
    }

    if (arguments.length < 1) {
        return true; //Die silently? Don't know how to handle such case, please help...
        // throw "Need two or more arguments to compare";
    }

    for (var i = 1, l = arguments.length; i < l; i++) {
        leftChain = []; //Todo: this can be cached
        rightChain = [];

        if (!compare2Objects (arguments[0], arguments[i])) {
            return false;
        }
    }

    return true;
}

// http://stackoverflow.com/questions/728360/most-elegant-way-to-clone-a-javascript-object
export function shallowClone<T> (toCopy: T) : T {
    var obj: any = toCopy;
    if (null == obj || "object" != typeof obj) return obj;
    var copy = obj.constructor();
    for (var attr in obj) {
        if (obj.hasOwnProperty(attr)) copy[attr] = obj[attr];
    }
    return copy;
}

// http://stackoverflow.com/questions/4459928/how-to-deep-clone-in-javascript
export function deepClone<T> (toCopy: T) : T {
    var item: any = toCopy;
    if (!item) { return item; } // null, undefined values check

    var types = [ Number, String, Boolean ];
    var result: any;

    // normalizing primitives if someone did new String('aaa'), or new Number('444');
    types.forEach(function(type) {
        if (item instanceof type) {
            result = type( item );
        }
    });

    if (typeof result == "undefined") {
        if (Object.prototype.toString.call( item ) === "[object Array]") {
            var arr: any = [];
            item.forEach(function(child, index, array) {
                arr [index] = deepClone( child );
            });
            return arr;
        } else if (typeof item == "object") {
            // testing that this is DOM
            if (item.nodeType && typeof item.cloneNode == "function") {
                var func: any = item.cloneNode( true );
                return func;
            } else if (!item.prototype) { // check that this is a literal
                if (item instanceof Date) {
                    var date: any = new Date(item);
                    return date;
                } else {
                    // it is an object literal
                    result = {};
                    for (var i in item) {
                        result[i] = deepClone( item[i] );
                    }
                }
            } else {
                result = item;
            }
        } else {
            result = item;
        }
    }

    return result;
}

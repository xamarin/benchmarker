var xamarin_utils = {
    findIndex: function findIndex (arr, f) {
	for (var i = 0; i < arr.length; ++i) {
	    if (f (arr [i]))
		return i;
	}
	return -1;
    },

    find: function find (arr, f) {
	return arr [xamarin_utils.findIndex (arr, f)];
    },

    uniqArray: function uniqArray (arr) {
	var hash = {};
	for (var i = 0; i < arr.length; ++i) {
	    hash [arr [i]] = true;
	}
	var newArr = [];
	for (var o in hash) {
	    newArr.push (o);
	}
	return newArr;
    },

    intersectArray: function intersectArray (arr, brr) {
	var crr = [];
	for (var i = 0; i < arr.length; ++i) {
	    var a = arr [i];
	    if (brr.indexOf (a) >= 0)
		crr.push (a);
	}
	return crr;
    },

    removeArrayElement: function removeArrayElement (arr, i) {
	return arr.slice (0, i).concat (arr.slice (i + 1));
    },

    updateArray: function updateArray (arr, i, v) {
	var newArr = arr.slice ();
	newArr [i] = v;
	return newArr;
    }
}

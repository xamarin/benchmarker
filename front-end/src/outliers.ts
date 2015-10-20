/* @flow */

"use strict";

function minBy (f: (x: number) => number, x: number, y: number): number {
	return Math.min (f (x), f (y));
}

export function outlierVariance (samples: Array<number>): string {
	return computeOutlierVariance (
		jackknife (samples, computeMean),
		jackknife (samples, computeStandardDeviation),
		samples.length);
}

function computeMean (samples: Array<number>): number {
	return samples.reduce ((x: number, y: number) => x + y, 0) / samples.length;
}

function computeStandardDeviation (samples: Array<number>): number {
	var mean = computeMean (samples);
	var n = samples.length;
	return Math.sqrt (samples.reduce ((sum: number, x: number) => sum + (x - mean) * (x - mean), 0) / n);
}

function jackknife (samples: Array<number>, estimate: ((xs: Array<number>) => number)): number {
	var n = samples.length;
	var resampled = [];
	for (var i = 0; i < n; ++i)
		resampled.push (estimate (samples.slice (0, i).concat (samples.slice (i + 1))));
	return computeMean (resampled);
}

/* Given the bootstrap estimate of the mean (m) and of the standard deviation
 * (sb), as well as the original number of samples (n), computes the extent to
 * which outliers in the sample data affected the mean and standard deviation.
 */
function computeOutlierVariance (
	mean: number /* jackknife estimate of mean */,
	stdDev: number /* jackknife estimate of standard deviation */,
	n: number /* number of samples */
): string /* report */ {
	var variance = stdDev * stdDev;
	var mn = mean / n;
	var mgMin = mn / 2;
	var sampledStdDev = Math.min (mgMin / 4, stdDev / Math.sqrt (n));
	var sampledVariance = sampledStdDev * sampledStdDev;
	var outlierVariance = function (x: number) : number {
		var delta = n - x;
		return (delta / n) * (variance - delta * sampledVariance);
	};
	var cMax = function (x: number) : number {
		var k = mn - x;
		var d = k * k;
		var nd = n * d;
		var k0 = -n * nd;
		var k1 = variance - n * sampledVariance + nd;
		var det = k1 * k1 - 4 * sampledVariance * k0;
		return Math.floor (-2 * k0 / (k1 + Math.sqrt (det)));
	};
	var result = minBy (outlierVariance, 1, minBy (cMax, 0, mgMin)) / variance;
	if (isNaN (result))
		result = 0;
	return result < 0.01 ? 'none'
		: result < 0.10 ? 'slight'
		: result < 0.50 ? 'moderate'
		: 'severe';
}

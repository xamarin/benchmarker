"use strict";

import * as Database from './database.ts';

export type GCPause = {
    start: number;
    duration: number;
}

export type BenchmarkResults = {
    aggregate: {[metric: string]: number};
    individual: {[metric: string]: Array<number>};
    gcPauses: Array<Array<GCPause>>;
    // FIXME: this shouldn't be in here
    disabled: boolean;
}

function newBenchmarkResults (disabled: boolean) : BenchmarkResults {
    return { aggregate: {}, individual: {}, gcPauses: [], disabled: disabled};
}

export type ResultsByBenchmark = {[benchmark: string]: BenchmarkResults};

type TimeSliceCount = {
    total: number;
    failed: number;
};

// We define a time slice as failed if its mutator utilization is below
// a certain threshold.  For now we've hard-coded a slice length of 100ms
// and an acceptable fraction of 50%.
function computeTimeSlices (starts: Array<number>, times: Array<number>) : TimeSliceCount {
    const sliceLength = 100;
    const acceptableFraction = 0.5;

    if (starts.length !== times.length) {
        console.log ("Error: starts and times have different lengths");
        return { total: 0, failed: 0 };
    }

    let slice = 0;
    let failed = 0;

    let i = 0;
    while (i < starts.length) {
        const sliceStart = slice * sliceLength;
        const sliceEnd = sliceStart + sliceLength;

        slice++;

        // FIXME: optimize
        if (sliceEnd <= starts [i]) {
            continue;
        }

        let pauseTime = 0;
        while (i < starts.length && starts [i] < sliceEnd) {
            const pauseEnd = starts [i] + times [i];
            pauseTime += Math.min (sliceEnd, pauseEnd) - Math.max (sliceStart, starts [i]);
            i++;
        }

        const mutatorTime = sliceLength - pauseTime;

        if (mutatorTime < sliceLength * acceptableFraction) {
            failed++;
        }
    }

    return { total: slice, failed: failed };
}

export function metricIsAggregate (metric: string) : boolean {
    return metric === 'acceptable-time-slices';
}

type GCPauseTimesEntry = {
    id: string;
    benchmark: string;
    starts: Array<number>;
    times: Array<number>;
};

export class Data {
    private runSet: Database.DBRunSet;

    private results: Array<Object>;
    private resultArrays: Array<Object>;

    private requests: Array<XMLHttpRequest>;

    private byBenchmark: ResultsByBenchmark;
    private metricsArray: Array<string>;
    private needsRecompute: boolean;

    constructor (runSet: Database.DBRunSet, update: (data: Data) => void, error: (error: Object) => void) {
        this.runSet = runSet;

        this.results = undefined;
        this.resultArrays = undefined;

        this.requests = [];

        this.needsRecompute = false;

        this.requests.push (Database.fetch ('results?runset=eq.' + runSet.get ('id'),
			(objs: Array<Object>) => {
                this.results = objs;
                this.needsRecompute = true;
                update (this);
			}, error));
        this.requests.push (Database.fetch ('resultarrays?rs_id=eq.' + runSet.get ('id'),
            (objs: Array<Object>) => {
                this.resultArrays = objs;
                this.needsRecompute = true;
                update (this);
            }, error));
    }

    public abortFetch () : void {
        this.requests.forEach ((r: XMLHttpRequest) => r.abort ());
    }

    public hasResults () : boolean {
        return this.results !== undefined || this.resultArrays !== undefined;
    }

    private recompute () : void {
        if (!this.needsRecompute) {
            return;
        }
        this.needsRecompute = false;

        const resultsByBenchmark: ResultsByBenchmark = {};

        var metricsDict: {[metric: string]: Object} = {};
        for (let i = 0; i < this.results.length; ++i) {
            let result = this.results [i];
            const benchmark = result ['benchmark'];
            const metric = result ['metric'];
            const results = resultsByBenchmark [benchmark] || newBenchmarkResults (result ['disabled'] as boolean);
            results.individual [metric] = result ['results'];
            resultsByBenchmark [benchmark] = results;

            metricsDict [metric] = {};
        }
        if (this.resultArrays !== undefined) {
            const runs: {[id: string]: GCPauseTimesEntry} = {};
            for (let i = 0; i < this.resultArrays.length; ++i) {
                const row = this.resultArrays [i];
                const id = row ['r_id'].toString ();
                const entry = runs [id] || { id: id, benchmark: row ['benchmark'], starts: undefined, times: undefined };
                const metric = row ['metric'];
                if (metric === 'pause-starts') {
                    entry.starts = row ['resultarray'];
                } else if (metric === 'pause-times') {
                    entry.times = row ['resultarray'];
                } else {
                    continue;
                }
                runs [id] = entry;
            }
            const entries = Object.keys (runs).map ((id: string) => runs [id]);
            const timeSlicesByBenchmark: {[benchmark: string]: TimeSliceCount} = {};
            entries.forEach ((entry: GCPauseTimesEntry) => {
                if (entry.starts === undefined || entry.times === undefined) {
                    return;
                }
                if (entry.starts.length !== entry.times.length) {
                    console.log ("Error: starts and times have different lengths");
                    return;
                }
                // FIXME: set disabled to proper value (it's not in the DB view yet)
                const results = resultsByBenchmark [entry.benchmark] || newBenchmarkResults (false);
                results.gcPauses.push (entry.starts.map ((s: number, i: number) => { return { start: s, duration: entry.times [i] }; }));
                resultsByBenchmark [entry.benchmark] = results;

                const timeSlices = timeSlicesByBenchmark [entry.benchmark] || { total: 0, failed: 0 };
                const {total, failed} = computeTimeSlices (entry.starts, entry.times);
                timeSlices.total += total;
                timeSlices.failed += failed;
                timeSlicesByBenchmark [entry.benchmark] = timeSlices;
            });
            Object.keys (timeSlicesByBenchmark).forEach ((benchmark: string) => {
                const {total, failed} = timeSlicesByBenchmark [benchmark];
                const percentage = parseFloat (((total - failed) / total * 100).toPrecision (3));
                resultsByBenchmark [benchmark].aggregate ['acceptable-time-slices'] = percentage;
                metricsDict ['acceptable-time-slices'] = {};
            });
        }

        this.byBenchmark = resultsByBenchmark;
        this.metricsArray = Object.keys (metricsDict);
        this.metricsArray.sort ();
    }

    public resultsByBenchmark (runSet: Database.DBRunSet) : ResultsByBenchmark {
        this.recompute ();
        return this.byBenchmark;
    }

    public metrics () : Array<string> {
        this.recompute ();
        return this.metricsArray;
    }
}

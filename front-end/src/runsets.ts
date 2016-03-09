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

type RunSetResults = {
    results: Array<Object>;
    resultArrays: Array<Object>;

    byBenchmark: ResultsByBenchmark;
    needsRecompute: boolean;
};

export class Data {
    private runSets: Array<Database.DBRunSet>;
    private results: {[runSetId: string]: RunSetResults};

    private requests: Array<XMLHttpRequest>;

    private metricsDict: {[metric: string]: Object};
    private benchmarksDict: {[benchmark: string]: Object};

    constructor (runSets: Array<Database.DBRunSet>, update: (data: Data) => void, error: (error: Object) => void) {
        this.runSets = runSets;
        this.results = {};
        this.requests = [];
        this.metricsDict = {};
        this.benchmarksDict = {};

        runSets.forEach ((runSet: Database.DBRunSet) => {
            const results: RunSetResults = { results: undefined, resultArrays: undefined, byBenchmark: {}, needsRecompute: false };
            this.results [runSet.get ('id')] = results;

            this.requests.push (Database.fetch ('results?runset=eq.' + runSet.get ('id'),
                (objs: Array<Object>) => {
                    results.results = objs;
                    results.needsRecompute = true;
                    update (this);
                }, error));
            this.requests.push (Database.fetch ('resultarrays?rs_id=eq.' + runSet.get ('id'),
                (objs: Array<Object>) => {
                    results.resultArrays = objs;
                    results.needsRecompute = true;
                    update (this);
                }, error));
        });
    }

    public abortFetch () : void {
        this.requests.forEach ((r: XMLHttpRequest) => r.abort ());
    }

    public hasResults () : boolean {
        this.recompute ();
        return Object.keys (this.metricsDict).length !== 0;
    }

    private recomputeResults (rsr: RunSetResults) : void {
        if (!rsr.needsRecompute) {
            return;
        }
        rsr.needsRecompute = false;

        const resultsByBenchmark: ResultsByBenchmark = {};

        for (let i = 0; i < rsr.results.length; ++i) {
            let result = rsr.results [i];
            const benchmark = result ['benchmark'];
            const metric = result ['metric'];
            const results = resultsByBenchmark [benchmark] || newBenchmarkResults (result ['disabled'] as boolean);
            results.individual [metric] = result ['results'];
            resultsByBenchmark [benchmark] = results;

            this.metricsDict [metric] = {};
            this.benchmarksDict [benchmark] = {};
        }
        if (rsr.resultArrays !== undefined) {
            const runs: {[id: string]: GCPauseTimesEntry} = {};
            for (let i = 0; i < rsr.resultArrays.length; ++i) {
                const row = rsr.resultArrays [i];
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
                this.benchmarksDict [entry.benchmark] = {};

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
                this.metricsDict ['acceptable-time-slices'] = {};
            });
        }

        rsr.byBenchmark = resultsByBenchmark;
    }

    private recompute () : void {
        this.runSets.forEach ((runSet: Database.DBRunSet) => {
            this.recomputeResults (this.results [runSet.get ('id')]);
        });
    }

    public resultsForRunSetAndBenchmark (runSet: Database.DBRunSet, benchmark: string) : BenchmarkResults {
        this.recompute ();
        return this.results [runSet.get ('id')].byBenchmark [benchmark];
    }

    public benchmarks () : Array<string> {
        this.recompute ();
        const benchmarksArray = Object.keys (this.benchmarksDict);
        benchmarksArray.sort ();
        return benchmarksArray;
    }

    public metrics () : Array<string> {
        this.recompute ();
        const metricsArray = Object.keys (this.metricsDict);
        metricsArray.sort ();
        return metricsArray;
    }
}

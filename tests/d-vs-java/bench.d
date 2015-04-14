/**
bench.d -
Allocation/Garbage collection benchmark for D
*/

import std.stdio;
import std.datetime;
import std.conv;

ulong[] ascendingAlloc(int n, int max_size, bool warmup)
{
    // setup timer
    StopWatch sw;
    TickDuration[] times;
    times.length = n;
    TickDuration last = TickDuration.from!"seconds"(0);
    sw.start();

    // microbenchmark code
    ulong[] lens;
    for (auto i = 0; i < n; i++)
    {
       double[][] x;
       auto ii = 0;
       while (ii++ < max_size)
       {
           double[] y;
           y.length = ii;
           x ~= y;
       }

       lens ~= x.length;

       ii = max_size;
       while (ii-- > 1)
       {
           x.length = ii;
       }
       x.length = 0;
    }

    // stop timer
    sw.stop();
    auto t = sw.peek() - last;

    // output results
    if (warmup == false)
        writeln("ascendingAllocD,", t.msecs);

    return lens;
}

ulong[] descendingAlloc(int n, int max_size, bool warmup)
{
    // setup timer
    StopWatch sw;
    TickDuration[] times;
    times.length = n;
    TickDuration last = TickDuration.from!"seconds"(0);
    sw.start();

     // microbenchmark code
    ulong[] lens;
    for (auto i = 0; i < n; i++)
    {
       double[][] x;
       auto ii = max_size;
       while (ii-- > 1)
       {
           double[] y;
           y.length = ii;
           x ~= y;
       }

       lens ~= x.length;

       ii = max_size;
       while (ii-- > 1)
       {
           x.length = ii;
       }

       x.length = 0;
    }

    // stop timer
    sw.stop();
    auto t = sw.peek() - last;

    // output results
    if (warmup == false)
        writeln("descendingAllocD,", t.msecs);
    return lens;
}

ulong[] alternatingAlloc(int n, int max_size, bool warmup)
{
    // setup timer
    StopWatch sw;
    TickDuration[] times;
    times.length = n;
    TickDuration last = TickDuration.from!"seconds"(0);
    sw.start();

    // microbenchmark code
    ulong[] lens;

    for (auto i = 0; i < n; i++)
    {
       double[][] x;
       auto ii = 0;
       int l1 = max_size / 2;
       int l2 = 0;
       int limit = l1;
       while (ii++ < limit)
       {
           double[] y;
           double[] y2;
           y.length = ++l1;
           y2.length = ++l2;
           x ~= y;
           x ~= y2;
       }

       lens ~= x.length;

       ii = limit;
       while (ii-- > 1)
       {
           x.length = ii;
       }
       x.length = 0;
    }

    // stop timer
    sw.stop();
    auto t = sw.peek() - last;

    // output results
    if (warmup == false)
        writeln("alternatingAllocD,", t.msecs);

    return lens;
}


void main(string[] args)
{
    auto bench_iters = to!int(args[1]);
    auto times = to!int(args[2]);
    auto max_size = to!int(args[3]);


    //warmup
    auto warmup = ascendingAlloc(times, max_size, true);
    warmup = ascendingAlloc(times, max_size, true);
    warmup = alternatingAlloc(times, max_size, true);
    warmup = ascendingAlloc(times, max_size, true);
    warmup = ascendingAlloc(times, max_size, true);
    warmup = alternatingAlloc(times, max_size, true);

    // run benchmarks
    while (bench_iters-- > 0)
     {
         auto result = ascendingAlloc(times, max_size, false);
         auto result2 = descendingAlloc(times, max_size, false);
         auto result3 = alternatingAlloc(times, max_size, false);
         auto result4 = alternatingAlloc(times, max_size, false);
         auto result5 = descendingAlloc(times, max_size, false);
         auto result6 = ascendingAlloc(times, max_size, false);
         auto result7 = ascendingAlloc(times, max_size, false);
         auto result8 = descendingAlloc(times, max_size, false);
         auto result9 = alternatingAlloc(times, max_size, false);
         max_size += 1000;
     }
}

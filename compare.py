#!/usr/bin/env python
import sys
import os
import colorsys
import numpy as np
import matplotlib
import re
from optparse import OptionParser
import math

def unique (l):
    met = set()
    return [x for x in l if x not in met and not met.add(x)]

parser = OptionParser ()
parser.add_option ("-o", "--output", dest = "output", help = "output graph to FILE", metavar = "FILE")
parser.add_option ("-i", "--include", action = "append", dest = "include", help = "only include BENCHMARK", metavar = "BENCHMARK")
parser.add_option ("-j", "--subtract-jit-time", action = "store_true", dest = "subtract_jit", default = False, help = "subtract JIT times from run times")
parser.add_option ("-c", "--counter", dest = "counter", help = "compare values of COUNTER", metavar = "COUNTER")
parser.add_option ("-m", "--minimum", dest = "minimum", help = "only include benchmarks where reference value is at least VALUE", metavar = "VALUE")
parser.add_option ("-g", "--geomean", action = "store_true", dest = "geomean", default = False, help = "Output geometric mean of the relative execution speed for each config.")

(options, configs) = parser.parse_args ()

configs = unique(configs)

if options.output:
    matplotlib.use('Agg')
    matplotlib.rcParams.update({'font.size': 8})

include = None
if options.include:
    include = set (options.include)

if options.counter and options.subtract_jit:
    print "Error: Can't use both --counter and --subtract-jit-time."
    sys.exit (1)

minimum_value = None
if options.minimum:
    minimum_value = float (options.minimum)

import matplotlib.pyplot as plt

def number (s):
    try:
        return int (s)
    except ValueError:
        return float (s)

def grep_stats (filename, statname):
    if not os.path.isfile (filename):
        return None
    for line in open (filename).readlines ():
        m = re.match ('([^:]+[^ \t])\s*:\s*([0-9.,]+)', line)
        if m and m.group (1) == statname:
            return number (m.group (2).replace(',','.'))
    return None

def normalized_rgb (r, g, b):
    return (r / 255.0, g / 255.0, b / 255.0)

def make_colors (n):
    if n > 4:
        return [colorsys.hsv_to_rgb (float (i) / n, 1.0, 1.0) for i in range (n)]
    colors = [(119, 208, 101), (180, 85, 182), (52, 152, 219), (44, 62, 80)]
    return [normalized_rgb (r, g, b) for (r, g, b) in colors] [:n]

def list_mean (l):
    return sum (l) / len (l)

benchmarks = set ()
data = {}

for arg in configs:
    data [arg] = {}
    if options.counter:
        suffix = '.stats'
    else:
        suffix = '.times'
    files = filter (lambda x: x.endswith (suffix), os.listdir (arg))
    for filename in files:
        name = filename [:-len (suffix)]
        filepath = '%s/%s' % (arg, filename)

        if include and not name in include:
            continue

        if options.subtract_jit:
            if name.startswith ('ironjs') or name.startswith ('scimark'):
                print "Can't subtract JIT time in benchmark %s - removing." % name
                continue
            jit_time = grep_stats ('%s/%s.stats' % (arg, name), 'Total time spent JITting (sec)')
            if not jit_time:
                print ("Can't get JIT time for %s/%s - removing." % (arg, name))
                continue
        else:
            jit_time = 0

        benchmarks.add (name)
        samples = []

        if options.counter:
            sample = grep_stats (filepath, options.counter)
            if sample == None:
                print "Error: Counter `%s` is not present in stats file `%s`." % (options.counter, filepath)
                sys.exit (1)
            samples.append (float (sample))
        else:
            for time in open (filepath).readlines ():
                time = float (time.strip ())
                samples.append (time - jit_time)

        if len (samples) >= 10:
            samples = samples [2 : -2]
        elif len (samples) >= 5:
            samples = samples [1 : -1]
        data [arg] [name] = samples

# remove benchmarks not in every config

for bench in benchmarks.copy ():
    if len (filter (lambda c: bench not in data [c], configs)) > 0:
        print "Don't have data for %s in all configurations - removing." % bench
        benchmarks.remove (bench)
    elif minimum_value != None and list_mean (data [configs [0]] [bench]) < minimum_value:
        print "Mean value for %s is below minimum - removing." % bench
        benchmarks.remove (bench)

benchmarks = list (benchmarks)
benchmarks.sort ()

# calculate means and errors

processed = {}

for config in configs:
    means = []
    errs = []
    for bench in benchmarks:
        times = data [config] [bench]
        times.sort ()
        mean = list_mean (times)
        means.append (mean)
        errs.append (mean - times [0])

    processed [config] = (means, errs)

# normalize

(nmeans, nerrs) = processed [configs [0]]
for config in configs [1 :]:
    (means, errs) = processed [config]
    for i in range (len (benchmarks)):
        means [i] = means [i] / nmeans [i]
        errs [i] = errs [i] / nmeans [i]
for i in range (len (benchmarks)):
    nerrs [i] = nerrs [i] / nmeans [i]
    nmeans [i] = 1.0

if options.geomean:
    # calculate geometric mean
    processed[configs[0]][0].append(1.)
    processed[configs[0]][1].append(0)
    for config in configs[1 :]:
        gmean = 1
        (means, errs) = processed [config]
        for i in range (len (benchmarks)):
            gmean *= means [i]
        gmean = math.pow (gmean, 1. / len(benchmarks))
        means.append(gmean)
        errs.append(0)
    benchmarks.append("Geometric mean")

    # output geometric mean in the console
    print
    print "Relative execution time compared to the base config (the lower the faster):"
    for config in configs:
        print "%s: %f%%" % (config, processed[config][0][-1] * 100)

# plot

bars_width = 0.6                        # the width of all bars for one benchmark combined
xoff = (1.0 - bars_width) / 2.0
ind = np.arange (len (benchmarks))      # the x locations for the groups
width = bars_width / len (configs)      # the width of the bars

fig = plt.figure()
fig.patch.set_facecolor (normalized_rgb (180, 188, 188))
ax = fig.add_subplot(111, axisbg = 'white')
rects = []

colors = make_colors (len (configs))

min_y = 1.0
max_y = 1.0

def register_min_max (mean, err):
    global min_y, max_y

    if mean - err < min_y:
        min_y = mean - err
    if mean + err > max_y:
        max_y = mean + err

i = 0
for config in configs:
    (means, errs) = processed [config]

    for j in range (len (means)):
        register_min_max (means [j], errs [j])

    if options.counter:
        errs = None
    plot = ax.bar (ind + xoff + i * width, means, width, yerr = errs, color = colors [i], linewidth = 0)
    rects.append (plot [0])

    i = i + 1

ax.set_xlim (-xoff, len (benchmarks) + xoff)

delta_y = max_y - min_y
ax.set_ylim (min_y - delta_y * 0.1, max_y + delta_y * 0.1)

# add some
ax.set_xticks (ind + xoff + (i - 0.5) * width)
ax.set_xticklabels (benchmarks)
ax.xaxis.set_tick_params (width = 0)

if options.counter:
    ax.set_ylabel ('relative %s' % options.counter)
else:
    ax.set_ylabel ('relative wall clock time')

ax.legend (rects, configs, loc='best', fontsize='medium', labelspacing=0.2).draggable()

fig.autofmt_xdate ()

if options.output:
    fig.savefig (options.output, dpi = 200, facecolor = fig.get_facecolor(), edgecolor = 'none')
else:
    plt.show()

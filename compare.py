#!/usr/bin/env python
import sys
import os
import colorsys
import numpy as np
import matplotlib.pyplot as plt
from optparse import OptionParser

def make_colors (n):
    return [colorsys.hsv_to_rgb (float (i) / n, 1.0, 1.0) for i in range (n)]

parser = OptionParser ()
parser.add_option ("-o", "--output", dest = "output", help = "output graph to FILE", metavar = "FILE")

(options, configs) = parser.parse_args ()

benchmarks = set ()
data = {}

for arg in configs:
    data [arg] = {}
    files = filter (lambda x: x.endswith ('.times'), os.listdir (arg))
    for name in files:
        benchmarks.add (name)
        times = []
        for time in open ('%s/%s' % (arg, name)).readlines ():
            time = float (time.strip ())
            if name.startswith ('scimark'):
                time = 10000 / time
            times.append (time)
        if len (times) >= 10:
            times = times [1 : -1]
        elif len (times) >= 5:
            times = times [2 : -2]
        data [arg] [name] = times

# remove benchmarks not in every config

for bench in benchmarks.copy ():
    if len (filter (lambda c: bench not in data [c], configs)) > 0:
        print "removing ", bench
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
        mean = sum (times) / len (times)
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


# plot

bars_width = 0.8                        # the width of all bars for one benchmark combined
xoff = (1.0 - bars_width) / 2.0
ind = np.arange (len (benchmarks))      # the x locations for the groups
width = bars_width / len (configs)      # the width of the bars

fig = plt.figure()
ax = fig.add_subplot(111)
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

    plot = ax.bar (ind + xoff + i * width, means, width, yerr = errs, color = colors [i])
    rects.append (plot [0])

    i = i + 1

ax.set_xlim (-xoff, len (benchmarks) + xoff)

delta_y = max_y - min_y
ax.set_ylim (min_y - delta_y * 0.1, max_y + delta_y * 0.1)

# add some
ax.set_xticks (ind + xoff + i * width)
ax.set_xticklabels (benchmarks)

ax.set_ylabel ('relative wall clock time')

ax.legend (rects, configs)

fig.autofmt_xdate ()

if options.output:
    fig.savefig (options.output, dpi = 100)
else:
    plt.show()

#!/usr/bin/env python
import sys
import os
import colorsys
import numpy as np
import matplotlib.pyplot as plt

def make_colors (n):
    return [colorsys.hsv_to_rgb (float (i) / n, 1.0, 1.0) for i in range (n)]

configs = sys.argv [1 :]
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

ind = np.arange (len (benchmarks))    # the x locations for the groups
width = 0.35                          # the width of the bars

fig = plt.figure()
ax = fig.add_subplot(111)
rects = []

colors = make_colors (len (configs))

i = 0
for config in configs:
    (means, errs) = processed [config]

    plot = ax.bar (ind + i * width, means, width, yerr = errs, color = colors [i])
    rects.append (plot [0])

    i = i + 1

# add some
ax.set_xticks (ind + i * width)
ax.set_xticklabels (benchmarks)

ax.legend (rects, configs)

fig.autofmt_xdate ()

plt.show()

#!/usr/bin/env python

import os
import re
from optparse import OptionParser

parser = OptionParser ()
parser.add_option ("--conc", action = "append", dest = "conc", help = "concurrent RESULTS", metavar = "RESULTS")
parser.add_option ("--non-conc", action = "append", dest = "nonconc", help = "non-concurrent RESULTS", metavar = "RESULTS")

(options, _) = parser.parse_args ()

configs = []
if options.conc:
    for config in options.conc:
        configs.append ((config, True))
if options.nonconc:
    for config in options.nonconc:
        configs.append ((config, False))

benchmarks = set ()
data = {}

for (arg, is_concurrent) in configs:
    data [arg] = {}
    files = filter (lambda x: x.endswith ('.pauses'), os.listdir (arg))
    for name in files:
        benchmarks.add (name)
        data [arg] [name] = {}
        for line in open ('%s/%s' % (arg, name)).readlines ():
            m = re.match ('^pause-time (\d+) (\d+) (\d+)', line)
            if m:
                generation = int (m.group (1))
                concurrent = int (m.group (2))
                usecs = int (m.group (3))
                if not is_concurrent or (generation == 0 or concurrent):
                    if not generation in data [arg] [name]:
                        data [arg] [name] [generation] = []
                    data [arg] [name] [generation].append (usecs)

# remove benchmarks not in every config

for bench in benchmarks.copy ():
    if len (filter (lambda (c, _): bench not in data [c], configs)) > 0:
        print "removing ", bench
        benchmarks.remove (bench)

benchmarks = list (benchmarks)
benchmarks.sort ()

for bench in benchmarks:
    for (config, _) in configs:
        for gen in data [config] [bench].keys ():
            l = sorted (data [config] [bench] [gen])
            if len (l) >= 3:
                l = l [0 : -2]
                i = len (l) / 2
                print "%s,%s,%d,%d,%d" % (bench, config, gen, l [i], l [-1])

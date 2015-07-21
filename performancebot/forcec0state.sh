#!/bin/bash

exec 3>/dev/cpu_dma_latency
echo -ne '\x00\x00\x00\x00' >&3

(while sleep 60m; do true; done) & disown

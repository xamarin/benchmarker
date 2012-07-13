ahcbench (version 1.0)

Based on an implementation of Adaptive Huffman Compression developed
by Stephen Toub (See http://www.gotdotnet.com)
Collected by Ben Zorn (Microsoft Research, zorn@microsoft.com)

Description:
ahcbench is based on compressing and uncompressing an input file using
Adaptive Huffman Compression. It is written in C#.  ahcbench is
compute-intensive, requiring a relatively small heap. There are 1267
lines of code in the source for the benchmark.

Compiler settings: ahcbench is compiled with the CLR 1.0.3705 csc.exe
compiler with the /optimize flag.

Developer: Based on an implementation of Adaptive Huffman Compression
by Stephen Toub

Compatibility: CLR 1.0.3705, Rotor 1.0 (others untested) 

Download: visit
http://research.microsoft.com/research/downloads/default.aspx

Source availability: the source code for the implementation can be
obtained at http://www.gotdotnet.com

Any questions or concerns regarding ahcbench should be directed to Ben
Zorn (zorn@microsoft.com).

_______________________________________

To invoke ahcbench:

ahcbench datafile

where datafile is any file.  ahcbench creates two files datafile.ahc,
the compressed version of datafile, and datafile.orig, the restored
original data file (compare against datafile to verify the correctness
of the compression/decompression).

The file Makefile provides a very simple makefile that can be used to
execute either the CLR version of the benchmark (make test) or the
Rotor version (make test-rotor).
_______________________________________

Inputs:

There are three sample inputs with this benchmark: input1.cs,
input2.cs, and input3.cs (correct inputs don't have to be C# source
files).  All three contain automatically generated C# code.

Rough execution time on a 2.4 GHz Pentium 4 processor with 1Gb of
memory:

CLI Implementation	Input	  	Time (sec)
CLR 1.0			input1.cs	0.6
CLR 1.0			input2.cs	3.1
CLR 1.0			input3.cs	7.2
Rotor 1.0		input1.cs	7.4
Rotor 1.0		input2.cs	36.1
Rotor 1.0		input3.cs	82.4

Notes: 
 - Rotor times are measured using the free build (env.bat free)
 - input2 and input3 are both large files
 - input2 is a subset of input3

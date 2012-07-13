lcscbench (version 1.0)
Developed by Dave Hanson and Todd Proebsting (Microsoft Research)
Collected by Ben Zorn (Microsoft Research, zorn@microsoft.com)

Description:
lcscbench is based on the front end of a compiler for C# written in C#
and uses a generalized LR (GLR) parsing algorithm.  lcscbench is
compute and memory intensive, requiring hundreds of megabytes of heap
for the largest input file provided (a C# source file with 125,000
lines of code). The source contains approximately 17,000 lines of code
if machine-generated code is not counted.

Compatibility: CLR 1.0.3705, Rotor 1.0 (others untested)

Compiler settings: lcscbench is compiled with the CLR 1.0.3705 csc.exe
compiler with the /optimize flag

Download: visit http://research.microsoft.com/research/downloads/default.aspx

Source availability: currently unavailable

Any questions or concerns regarding lcscbench should be directed to
Ben Zorn (zorn@microsoft.com).
_______________________________________

To invoke lcscbench:

lcscbench file.cs

where file.cs is a syntactically correct C# file.

The file Makefile provides a very simple makefile that can be used to execute
either the CLR version of the benchmark (nmake test) or the Rotor
version (nmake test-rotor).

The output from the 3 benchmark inputs is as follows:

input1.cs	    Counted 43219 nodes
input2.cs	    Counted 217978 nodes
input3.cs	    Counted 518172 nodes
_______________________________________

Inputs:
There are three sample inputs with this benchmark: input1, input2, and
input3.  All three contain automatically generated C# code.

Rough execution time on a 2.4 GHz Pentium 4 processor with 1Gb of
memory:

CLI Implementation	Input	  	Time (sec)
CLR 1.0			input1.cs	2.4
CLR 1.0			input2.cs	13.1
CLR 1.0			input3.cs	47.4
Rotor 1.0		input1.cs	4.2
Rotor 1.0		input2.cs	46.7
Rotor 1.0		input3.cs	206.8

Notes:
 - Rotor times are measured using the free build (env.bat free)
 - input2 and input3 are both large files
 - input2 is a subset of input3
 - input3 requires a large amount of physical memory to run (100s of megabytes)

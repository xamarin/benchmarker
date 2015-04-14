#!/bin/bash

## run the d benchmarks and java benchmarks
## results included run with ./run.sh 5 6000 1000

if [ $# -lt 3 ]
then
    echo "usage: ./run.sh (bench_iters) (times) (maxsize start)"
    exit 1;
fi

echo D
rm -f ./dresults.csv;
dmd -m64 -O -inline -noboundscheck -ofbench bench.d
./bench $* > dresults.csv;
rm -f *.o;
rm -f bench;

echo Csharp
rm -f ./csharpresults.csv
mcs bench.cs
mono bench.exe $* > csharpresults.csv
rm bench.exe

echo Java
rm -f ./javaresults.csv;
javac Bench.java;
java -server Bench $*  > javaresults.csv;
rm *.class

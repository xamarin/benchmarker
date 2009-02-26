set size ratio 0.2 1,0.5
set terminal postscript eps
set output "out.eps"
unset xtics
unset ytics
unset border
plot 'test.dat' using 1:2 w lines lt 1 lc rgb "black" lw 6 notitle,			\
     '' using 1:3 w lines lt 1 lc rgb "#008800" lw 6 notitle,	\
     '' using 1:4 w lines lt 1 lc rgb "red" lw 6 notitle

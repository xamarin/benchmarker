set size ratio 0.2 1,0.5
#set terminal png
#set output "out.png"
set terminal postscript eps
set output "out.eps"
unset xtics
unset ytics
unset border
plot 'test.dat' using 1:4:5 w filledcu notitle lt rgb "#dddddd",	\
     '' using 1:3 w lines lt 1 lc rgb "black" lw 6 notitle,			\
     'max.dat' using 1:2 lt rgb "red" pt 6 ps 6 lw 7 notitle,	\
     '' using 1:2:1 with labels offset 0,2.5 font "Helvetica,60" tc rgb "red" notitle,				\
     'min.dat' using 1:2 lt rgb "#008800" pt 6 ps 6 lw 7 notitle,		\
     '' using 1:2:1 with labels offset 0,-4.5 font "Helvetica,60" tc rgb "#008800" notitle

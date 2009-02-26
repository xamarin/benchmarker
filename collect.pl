#!/usr/bin/perl

use strict;

my $basedir = ".";

my %test_rev_data = ();
my %test_data = ();

my %revisions = ();

my %inverse_tests = ( "scimark" => 10000 );

opendir DIR, $basedir or die;
my @rev_dirs = grep /^r\d+$/, readdir DIR;
closedir DIR;

foreach my $subdir (@rev_dirs) {
    $subdir =~ /^r(\d+)$/ or die;
    my $revision = $1;

    my $dir = "$basedir/$subdir";
    opendir DIR, $dir or die;
    my @filenames = grep /\.times$/, readdir DIR;
    closedir DIR;

    foreach my $filename (@filenames) {
	$filename =~ /^(.+)\.times$/ or die;
	my $test = $1;
	my @values;

	open FILE, "<$dir/$filename" or die;
	while (<FILE>) {
	    chomp;
	    $_ =~ /^\d+(\.\d+)?$/ or die;
	    push @values, $_;
	}
	close FILE;

	@values > 0 or die;

	if (exists $inverse_tests{$test}) {
	    @values = map { $inverse_tests{$test} / $_ } @values;
	}

	my $min = $values[0];
	my $max = $values[0];
	my $sum = 0;

	foreach my $value (@values) {
	    $sum += $value;
	    $min = $value if $value < $min;
	    $max = $value if $value > $max;
	}

	my $avg = $sum / @values;

	$test_rev_data{$test}{$revision}{"min"} = $min;
	$test_rev_data{$test}{$revision}{"max"} = $max;
	$test_rev_data{$test}{$revision}{"avg"} = $avg;

	$revisions{$revision} = 1;

	open FILE, "<$dir/$test.size" or die;
	my $size = <FILE> or die;
	close FILE;

	chomp $size;
	$size =~ /^\d+$/ or die "cannot parse size for $dir/$test.size";
	$test_rev_data{$test}{$revision}{"size"} = $size;
    }
}

#compute test data
foreach my $test (keys %test_rev_data) {
    my $sum = 0;
    my $n = 0;
    my $min_rev = undef;
    my $max_rev = undef;

    foreach my $revision (keys %{$test_rev_data{$test}}) {
	my $val = $test_rev_data{$test}{$revision}{"avg"};
	$sum += $val;
	++$n;

	if (defined $min_rev) {
	    $min_rev = $revision if $val < $test_rev_data{$test}{$min_rev}{"avg"};
	    $max_rev = $revision if $val > $test_rev_data{$test}{$max_rev}{"avg"};
	} else {
	    $min_rev = $revision;
	    $max_rev = $revision;
	}
    }

    my $avg = $sum / $n;

    $test_data{$test}{"avg"} = $avg;
    $test_data{$test}{"avg_min_rev"} = $min_rev;
    $test_data{$test}{"avg_max_rev"} = $max_rev;
}

#write plot data for single tests
foreach my $test (keys %test_rev_data) {
    open FILE, ">$test.dat" or die;

    print FILE "#revision size avg min max\n";

    foreach my $revision (sort { $a <=> $b } keys %{$test_rev_data{$test}}) {
	my $size = $test_rev_data{$test}{$revision}{"size"};
	my $min = sprintf "%.2f", $test_rev_data{$test}{$revision}{"min"};
	my $max = sprintf "%.2f", $test_rev_data{$test}{$revision}{"max"};
	my $avg = sprintf "%.2f", $test_rev_data{$test}{$revision}{"avg"};
	print FILE "$revision $size $avg $min $max\n";
    }

    close FILE;

    my $avg_min_rev = $test_data{$test}{"avg_min_rev"};
    my $avg_min = $test_rev_data{$test}{$avg_min_rev}{"avg"};

    open FILE, ">$test.min.dat" or die;
    print FILE "$avg_min_rev $avg_min\n";
    close FILE;

    my $avg_max_rev = $test_data{$test}{"avg_max_rev"};;
    my $avg_max = $test_rev_data{$test}{$avg_max_rev}{"avg"};

    open FILE, ">$test.max.dat" or die;
    print FILE "$avg_max_rev $avg_max\n";
    close FILE;
}

#write plot data for combined plot
open FILE, ">combined.dat" or die;
print FILE "#revision avg min max\n";
foreach my $revision (sort { $a <=> $b } keys %revisions) {
    my $sum = 0;
    my $n = 0;
    my $min = undef;
    my $max = undef;

    foreach my $test (keys %test_rev_data) {
	if (exists $test_rev_data{$test}{$revision}) {
	    my $value = $test_rev_data{$test}{$revision}{"avg"} / $test_data{$test}{"avg"};

	    $sum += $value;
	    ++$n;

	    if (defined($min)) {
		$min = $value if $value < $min;
		$max = $value if $value > $max;
	    } else {
		$min = $value;
		$max = $value;
	    }
	}
    }

    my $avg = $sum / $n;

    printf FILE "$revision %.3f %.3f %.3f\n", $avg, $min, $max;
}
close FILE;

#write html
my @last_revs = (sort { $a <=> $b } keys %revisions) [-3 .. -1];

open FILE, ">index.html" or die;
print FILE "<html><body>\n";
print FILE "<p><img src=\"combined_large.png\">\n";
print FILE "<p><table>\n";

print FILE "<tr><td>Test</td><td>Best</td><td>Worst</td>";
foreach my $rev (@last_revs) {
    print FILE "<td>r$rev</td>";
}
print FILE "<td>Graph</td></tr>\n";

foreach my $test (sort keys %test_rev_data) {
    print FILE "<tr><td>$test</td>";

    my $avg_min_rev = $test_data{$test}{"avg_min_rev"};
    my $avg_min = $test_rev_data{$test}{$avg_min_rev}{"avg"};
    my $avg_max_rev = $test_data{$test}{"avg_max_rev"};
    my $avg_max = $test_rev_data{$test}{$avg_max_rev}{"avg"};

    printf FILE "<td>%.2f (r$avg_min_rev)</td><td>%.2f (r$avg_max_rev)</td>", $avg_min, $avg_max;

    foreach my $rev (@last_revs) {
	my $val;

	if (exists $test_rev_data{$test}{$rev}) {
	    $val = sprintf "%.2f", $test_rev_data{$test}{$rev}{"avg"};
	} else {
	    $val = "-";
	}

	print FILE "<td>$val</td>";
    }

    print FILE "<td><a href=\"$test\_large.png\"><img src=\"$test.png\"></a></td></tr>\n";
}
print FILE "</table>\n";
print FILE "</body></html>";
close FILE;

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
	    $max = $value if $value > $min;
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

    foreach my $revision (keys %{$test_rev_data{$test}}) {
	$sum += $test_rev_data{$test}{$revision}{"avg"};
	++$n;
    }

    my $avg = $sum / $n;

    $test_data{$test}{"avg"} = $avg;
}

#write plot data for single tests
foreach my $test (keys %test_rev_data) {
    open FILE, ">$test.dat" or die;

    print FILE "#revision size avg min max\n";

    my $avg_min = undef;
    my $avg_min_rev;
    my $avg_max;
    my $avg_max_rev;

    foreach my $revision (sort { $a <=> $b } keys %{$test_rev_data{$test}}) {
	my $size = $test_rev_data{$test}{$revision}{"size"};
	my $min = sprintf "%.2f", $test_rev_data{$test}{$revision}{"min"};
	my $max = sprintf "%.2f", $test_rev_data{$test}{$revision}{"max"};
	my $avg = sprintf "%.2f", $test_rev_data{$test}{$revision}{"avg"};
	print FILE "$revision $size $avg $min $max\n";

	if (defined $avg_min) {
	    if ($avg < $avg_min) {
		$avg_min = $avg;
		$avg_min_rev = $revision;
	    }
	    if ($avg < $avg_max) {
		$avg_max = $avg;
		$avg_max_rev = $revision;
	    }
	} else {
	    $avg_min = $avg;
	    $avg_min_rev = $revision;
	    $avg_max = $avg;
	    $avg_max_rev = $revision;
	}
    }

    close FILE;

    open FILE, ">$test.min.dat" or die;
    print FILE "$avg_min_rev $avg_min\n";
    close FILE;

    open FILE, ">$test.max.dat" or die;
    print FILE "$avg_max_rev $avg_max\n";
    close FILE;
}

#write plot data for combined plot
open FILE, ">combined.dat" or die;
print FILE "#revision avg min max\n";
foreach my $revision (sort keys %revisions) {
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

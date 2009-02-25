#!/usr/bin/perl

use strict;

my $basedir = ".";

my %data = ();

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

	my $min = $values[0];
	my $max = $values[0];
	my $sum = 0;

	foreach my $value (@values) {
	    $sum += $value;
	    $min = $value if $value < $min;
	    $max = $value if $value > $min;
	}

	my $avg = $sum / @values;

	$data{$test}{$revision}{"min"} = $min;
	$data{$test}{$revision}{"max"} = $max;
	$data{$test}{$revision}{"avg"} = $avg;

	open FILE, "<$dir/$test.size" or die;
	my $size = <FILE> or die;
	close FILE;

	chomp $size;
	$size =~ /^\d+$/ or die "cannot parse size for $dir/$test.size";
	$data{$test}{$revision}{"size"} = $size;
    }
}

foreach my $test (keys %data) {
    open FILE, ">$test.dat" or die;

    print FILE "#revision size avg min max\n";

    my $avg_min = undef;
    my $avg_min_rev;
    my $avg_max;
    my $avg_max_rev;

    foreach my $revision (sort { $a <=> $b } keys %{$data{$test}}) {
	my $size = $data{$test}{$revision}{"size"};
	my $min = $data{$test}{$revision}{"min"};
	my $max = $data{$test}{$revision}{"max"};
	my $avg = sprintf "%.2f", $data{$test}{$revision}{"avg"};
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

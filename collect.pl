#!/usr/bin/perl

use strict;
use Cairo;
use List::Util qw(min max);

use constant PI => 4 * atan2(1, 1);

sub make_revisions_path {
    my ($cr, $revisions, $data, $key, $moveto) = @_;
    my @revisions = @$revisions;
    my %data = %$data;

    if ($moveto) {
	$cr->move_to($revisions[0], $data{$revisions[0]}{$key});
	@revisions = @revisions[1 .. $#revisions];
    }

    foreach my $revision (@revisions) {
	$cr->line_to($revision, $data{$revision}{$key});
    }
}

sub transform_coords {
    my ($cr, $window_x, $window_y, $window_width, $window_height, $min_x, $max_x, $min_y, $max_y) = @_;

    $cr->translate($window_x, $window_y);
    $cr->scale($window_width / max(($max_x - $min_x), 0.00001),
	       - $window_height / max(($max_y - $min_y), 0.00001));
    $cr->translate(0, - ($max_y - $min_y));
    $cr->translate(-$min_x, -$min_y);
}

sub make_surface_context {
    my ($img_width, $img_height) = @_;

    my $surface = Cairo::ImageSurface->create('rgb24', $img_width, $img_height);
    my $cr = Cairo::Context->create($surface);

    $cr->set_source_rgb(1, 1, 1);
    $cr->rectangle(0, 0, $img_width, $img_height);
    $cr->fill;

    return ($surface, $cr);
}

sub compute_min_max {
    my ($data, $min_key, $max_key) = @_;
    my @revisions = sort { $a <=> $b } keys %$data;

    my $min_x = min @revisions;
    my $max_x = max @revisions;
    my $min_y = min (map { $data->{$_}{$min_key} } @revisions);
    my $max_y = max (map { $data->{$_}{$max_key} } @revisions);

    return (\@revisions, $min_x, $max_x, $min_y, $max_y);
}

sub show_text_above {
    my ($cr, $text, $x, $y, $img_width, $img_height) = @_;

    my $extents = $cr->text_extents($text);
    my ($width, $x_bearing) = ($extents->{width}, $extents->{x_bearing});

    $x -= $width / 2;
    $x -= $x_bearing;

    $x = -$x_bearing if $x < -$x_bearing;
    $x = $img_width - $x_bearing - $width if $x + $x_bearing + $width > $img_width;

    $cr->move_to($x, $y);
    $cr->show_text($text);
}

sub show_text_below {
    my ($cr, $text, $x, $y, $img_width, $img_height) = @_;

    my $extents = $cr->text_extents($text);

    $y -= $extents->{y_bearing};

    show_text_above($cr, $text, $x, $y, $img_width, $img_height);
}

sub plot_marker_circle {
    my ($cr, $x, $y, $radius, $line_width, $color_r, $color_g, $color_b) = @_;

    $cr->new_path;
    $cr->arc($x, $y, $radius, 0, 2 * PI);
    $cr->set_line_width($line_width);
    $cr->set_source_rgb($color_r, $color_g, $color_b);
    $cr->stroke;
}

sub set_font {
    my ($cr, $font_size) = @_;

    $cr->select_font_face("Sans", "normal", $font_size > 12 ? "bold" : "normal");
    $cr->set_font_size($font_size);
}

sub plot_cairo_single {
    my ($rev_data, $test_data, $min_x, $max_x, $have_min_max, $avg_key, $filename,
	$img_width, $img_height, $line_width, $marker_radius, $font_size) = @_;

    my $min_key = $have_min_max ? "min" : $avg_key;
    my $max_key = $have_min_max ? "max" : $avg_key;
    my ($revisions, $dummy_min_x, $dummy_min_y, $min_y, $max_y) =
	compute_min_max($rev_data, $min_key, $max_key);
    my ($surface, $cr) = make_surface_context($img_width, $img_height);

    my $text_distance = $marker_radius + $font_size / 5;
    my $additional_space_x = $marker_radius;
    my $additional_space_y = $text_distance + $font_size;

    my ($window_x, $window_y, $window_width, $window_height) =
	($additional_space_x, $additional_space_y,
	 $img_width - 2 * $additional_space_x, $img_height - 2 * $additional_space_y);

    #min/max
    if ($have_min_max) {
	$cr->save;
	transform_coords($cr, $window_x, $window_y, $window_width, $window_height, $min_x, $max_x, $min_y, $max_y);
	make_revisions_path($cr, $revisions, $rev_data, "min", 1);
	my @revisions_rev = reverse @$revisions;
	make_revisions_path($cr, \@revisions_rev, $rev_data, "max", 0);
	$cr->restore;

	$cr->set_source_rgb(0.7, 0.7, 0.7);
	$cr->fill;
    }

    #avg
    $cr->save;
    transform_coords($cr, $window_x, $window_y, $window_width, $window_height, $min_x, $max_x, $min_y, $max_y);
    make_revisions_path($cr, $revisions, $rev_data, $avg_key, 1);
    $cr->restore;

    $cr->set_line_width($line_width);
    $cr->set_source_rgb(0, 0, 0);
    $cr->stroke;

    #min/max markers
    set_font($cr, $font_size);

    $cr->save;
    transform_coords($cr, $window_x, $window_y, $window_width, $window_height, $min_x, $max_x, $min_y, $max_y);
    my ($avg_min_rev, $avg_max_rev) = ($test_data->{"$avg_key\_min_rev"}, $test_data->{"$avg_key\_max_rev"});
    my ($avg_min_x, $avg_min_y) = $cr->user_to_device($avg_min_rev, $rev_data->{$avg_min_rev}{$avg_key});
    my ($avg_max_x, $avg_max_y) = $cr->user_to_device($avg_max_rev, $rev_data->{$avg_max_rev}{$avg_key});
    $cr->restore;

    plot_marker_circle($cr, $avg_min_x, $avg_min_y, $marker_radius, $line_width, 0, 0.6, 0);
    show_text_below($cr, $avg_min_rev, $avg_min_x, $avg_min_y + $text_distance, $img_width, $img_height);

    plot_marker_circle($cr, $avg_max_x, $avg_max_y, $marker_radius, $line_width, 1, 0, 0);
    show_text_above($cr, $avg_max_rev, $avg_max_x, $avg_max_y - $text_distance, $img_width, $img_height);

    $cr->show_page;
    $surface->write_to_png($filename);
}

sub plot_cairo_combined {
    my ($combined_data, $filename, $img_width, $img_height, $plot_min_max,
	$line_width, $marker_radius, $font_size) = @_;

    my $min_key = $plot_min_max ? "min" : "avg";
    my $max_key = $plot_min_max ? "max" : "avg";
    my ($revisions, $min_x, $max_x, $min_y, $max_y) = compute_min_max($combined_data, $min_key, $max_key);
    my ($surface, $cr) = make_surface_context($img_width, $img_height);

    my $text_distance = $marker_radius + $font_size / 5;
    my $additional_space_x = $marker_radius;
    my $additional_space_y = $text_distance + $font_size;

    my ($window_x, $window_y, $window_width, $window_height) =
	($additional_space_x, $additional_space_y,
	 $img_width - 2 * $additional_space_x, $img_height - 2 * $additional_space_y);

    #avg
    $cr->save;
    transform_coords($cr, $window_x, $window_y, $window_width, $window_height, $min_x, $max_x, $min_y, $max_y);
    make_revisions_path($cr, $revisions, $combined_data, "avg", 1);
    $cr->restore;

    $cr->set_line_width($line_width);
    $cr->set_source_rgb(0, 0, 0);
    $cr->stroke;

    if ($plot_min_max) {
	#min
	$cr->save;
	transform_coords($cr, $window_x, $window_y, $window_width, $window_height, $min_x, $max_x, $min_y, $max_y);
	make_revisions_path($cr, $revisions, $combined_data, "min", 1);
	$cr->restore;

	$cr->set_line_width($line_width);
	$cr->set_source_rgb(0, 0.3, 0);
	$cr->stroke;

	#max
	$cr->save;
	transform_coords($cr, $window_x, $window_y, $window_width, $window_height, $min_x, $max_x, $min_y, $max_y);
	make_revisions_path($cr, $revisions, $combined_data, "max", 1);
	$cr->restore;

	$cr->set_line_width($line_width);
	$cr->set_source_rgb(0.5, 0, 0);
	$cr->stroke;
    }

    #min/max markers
    my ($min_rev, $min_avg, $max_rev, $max_avg);

    foreach my $rev (@$revisions) {
	my $val = $combined_data->{$rev}{"avg"};
	if (!defined($min_rev)) {
	    $min_rev = $max_rev = $rev;
	    $min_avg = $max_avg = $val;
	} else {
	    if ($val < $min_avg) {
		$min_rev = $rev;
		$min_avg = $val;
	    }
	    if ($val > $max_avg) {
		$max_rev = $rev;
		$max_avg = $val;
	    }
	}
    }

    set_font($cr, $font_size);

    $cr->save;
    transform_coords($cr, $window_x, $window_y, $window_width, $window_height, $min_x, $max_x, $min_y, $max_y);
    my ($avg_min_x, $avg_min_y) = $cr->user_to_device($min_rev, $min_avg);
    my ($avg_max_x, $avg_max_y) = $cr->user_to_device($max_rev, $max_avg);
    $cr->restore;

    plot_marker_circle($cr, $avg_min_x, $avg_min_y, $marker_radius, $line_width, 0, 0.6, 0);
    show_text_below($cr, $min_rev, $avg_min_x, $avg_min_y + $text_distance, $img_width, $img_height);

    plot_marker_circle($cr, $avg_max_x, $avg_max_y, $marker_radius, $line_width, 1, 0, 0);
    show_text_above($cr, $max_rev, $avg_max_x, $avg_max_y - $text_distance, $img_width, $img_height);

    $cr->show_page;
    $surface->write_to_png($filename);
}

opendir DIR, "configs" or die;
my @configs = grep { !/^\.\.?$/ && (-d "configs/$_") } readdir DIR;
closedir DIR;

my %all_combined_data = ();
my %all_test_data = ();
my %all_test_rev_data = ();

foreach my $config (@configs) {
    my $basedir = "configs/$config";

    my %test_rev_data = ();
    my %test_data = ();

    my %revisions = ();

    my %inverse_tests = ( "scimark" => 10000 );

    opendir DIR, $basedir or die;
    my @rev_dirs = grep /^r\d+$/, readdir DIR;
    closedir DIR;

    my ($first_rev, $last_rev);

    foreach my $subdir (@rev_dirs) {
	$subdir =~ /^r(\d+)$/ or die;
	my $revision = $1;

	if (!defined($first_rev)) {
	    $first_rev = $revision;
	    $last_rev = $revision;
	} else {
	    $first_rev = min($first_rev, $revision);
	    $last_rev = max($last_rev, $revision);
	}

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
		$_ =~ /^\d+(\.\d+)?$/ or die "invalid time data in $dir/$filename";
		push @values, $_;
	    }
	    close FILE;

	    @values > 0 or die;

	    if (exists $inverse_tests{$test}) {
		@values = map { $inverse_tests{$test} / $_ } @values;
	    }

	    @values = sort { $a <=> $b } @values;
	    if (@values >= 10) {
		@values = @values[2 .. $#values - 2];
	    } elsif (@values >= 5) {
		@values = @values[1 .. $#values - 1];
	    }

	    my $sum = 0;

	    foreach my $value (@values) {
		$sum += $value;
	    }

	    my $avg = $sum / @values;
	    my $min = $values[0];
	    my $max = $values[$#values];

	    $test_rev_data{$test}{$revision}{"min"} = $min;
	    $test_rev_data{$test}{$revision}{"max"} = $max;
	    $test_rev_data{$test}{$revision}{"avg"} = $avg;

	    $revisions{$revision} = 1;

	    if (-f "$dir/$test.size") {
		open FILE, "<$dir/$test.size" or die;
		my $size = <FILE> or die;
		close FILE;

		chomp $size;
		$size =~ /^\d+$/ or die "cannot parse size for $dir/$test.size";
		$test_rev_data{$test}{$revision}{"size"} = $size;
	    }
	}
    }

    $all_test_rev_data{$config} = \%test_rev_data;

    #compute test data
    foreach my $test (keys %test_rev_data) {
	my $sum = 0;
	my $n = 0;
	my $min_rev = undef;
	my $max_rev = undef;
	my $min_size_rev = undef;
	my $max_size_rev = undef;

	foreach my $revision (keys %{$test_rev_data{$test}}) {
	    my $val = $test_rev_data{$test}{$revision}{"avg"};
	    my $size = $test_rev_data{$test}{$revision}{"size"};
	    $sum += $val;
	    ++$n;

	    if (defined $min_rev) {
		$min_rev = $revision if $val < $test_rev_data{$test}{$min_rev}{"avg"};
		$max_rev = $revision if $val > $test_rev_data{$test}{$max_rev}{"avg"};
		$min_size_rev = $revision if $size < $test_rev_data{$test}{$min_size_rev}{"size"};
		$max_size_rev = $revision if $size > $test_rev_data{$test}{$max_size_rev}{"size"};
	    } else {
		$min_rev = $revision;
		$max_rev = $revision;
		$min_size_rev = $revision;
		$max_size_rev = $revision;
	    }
	}

	my $avg = $sum / $n;

	$test_data{$test}{"avg"} = $avg;
	$test_data{$test}{"avg_min_rev"} = $min_rev;
	$test_data{$test}{"avg_max_rev"} = $max_rev;
	$test_data{$test}{"size_min_rev"} = $min_size_rev;
	$test_data{$test}{"size_max_rev"} = $max_size_rev;
    }

    $all_test_data{$config} = \%test_data;

    #single test plots
    foreach my $test (keys %test_rev_data) {
	plot_cairo_single($test_rev_data{$test}, $test_data{$test}, $first_rev, $last_rev,
			  1, "avg", "$basedir/$test\_large.png", 500, 150, 2, 5, 16);
	plot_cairo_single($test_rev_data{$test}, $test_data{$test}, $first_rev, $last_rev,
			  1, "avg", "$basedir/$test.png", 150, 60, 1, 3, 8);

	plot_cairo_single($test_rev_data{$test}, $test_data{$test}, $first_rev, $last_rev,
			  0, "size", "$basedir/$test\_size_large.png", 500, 150, 2, 5, 16);
	plot_cairo_single($test_rev_data{$test}, $test_data{$test}, $first_rev, $last_rev,
			  0, "size", "$basedir/$test\_size.png", 150, 60, 1, 3, 8);
    }

    #compute combined plot data
    my %combined_data = ();

    foreach my $revision (keys %revisions) {
	my $sum = 0;
	my $n = 0;
	my ($min, $max, $min_test, $max_test);

	foreach my $test (keys %test_rev_data) {
	    if (exists $test_rev_data{$test}{$revision}) {
		my $value = $test_rev_data{$test}{$revision}{"avg"} / $test_data{$test}{"avg"};

		$sum += $value;
		++$n;

		if (defined($min)) {
		    if ($value < $min) {
			$min = $value;
			$min_test = $test;
		    }
		    if ($value > $max) {
			$max = $value;
			$max_test = $test;
		    }
		} else {
		    $min = $value;
		    $min_test = $test;
		    $max = $value;
		    $max_test = $test;
		}
	    }
	}

	my $avg = $sum / $n;

	$combined_data{$revision}{"avg"} = $avg;
	$combined_data{$revision}{"min"} = $min;
	$combined_data{$revision}{"min_test"} = $min_test;
	$combined_data{$revision}{"max"} = $max;
	$combined_data{$revision}{"max_test"} = $max_test;
    }

    $all_combined_data{$config} = \%combined_data;

    #combined plot
    plot_cairo_combined(\%combined_data, "$basedir/combined_large.png", 500, 150, 1, 2, 5, 16);
    plot_cairo_combined(\%combined_data, "$basedir/combined.png", 150, 60, 0, 1, 3, 8);

    #write html index
    my @last_revs = (sort { $a <=> $b } keys %revisions) [-3 .. -1];

    open FILE, ">$basedir/index.html" or die;
    print FILE "<html><body>\n";
    print FILE "<p><a href=\"../index.html\">All Configs</a>\n";
    print FILE "<h1>$config</h1>\n";
    print FILE "<p><img src=\"combined_large.png\">\n";
    print FILE "<p><table cellpadding=\"5\" border=\"1\" rules=\"groups\">\n";
    print FILE "<colgroup align=\"left\">\n";
    print FILE "<colgroup align=\"left\" span=\"2\">\n";
    print FILE "<colgroup align=\"left\" span=\"2\">\n";
    foreach my $rev (@last_revs) {
	print FILE "<colgroup align=\"left\" span=\"2\">\n";
    }
    print FILE "<colgroup align=\"left\">\n";
    print FILE "<colgroup align=\"left\">\n";

    print FILE "<tr><td><b>Test</b></td><td colspan=\"2\"><b>Best</b></td><td colspan=\"2\"><b>Worst</b></td>";
    foreach my $rev (@last_revs) {
	print FILE "<td colspan=\"2\"><b>r$rev</b></td>";
    }
    print FILE "<td><b>Duration</b></td><td><b>Size</b></td></tr>\n";

    foreach my $test (sort keys %test_rev_data) {
	print FILE "<tr><td><a href=\"$test.html\">$test</a></td>";

	my $avg_min_rev = $test_data{$test}{"avg_min_rev"};
	my $avg_min = $test_rev_data{$test}{$avg_min_rev}{"avg"};
	my $avg_max_rev = $test_data{$test}{"avg_max_rev"};
	my $avg_max = $test_rev_data{$test}{$avg_max_rev}{"avg"};

	printf FILE "<td>%.2f</td><td>r$avg_min_rev</td><td>%.2f</td><td>r$avg_max_rev</td>", $avg_min, $avg_max;

	foreach my $rev (@last_revs) {
	    if (exists $test_rev_data{$test}{$rev}) {
		my $val = $test_rev_data{$test}{$rev}{"avg"};
		my $percentage = $val / $avg_min * 100;
		my $color;
		if ($percentage <= 102) {
		    $color = "green";
		} elsif ($percentage <= 105) {
		    $color = "black";
		} else {
		    $color = "red";
		}
		printf FILE "<td>%.2f</td><td><font color=\"$color\">%.2f%%</font></td>", $val, $percentage;
	    } else {
		print FILE "<td colspan=\"2\">-</td>";
	    }
	}

	print FILE "<td><a href=\"$test\_large.png\"><img src=\"$test.png\" border=\"0\"></a></td>";
	print FILE "<td><a href=\"$test\_size_large.png\"><img src=\"$test\_size.png\" border=\"0\"></a></td>";
	print FILE "</tr>\n";
    }
    print FILE "</table>\n";
    print FILE "</body></html>";
    close FILE;

    #write html for tests
    foreach my $test (keys %test_rev_data) {
	open FILE, ">$basedir/$test.html" or die;

	print FILE "<html><body>\n";
	print FILE "<p><a href=\"index.html\">$config</a>\n";
	print FILE "<h1>$test on $config</h1>\n";
	print FILE "<p><img src=\"$test\_large.png\">\n";

	print FILE "<p><table cellpadding=\"5\"><tr><td><b>Revision</b></td><td><b>Average</b></td><td><b>Min</b></td><td><b>Max</b></td><td><b>Size (bytes)</b></td></tr>\n";
	foreach my $revision (sort { $a <=> $b } keys %{$test_rev_data{$test}}) {
	    my $avg = $test_rev_data{$test}{$revision}{"avg"};
	    my $min = $test_rev_data{$test}{$revision}{"min"};
	    my $max = $test_rev_data{$test}{$revision}{"max"};
	    my $size = $test_rev_data{$test}{$revision}{"size"};

	    printf FILE "<tr><td><a href=\"r$revision/$test.times\">r$revision</a></td><td>%.2f</td><td>%.2f</td><td>%.2f</td><td>$size</td></tr>\n", $avg, $min, $max;
	}
	print FILE "</table>\n";

	print FILE "</body></html>\n";

	close FILE;
    }
}

#write main index
open FILE, ">configs/index.html" or die;

print FILE "<html><body>\n";

print FILE "<table cellpadding=\"5\"><tr><td><b>Config</b></td><td><b>Last Revision</b></td><td><b>Average</b></td><td colspan=\"2\"><b>Worst</b></td><td><b>Duration</b></td></tr>\n";
foreach my $config (@configs) {
    my $combined_data = $all_combined_data{$config};
    my $test_data = $all_test_data{$config};
    my $test_rev_data = $all_test_rev_data{$config};
    my @revisions = sort { $a <=> $b } keys %$combined_data;
    my $last_revision = $revisions[-1];
    my $best_avg = min (map { $combined_data->{$_}{"avg"} } @revisions);
    my ($worst_quot, $worst_test);

    foreach my $test (keys %$test_data) {
	next unless exists $test_rev_data->{$test}{$last_revision};

	my $best = $test_rev_data->{$test}{$test_data->{$test}{"avg_min_rev"}}{"avg"};
	my $quot = $test_rev_data->{$test}{$last_revision}{"avg"} / $best;

	if (!defined($worst_quot) || $quot > $worst_quot) {
	    $worst_quot = $quot;
	    $worst_test = $test;
	}
    }

    print FILE "<tr><td><a href=\"$config/index.html\">$config</a></td>";
    print FILE "<td>r$last_revision</td>";
    printf FILE "<td>%.2f%%</td>", $combined_data->{$last_revision}{"avg"} / $best_avg * 100;
    printf FILE "<td>%.2f%%</td><td>$worst_test</td>", $worst_quot * 100;
    print FILE "<td><a href=\"$config/combined_large.png\"><img src=\"$config/combined.png\" border=\"0\"></a></td></tr>\n";
}
print FILE "</table>\n";

print FILE "</body></html>\n";

close FILE;

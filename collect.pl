#!/usr/bin/perl

use strict;
use Cairo;
use List::Util qw(min max);
use File::Basename;
use Getopt::Long;

use constant PI => 4 * atan2(1, 1);

use constant LARGE_WIDTH => 500;
use constant LARGE_HEIGHT => 150;
use constant LARGE_LINE_WIDTH => 2;
use constant LARGE_MARKER_RADIUS => 5;
use constant LARGE_FONT_SIZE => 16;

use constant SMALL_WIDTH => 150;
use constant SMALL_HEIGHT => 60;
use constant SMALL_LINE_WIDTH => 1;
use constant SMALL_MARKER_RADIUS => 3;
use constant SMALL_FONT_SIZE => 8;

use constant SCALE => 2;

sub make_revisions_path {
    my ($cr, $revisions, $rev_indexes, $data, $key, $moveto) = @_;
    my @revisions = @$revisions;
    my %data = %$data;

    if ($moveto) {
	$cr->move_to($rev_indexes->{$revisions[0]}, $data{$revisions[0]}{$key});
	@revisions = @revisions[1 .. $#revisions];
    }

    foreach my $revision (@revisions) {
	$cr->line_to($rev_indexes->{$revision}, $data{$revision}{$key});
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
    my ($data, $rev_indexes, $min_key, $max_key) = @_;
    my @revisions = sort { $a cmp $b } keys %$data;

    my $min_x = min values %$rev_indexes;
    my $max_x = max values %$rev_indexes;
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
    my ($rev_data, $test_data, $rev_indexes, $min_x_rev, $max_x_rev, $have_min_max, $avg_key, $filename, $revname,
	$img_width, $img_height, $line_width, $marker_radius, $font_size, $scale) = @_;

    $img_width *= $scale;
    $img_height *= $scale;
    $line_width *= $scale;
    $marker_radius *= $scale;
    $font_size *= $scale;

    my $min_x = $rev_indexes->{$min_x_rev};
    my $max_x = $rev_indexes->{$max_x_rev};
    my $min_key = $have_min_max ? "min" : $avg_key;
    my $max_key = $have_min_max ? "max" : $avg_key;
    my ($revisions, $dummy_min_x, $dummy_min_y, $min_y, $max_y) =
	compute_min_max($rev_data, $rev_indexes, $min_key, $max_key);
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
	make_revisions_path($cr, $revisions, $rev_indexes, $rev_data, "min", 1);
	my @revisions_rev = reverse @$revisions;
	make_revisions_path($cr, \@revisions_rev, $rev_indexes, $rev_data, "max", 0);
	$cr->restore;

	$cr->set_source_rgb(0.7, 0.7, 0.7);
	$cr->fill;
    }

    #avg
    $cr->save;
    transform_coords($cr, $window_x, $window_y, $window_width, $window_height, $min_x, $max_x, $min_y, $max_y);
    make_revisions_path($cr, $revisions, $rev_indexes, $rev_data, $avg_key, 1);
    $cr->restore;

    $cr->set_line_width($line_width);
    $cr->set_source_rgb(0, 0, 0);
    $cr->stroke;

    #min/max markers
    set_font($cr, $font_size);

    $cr->save;
    transform_coords($cr, $window_x, $window_y, $window_width, $window_height, $min_x, $max_x, $min_y, $max_y);
    my ($avg_min_rev, $avg_max_rev) = ($test_data->{"$avg_key\_min_rev"}, $test_data->{"$avg_key\_max_rev"});
    my ($avg_min_x, $avg_min_y) = $cr->user_to_device($rev_indexes->{$avg_min_rev}, $rev_data->{$avg_min_rev}{$avg_key});
    my ($avg_max_x, $avg_max_y) = $cr->user_to_device($rev_indexes->{$avg_max_rev}, $rev_data->{$avg_max_rev}{$avg_key});
    $cr->restore;

    plot_marker_circle($cr, $avg_min_x, $avg_min_y, $marker_radius, $line_width, 0, 0.6, 0);
    show_text_below($cr, $revname->($avg_min_rev), $avg_min_x, $avg_min_y + $text_distance, $img_width, $img_height);

    plot_marker_circle($cr, $avg_max_x, $avg_max_y, $marker_radius, $line_width, 1, 0, 0);
    show_text_above($cr, $revname->($avg_max_rev), $avg_max_x, $avg_max_y - $text_distance, $img_width, $img_height);

    $cr->show_page;
    $surface->write_to_png($filename);
}

sub plot_cairo_combined {
    my ($combined_data, $rev_indexes, $filename, $revname, $plot_min_max,
	$img_width, $img_height, $line_width, $marker_radius, $font_size, $scale) = @_;

    $img_width *= $scale;
    $img_height *= $scale;
    $line_width *= $scale;
    $marker_radius *= $scale;
    $font_size *= $scale;

    my $min_key = $plot_min_max ? "min" : "avg";
    my $max_key = $plot_min_max ? "max" : "avg";
    my ($revisions, $min_x, $max_x, $min_y, $max_y) = compute_min_max($combined_data, $rev_indexes, $min_key, $max_key);
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
    make_revisions_path($cr, $revisions, $rev_indexes, $combined_data, "avg", 1);
    $cr->restore;

    $cr->set_line_width($line_width);
    $cr->set_source_rgb(0, 0, 0);
    $cr->stroke;

    if ($plot_min_max) {
	#min
	$cr->save;
	transform_coords($cr, $window_x, $window_y, $window_width, $window_height, $min_x, $max_x, $min_y, $max_y);
	make_revisions_path($cr, $revisions, $rev_indexes, $combined_data, "min", 1);
	$cr->restore;

	$cr->set_line_width($line_width);
	$cr->set_source_rgb(0, 0.3, 0);
	$cr->stroke;

	#max
	$cr->save;
	transform_coords($cr, $window_x, $window_y, $window_width, $window_height, $min_x, $max_x, $min_y, $max_y);
	make_revisions_path($cr, $revisions, $rev_indexes, $combined_data, "max", 1);
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
    my ($avg_min_x, $avg_min_y) = $cr->user_to_device($rev_indexes->{$min_rev}, $min_avg);
    my ($avg_max_x, $avg_max_y) = $cr->user_to_device($rev_indexes->{$max_rev}, $max_avg);
    $cr->restore;

    plot_marker_circle($cr, $avg_min_x, $avg_min_y, $marker_radius, $line_width, 0, 0.6, 0);
    show_text_below($cr, $revname->($min_rev), $avg_min_x, $avg_min_y + $text_distance, $img_width, $img_height);

    plot_marker_circle($cr, $avg_max_x, $avg_max_y, $marker_radius, $line_width, 1, 0, 0);
    show_text_above($cr, $revname->($max_rev), $avg_max_x, $avg_max_y - $text_distance, $img_width, $img_height);

    $cr->show_page;
    $surface->write_to_png($filename);
}

sub file_mtime {
    my ($filename) = @_;
    my ($dev, $ino, $mode, $nlink, $uid, $gid, $rdev, $size, $atime, $mtime, $ctime, $blksize, $blocks) = stat $filename;

    return $mtime;
}

my @config_files = ();

if (!GetOptions ('conf=s' => \@config_files) || $#ARGV < 1) {
    print STDERR "Usage: collect.pl <config-root> <config-dir> ...\n";
    exit 1;
}

my $config_root = $ARGV [0];
my @configs = @ARGV [1 .. $#ARGV];

my %all_combined_data = ();
my %all_test_data = ();
my %all_test_rev_data = ();

my %config_file_data = ();

foreach my $file (@config_files) {
    my $name = undef;
    my %data = ();
    open CONF, $file or die;
    while (<CONF>) {
	chomp;
	if (/(\w+)\s*=\s*("?)([^"]+)\2/) {
	    if ($1 eq "CONFIG_NAME") {
		$name = $3;
	    } elsif ($1 eq "IGNORE_REVS") {
		my @revs = split /\s+/, $3;
		$data{"ignore"} = \@revs;
	    }
	}
    }
    if (defined ($name)) {
	$config_file_data {$name} = \%data;
    } else {
	print STDERR "Warning: No CONFIG_NAME in '$file' - ignoring.\n";
    }
    close CONF;
}

sub ignore_sha {
    my ($config, $sha) = @_;
    if (exists $config_file_data{$config}->{"ignore"}) {
	my @ignores = @{$config_file_data{$config}->{"ignore"}};
	foreach my $ignore (@ignores) {
	    return 1 if $sha =~ /^$ignore/;
	}
    }
    return 0;
}

foreach my $confdir (@configs) {
    my $basedir = "$config_root/$confdir";

    if (! -d $basedir) {
	print STDERR "Error: Configuration directory '$basedir' does not exist.";
	exit 1;
    }

    my $config = basename ($basedir);

    my %test_rev_data = ();
    my %test_data = ();

    my %revisions = ();
    my %shas = ();

    sub revname {
	my ($revision) = @_;
	if (exists $shas{$revision}) {
	    my $sha = $shas{$revision};
	    return substr $sha, 0, 10;
	} else {
	    return $revision;
	}
    }

    sub revlink {
	my ($revision) = @_;
	if (exists $shas{$revision}) {
	    my $sha = $shas{$revision};
	    return sprintf "<a href=\"https://github.com/mono/mono/commit/%s\">%s</a>", $sha, revname ($revision);
	} else {
	    return $revision;
	}
    }

    my %rev_indexes = ();
    my $next_rev_index = 0;

    opendir DIR, $basedir or die;
    my @rev_dirs = sort { $a cmp $b } grep {/^r/ and -d "$basedir/$_"} readdir DIR;
    closedir DIR;

    my ($first_rev, $last_rev);

    foreach my $subdir (@rev_dirs) {
	$subdir =~ /^r(.+)$/ or die;
	my $revision = $1;
	my $rev_index = $next_rev_index++;

	my $dir = "$basedir/$subdir";
	opendir DIR, $dir or die;
	my @filenames = grep /\.times$/, readdir DIR;
	closedir DIR;

	next unless @filenames;

	if (!defined($first_rev)) {
	    $first_rev = $revision;
	    $last_rev = $revision;
	} else {
	    $first_rev = min($first_rev, $revision);
	    $last_rev = max($last_rev, $revision);
	}

	my $shaname = "$dir/sha1";
	if (-f $shaname) {
	    open SHA, $shaname or die;
	    my $sha = <SHA>;
	    close SHA;
	    chomp $sha;
	    if ($sha =~ /^[0-9a-f]{5,40}$/) {
		if (ignore_sha ($config, $sha)) {
		    print "ignoring $sha\n";
		    next;
		}

		$shas{$revision} = $sha;
		print "$revision - $sha\n";
	    } else {
		print STDERR "Warning: Invalid SHA1 in '$shaname' - ignoring.\n";
	    }
	}

	$rev_indexes{$revision} = $rev_index;

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
	plot_cairo_single($test_rev_data{$test}, $test_data{$test}, \%rev_indexes, $first_rev, $last_rev,
			  1, "avg", "$basedir/$test\_large.png", \&revname,
			  LARGE_WIDTH, LARGE_HEIGHT, LARGE_LINE_WIDTH, LARGE_MARKER_RADIUS, LARGE_FONT_SIZE, SCALE);
	plot_cairo_single($test_rev_data{$test}, $test_data{$test}, \%rev_indexes, $first_rev, $last_rev,
			  1, "avg", "$basedir/$test.png", \&revname,
			  SMALL_WIDTH, SMALL_HEIGHT, SMALL_LINE_WIDTH, SMALL_MARKER_RADIUS, SMALL_FONT_SIZE, SCALE);

	plot_cairo_single($test_rev_data{$test}, $test_data{$test}, \%rev_indexes, $first_rev, $last_rev,
			  0, "size", "$basedir/$test\_size_large.png", \&revname,
			  LARGE_WIDTH, LARGE_HEIGHT, LARGE_LINE_WIDTH, LARGE_MARKER_RADIUS, LARGE_FONT_SIZE, SCALE);
	plot_cairo_single($test_rev_data{$test}, $test_data{$test}, \%rev_indexes, $first_rev, $last_rev,
			  0, "size", "$basedir/$test\_size.png", \&revname,
			  SMALL_WIDTH, SMALL_HEIGHT, SMALL_LINE_WIDTH, SMALL_MARKER_RADIUS, SMALL_FONT_SIZE, SCALE);
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
    plot_cairo_combined(\%combined_data, \%rev_indexes, "$basedir/combined_large.png", \&revname, 1,
			LARGE_WIDTH, LARGE_HEIGHT, LARGE_LINE_WIDTH, LARGE_MARKER_RADIUS, LARGE_FONT_SIZE, SCALE);
    plot_cairo_combined(\%combined_data, \%rev_indexes, "$basedir/combined.png", \&revname, 0,
			SMALL_WIDTH, SMALL_HEIGHT, SMALL_LINE_WIDTH, SMALL_MARKER_RADIUS, SMALL_FONT_SIZE, SCALE);

    #write html index
    my @last_revs = (sort { $a cmp $b } keys %revisions) [-3 .. -1];

    open FILE, ">$basedir/index.html" or die;
    print FILE "<html><body>\n";
    print FILE "<h1>$config</h1>\n";
    printf FILE "<p><img src=\"combined_large.png\" width=\"%dpx\" height=\"%dpx\">\n", LARGE_WIDTH, LARGE_HEIGHT;
    print FILE "<p><table cellpadding=\"5\" border=\"1\" rules=\"groups\">\n";
    print FILE "<colgroup align=\"left\">\n";
    print FILE "<colgroup align=\"left\" span=\"2\">\n";
    print FILE "<colgroup align=\"left\" span=\"2\">\n";
    foreach my $rev (@last_revs) {
	print FILE "<colgroup align=\"left\" span=\"2\">\n";
    }
    print FILE "<colgroup align=\"left\">\n";
    print FILE "<colgroup align=\"left\">\n";

    print FILE "<tr><td><b>Benchmark</b></td><td colspan=\"2\"><b>Best</b></td><td colspan=\"2\"><b>Worst</b></td><td colspan=\"2\"><b>Latest but two</b></td><td colspan=\"2\"><b>Latest but one</b></td><td colspan=\"2\"><b>Latest</b></td><td><b>Duration</b></td><td><b>Size</b></td></tr>";
    print FILE "<tr><td></td><td colspan=\"2\"></td><td colspan=\"2\"></td>";
    foreach my $rev (@last_revs) {
	printf FILE "<td colspan=\"2\">%s</td>", revlink ($rev);
    }
    print FILE "<td></td><td></td></tr>\n";

    foreach my $test (sort keys %test_rev_data) {
	print FILE "<tr><td><a href=\"$test.html\">$test</a></td>";

	my $avg_min_rev = $test_data{$test}{"avg_min_rev"};
	my $avg_min = $test_rev_data{$test}{$avg_min_rev}{"avg"};
	my $avg_max_rev = $test_data{$test}{"avg_max_rev"};
	my $avg_max = $test_rev_data{$test}{$avg_max_rev}{"avg"};

	printf FILE "<td>%.2f</td><td>%s</td><td>%.2f</td><td>%s</td>", $avg_min, revlink ($avg_min_rev), $avg_max, revlink ($avg_max_rev);

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

	printf FILE "<td><a href=\"$test\_large.png\"><img src=\"$test.png\" border=\"0\" width=\"%dpx\" height=\"%dpx\"></a></td>", SMALL_WIDTH, SMALL_HEIGHT;
	printf FILE "<td><a href=\"$test\_size_large.png\"><img src=\"$test\_size.png\" border=\"0\" width=\"%dpx\" height=\"%dpx\"></a></td>", SMALL_WIDTH, SMALL_HEIGHT;
	print FILE "</tr>\n";
    }
    print FILE "</table>\n";
    print FILE "<p>Written on " . (scalar localtime) . ".</p>\n";
    print FILE "</body></html>";
    close FILE;

    #write html for tests
    foreach my $test (keys %test_rev_data) {
	open FILE, ">$basedir/$test.html" or die;

	print FILE "<html><body>\n";
	print FILE "<p><a href=\"index.html\">$config</a>\n";
	print FILE "<h1>$test on $config</h1>\n";
	printf FILE "<p><img src=\"$test\_large.png\" width=\"%dpx\" height=\"%dpx\">\n", LARGE_WIDTH, LARGE_HEIGHT;

	print FILE "<p><table cellpadding=\"5\"><tr><td><b>Revision</b></td><td><b>Average</b></td><td><b>Min</b></td><td><b>Max</b></td><td><b>Size (bytes)</b></td><td><b>Benchmarked on</b></td><td><b>All times</b></td></tr>\n";
	foreach my $revision (sort { $b cmp $a } keys %{$test_rev_data{$test}}) {
	    my $html_filename = "r$revision/$test.times";
	    my $filename = "$basedir/$html_filename";
	    my $ctime = file_mtime($filename);
	    my $avg = $test_rev_data{$test}{$revision}{"avg"};
	    my $min = $test_rev_data{$test}{$revision}{"min"};
	    my $max = $test_rev_data{$test}{$revision}{"max"};
	    my $size = $test_rev_data{$test}{$revision}{"size"};

	    printf FILE "<tr><td>%s</td><td>%.2f</td><td>%.2f</td><td>%.2f</td><td>$size</td><td>%s</td><td><a href=\"$html_filename\">All times</a></td></tr>\n", revlink ($revision), $avg, $min, $max, (scalar localtime($ctime));
	}
	print FILE "</table>\n";
	print FILE "<p>Written on " . (scalar localtime) . ".</p>\n";
	print FILE "</body></html>\n";

	close FILE;
    }
}

#write main index
open FILE, ">$config_root/index.html" or die;

print FILE "<html><body>\n";
print FILE "<h1>Mono Performance Monitoring</h1>\n";

print FILE "<a href=\"http://www.mono-project.com/Benchmark_Suite\">Need help?</a>\n";

print FILE "<table cellpadding=\"5\"><tr><td><b>Config</b></td><td><b>Last Revision</b></td><td><b>Average</b></td><td colspan=\"2\"><b>Worst</b></td><td><b>Duration</b></td></tr>\n";
foreach my $confdir (@configs) {
    my $basedir = "$config_root/$confdir";
    my $config = basename ($basedir);
    my $combined_data = $all_combined_data{$config};
    my $test_data = $all_test_data{$config};
    my $test_rev_data = $all_test_rev_data{$config};
    my @revisions = sort { $a cmp $b } keys %$combined_data;

    if ($#revisions < 0) {
	print STDERR "Warning: Configuration '$config' has no revisions - ignoring.\n";
	next;
    }

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

    print FILE "<tr><td><a href=\"$confdir/index.html\">$config</a></td>";
    printf FILE "<td>%s</td>", revlink ($last_revision);
    printf FILE "<td>%.2f%%</td>", $combined_data->{$last_revision}{"avg"} / $best_avg * 100;
    printf FILE "<td>%.2f%%</td><td>$worst_test</td>", $worst_quot * 100;
    printf FILE "<td><a href=\"$confdir/combined_large.png\"><img src=\"$confdir/combined.png\" border=\"0\" width=\"%dpx\" height=\"%dpx\"></a></td></tr>\n", SMALL_WIDTH, SMALL_HEIGHT;
}
print FILE "</table>\n";

# iterate through revisions in reverse order
my $last_common_revision = undef;
REVISION: foreach my $revision (sort { $b cmp $a } keys %{$all_combined_data{$configs[0]}}) {
    foreach my $confdir (@configs) {
	my $basedir = "$config_root/$confdir";
	my $config = basename ($basedir);
	next REVISION unless exists $all_combined_data{$config}->{$revision};
    }
    $last_common_revision = $revision;
    last;
}

if (defined ($last_common_revision)) {
    print STDERR "Last common revision is $last_common_revision.\n";
    my $png_file = "$config_root/comparison.png";
    my @args = ("./compare.py", "-o", $png_file);
    foreach my $confdir (@configs) {
	my $basedir = "$config_root/$confdir";
	push @args, "$basedir/r$last_common_revision";
    }
    system (@args);
    if ($? == 0 and -f $png_file) {
	printf FILE "<h3>Comparison between configs for %s:</h3>", revlink ($last_common_revision);
	print FILE "<img src=\"comparison.png\" style=\"zoom: 50%;\">";
    } else {
	print STDERR "Warning: Generating comparison graph failed.\n";
	print FILE "<p>Comparison graph generation failed.";
    }
} else {
    print STDERR "Warning: No revisions in common - not generating comparison.\n";
}

print FILE "<p>Written on " . (scalar localtime) . ".</p>\n";
print FILE "</body></html>\n";

close FILE;

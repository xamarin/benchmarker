$tests = get-childitem ../tests/ *.exe -rec

if (($args.length -lt 1) -or (($args.get(0) -ne "ON") -and ($args.get(0) -ne "OFF"))) {
	write-host "Usage : force32 [ON|OFF]"
	exit
}

if ($args.get(0) -eq "ON") {
	$corflags_arg = "/32BIT+"
}
elseif ($args.get(0) -eq "OFF") {
	$corflags_arg = "/32BIT-"
}
else {
	exit
}

foreach ($test in $tests) {
	write-host "$test : Updating Force 32 bit flag to " $args.get(0)
	.\CorFlags.exe $test.fullname $corflags_arg /Force
	write-host
}
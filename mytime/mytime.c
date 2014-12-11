#include <stdio.h>
#include <unistd.h>
#include <sys/time.h>
#include <errno.h>
#include <string.h>
#include <assert.h>
#include <sys/wait.h>
#include <stdlib.h>
#include <signal.h>

int
main (int argc, char *argv [])
{
	char *filename = argv [1];
	FILE *file;
	int worker_pid, timeout_pid, wait_pid;
	struct timeval tv1, tv2;
	int stat;
	double diff;
	int timeout;

	if (argc <= 3) {
		fprintf (stderr, "Usage: mytime TIME-FILE TIMEOUT PROGRAM [ARG ...]\n");
		exit (1);
	}

	assert (argc > 3);

	timeout = atoi (argv [2]);
	assert (timeout > 0);

	timeout_pid = fork ();
	assert (timeout_pid != -1);
	if (timeout_pid == 0) {
		sleep (timeout);
		exit (0);
	}

	worker_pid = fork ();
	assert (worker_pid != -1);
	if (worker_pid == 0) {
		char *args [argc - 2 + 1];
		int i;

		for (i = 0; i < argc - 2; ++i)
			args [i] = argv [i + 3];
		args [i] = NULL;

		execv (argv [3], args);

		fprintf (stderr, "Could not exec: %s\n", strerror (errno));

		return 1;
	}

	gettimeofday (&tv1, NULL);

	do {
		wait_pid = wait (&stat);
	} while (wait_pid == -1 && errno == EINTR);

	gettimeofday (&tv2, NULL);

	if (wait_pid == -1) {
		fprintf (stderr, "Error: Could not wait: %s\n", strerror (errno));
		return 1;
	}

	if (wait_pid == worker_pid) {
		kill (timeout_pid, SIGKILL);
	} else {
		assert (wait_pid == timeout_pid);
		kill (worker_pid, SIGKILL);

		fprintf (stderr, "Error: Timeout.\n");
		return 1;
	}

	if (!WIFEXITED (stat)) {
		fprintf (stderr, "Error: Not exited normally.\n");
		return 1;
	}
	if (WEXITSTATUS (stat) != 0) {
		fprintf (stderr, "Error: Exited with error.\n");
		return WEXITSTATUS (stat);
	}

	diff = tv2.tv_sec - tv1.tv_sec;
	if (tv2.tv_usec < tv1.tv_usec)
		diff -= (tv1.tv_usec - tv2.tv_usec) / 1000000.0;
	else
		diff += (tv2.tv_usec - tv1.tv_usec) / 1000000.0;

	file = fopen (filename, "a");
	if (!file) {
		fprintf (stderr, "Could not open output file: %s\n", strerror (errno));
		return 1;
	}

	fprintf (file, "%0.2f\n", diff);

	fclose (file);

	return 0;
}

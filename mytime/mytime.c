#include <stdio.h>
#include <unistd.h>
#include <sys/time.h>
#include <errno.h>
#include <string.h>
#include <assert.h>
#include <sys/wait.h>

int
main (int argc, char *argv [])
{
	char *filename = argv [1];
	FILE *file;
	int pid;
	struct timeval tv1, tv2;
	int stat;
	double diff;

	assert (argc > 2);


	pid = fork ();
	assert (pid != -1);
	if (pid == 0) {
		char *args [argc - 2 + 1];
		int i;

		for (i = 0; i < argc - 2; ++i)
			args [i] = argv [i + 2];
		args [i] = NULL;

		execv (argv [2], args);

		fprintf (stderr, "Could not exec: %s\n", strerror (errno));

		return 1;
	}

	gettimeofday (&tv1, NULL);

	while (wait (&stat) != pid) {
		if (errno != EINTR) {
			fprintf (stderr, "Could not wait: %s\n", strerror (errno));
			return 1;
		}
	}

	gettimeofday (&tv2, NULL);

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

using System;
using Benchmarker;
using System.Threading.Tasks;

namespace Benchmarker
{
	public class Helper
	{
		public async static Task RunWithRetry (Func<Task> run, int numTries = 3) {
			for (var i = 0; i < numTries - 1; ++i) {
				try {
					await run ();
					return;
				} catch (Exception exc) {
					var seconds = (i == 0) ? 10 : 60 * i;
					Logging.GetLogging ().ErrorFormat ("Exception when running task - sleeping {0} seconds and retrying: {1}", seconds, exc);
					await Task.Delay (seconds * 1000);
				}
			}
			await run ();
		}

		public async static Task<T> RunWithRetry<T> (Func<Task<T>> run, int numTries = 3) {
			for (var i = 0; i < numTries - 1; ++i) {
				try {
					return await run ();
				} catch (Exception exc) {
					var seconds = (i == 0) ? 10 : 60 * i;
					Logging.GetLogging ().ErrorFormat ("Exception when running task - sleeping {0} seconds and retrying: {1}", seconds, exc);
					await Task.Delay (seconds * 1000);
				}
			}
			return await run ();
		}
	}
}


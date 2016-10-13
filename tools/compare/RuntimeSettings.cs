namespace Xamarin.Test.Performance.Utilities
{
    /// <summary>
    /// Global statics shared between the runner and tests.
    /// </summary>
    public static class RuntimeSettings
    {
        /// <summary>
        /// True if the logger should be verbose.
        /// </summary>
        public static bool IsVerbose = true;

        /// <summary>
        /// True if a runner is orchestrating the test runs.
        /// </summary>
        public static bool IsRunnerAttached = false;
    }
}

using Smartstore.Utilities;

namespace Smartstore.Diagnostics
{
    /// <summary>
    /// Measures execution time of a unit of work and displays
    /// the result in a profiler widget.
    /// </summary>
    public interface IChronometer : IDisposable
    {
        /// <summary>
        /// Starts a timer with a given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Unique step key</param>
        /// <param name="message">Message to display in the widget</param>
        void StepStart(string key, string message);

        /// <summary>
        /// Stops the timer with the given <paramref name="key"/>
        /// </summary>
        /// <param name="key"></param>
        void StepStop(string key);
    }

    public static class IChronometerExtensions
    {
        /// <summary>
        /// Starts a timer and stops it on disposing.
        /// </summary>
        public static IDisposable Step(this IChronometer chronometer, string message)
        {
            Guard.NotEmpty(message, nameof(message));

            var key = "step" + CommonHelper.GenerateRandomDigitCode(10);

            chronometer.StepStart(key, message);
            return new ActionDisposable(() => chronometer.StepStop(key));
        }
    }

    public class NullChronometer : IChronometer
    {
        public static IChronometer Instance => new NullChronometer();

        public void StepStart(string key, string message)
        {
        }

        public void StepStop(string key)
        {
        }

        public void Dispose()
        {
        }
    }
}

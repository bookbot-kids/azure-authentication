using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Authentication.Shared.Utils
{
    /// <summary>
    /// Tracking execute time of function
    /// </summary>
    public class TimeTracking
    {
        /// <summary>
        /// Stopwatch to count time
        /// </summary>
        private readonly Stopwatch stopwatch = new Stopwatch();

        /// <summary>
        /// The stopwatch to count the whole execution time
        /// </summary>
        private Stopwatch rootStopwatch;

        /// <summary>
        /// Initialize stopwatch for the whole function
        /// </summary>
        public TimeTracking()
        {
            rootStopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Begin count time
        /// </summary>
        public void BeginTracking()
        {
            try
            {
                stopwatch.Start();
            }catch(Exception ex)
            {
                Logger.Log?.LogError($"BeginTracking error {ex.Message}");
            }
            
        }

        /// <summary>
        /// End count time
        /// </summary>
        /// <param name="message">Log message</param>
        public void EndTracking(string message)
        {
            try
            {
                stopwatch.Stop();
                Logger.Log?.LogInformation($"{message} executed in {stopwatch.ElapsedMilliseconds} ms");
                stopwatch.Reset();
            }catch(Exception ex)
            {
                Logger.Log?.LogError($"EndTracking error {ex.Message}");
            }
        }

        /// <summary>
        /// Deconstructor to log the execute time
        /// </summary>
        ~TimeTracking()  // finalizer
        {
            try
            {
                rootStopwatch.Stop();
                Logger.Log?.LogInformation($"Function executed in {rootStopwatch.ElapsedMilliseconds} ms");
                rootStopwatch.Reset();
            }
            catch (Exception ex)
            {
                Logger.Log?.LogError($"EndTracking error {ex.Message}");
            }
        }

    }
}

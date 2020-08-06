using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Authentication.Shared.Library
{
    /// <summary>
    /// Tracking execute time of function
    /// </summary>
    public class TimeTracking
    {
        private readonly bool enableTracking = true;

        /// <summary>
        /// Stopwatch to count time of each part
        /// </summary>
        private readonly Stopwatch stopwatch = new Stopwatch();

        /// <summary>
        /// The stopwatch to count the whole execution time
        /// </summary>
        private readonly Stopwatch rootStopwatch;

        /// <summary>
        /// Initialize stopwatch for the whole function
        /// </summary>
        public TimeTracking(bool enableTracking = true)
        {
            this.enableTracking = enableTracking;
            if(enableTracking)
            {
                rootStopwatch = Stopwatch.StartNew();
            }
        }

        /// <summary>
        /// Begin count time
        /// </summary>
        public void BeginTracking()
        {
            if(!enableTracking)
            {
                return;
            }

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
            if (!enableTracking)
            {
                return;
            }

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
        /// Stop the whole counter to log the execute time
        /// </summary>
        public void Stop(string message)
        {
            if (enableTracking)
            {
                try
                {
                    rootStopwatch.Stop();
                    Logger.Log?.LogInformation($"{message} executed in {rootStopwatch.ElapsedMilliseconds} ms");
                    rootStopwatch.Reset();
                }
                catch (Exception ex)
                {
                    Logger.Log?.LogError($"EndTracking error {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Deconstructor, stop the watch if it doesn't stop
        /// </summary>
        ~TimeTracking()
        {
            if (enableTracking)
            {
                try
                {
                    if(rootStopwatch.IsRunning)
                    {
                        rootStopwatch.Stop();
                        rootStopwatch.Reset();
                    }

                    if(stopwatch.IsRunning)
                    {
                        stopwatch.Stop();
                        stopwatch.Reset();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log?.LogError($"EndTracking error {ex.Message}");
                }
            }
        }

    }
}

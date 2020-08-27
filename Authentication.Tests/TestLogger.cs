using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Authentication.Tests
{
    /// <summary>
    /// Logger types
    /// </summary>
    public enum LoggerTypes
    {
        Null,
        List
    }

    /// <summary>
    /// Mocks a scope for the test cases to pass to the ListLogger class
    /// </summary>
    public class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();

        private NullScope() { }

        public void Dispose() { }
    }

    /// <summary>
    /// Custom logger to pass into azure function
    /// </summary>
    public class ListLogger : ILogger
    {
        public IList<string> Logs;

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => false;

        public ListLogger()
        {
            Logs = new List<string>();
        }

        public void Log<TState>(LogLevel logLevel,
                                EventId eventId,
                                TState state,
                                Exception exception,
                                Func<TState, Exception, string> formatter)
        {
            string message = formatter(state, exception);
            Logs.Add(message);
        }
    }
}

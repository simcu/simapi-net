using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SimApi.Logger
{
    public class SimApiLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, SimApiLogger> _loggers =
            new ConcurrentDictionary<string, SimApiLogger>();


        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new SimApiLogger(name));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
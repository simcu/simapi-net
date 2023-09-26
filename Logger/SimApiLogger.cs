using System;
using Microsoft.Extensions.Logging;
using SimApi.Helpers;

namespace SimApi.Logger
{
    public class SimApiLogger : ILogger
    {
        private string Name { get; }

        public SimApiLogger(string name)
        {
            Name = name;
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            Console.ForegroundColor = logLevel switch
            {
                LogLevel.Debug => ConsoleColor.DarkMagenta,
                LogLevel.Information => ConsoleColor.DarkCyan,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
            var message =
                $"[ {Name} ][ {SimApiUtil.CstNow.ToString("yyyy-MM-dd HH:mm:ss:ffff")} ][ {logLevel.ToString()} ]\n{state}\n";
            if (exception != null)
            {
                message += $"{exception}\n";
            }
            Console.WriteLine(message);
        }
    }
}
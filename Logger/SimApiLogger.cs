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
            var defautColor = Console.ForegroundColor;
            Console.ForegroundColor = logLevel switch
            {
                LogLevel.Debug => ConsoleColor.DarkMagenta,
                LogLevel.Information => ConsoleColor.DarkCyan,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => defautColor
            };
            Console.WriteLine(
                $"[ {Name} ][ {SimApiUtil.CstNow.ToString("yyyy-MM-dd HH:mm:ss:ffff")} ][ {logLevel.ToString()} ]");
            Console.ForegroundColor = defautColor;
            Console.WriteLine($"{state}");
            if (exception != null)
            {
                Console.WriteLine($"{exception}");
            }

            Console.WriteLine();
        }
    }
}
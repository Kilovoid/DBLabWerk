using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SQLWerk.Services;

public sealed record LogEntry(DateTime Time, LogLevel Level, string Message,
                              string File, int Row, string Reason);

public sealed class InMemoryLoggerProvider : ILoggerProvider
{
    private readonly List<LogEntry> _entries = new();
    private readonly object _lock = new();

    public IReadOnlyList<LogEntry> Entries
    {
        get { lock (_lock) return _entries.ToList(); }
    }

    public bool HasWarnings
    {
        get { lock (_lock) return _entries.Any(e => e.Level >= LogLevel.Warning); }
    }

    public event EventHandler? Changed;

    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);

    public void Clear()
    {
        lock (_lock) _entries.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose() { }

    private void Add(LogEntry entry)
    {
        lock (_lock) _entries.Add(entry);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed class Logger : ILogger
    {
        private readonly InMemoryLoggerProvider _owner;
        private readonly string _category;

        public Logger(InMemoryLoggerProvider owner, string category)
        {
            _owner = owner;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel level, EventId id, TState state,
            Exception? ex, Func<TState, Exception?, string> fmt)
        {
            if (!IsEnabled(level)) return;

            string file = "", reason = "";
            int row = 0;

            if (state is IReadOnlyList<KeyValuePair<string, object?>> kv)
            {
                foreach (var p in kv)
                {
                    switch (p.Key)
                    {
                        case "File": file = p.Value?.ToString() ?? ""; break;
                        case "Row": int.TryParse(p.Value?.ToString(), out row); break;
                        case "Reason": reason = p.Value?.ToString() ?? ""; break;
                    }
                }
            }

            _owner.Add(new LogEntry(DateTime.Now, level, fmt(state, ex), file, row, reason));
        }
    }
}

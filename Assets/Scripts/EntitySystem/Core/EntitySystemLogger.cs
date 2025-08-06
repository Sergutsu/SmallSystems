using System;
using System.Collections.Generic;
using UnityEngine;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Centralized logging system for the Entity System
    /// </summary>
    public static class EntitySystemLogger
    {
        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Critical = 4
        }

        private static LogLevel _minLogLevel = LogLevel.Info;
        private static bool _enableFileLogging = false;
        private static string _logFilePath;
        private static Queue<LogEntry> _logHistory = new();
        private static int _maxHistorySize = 1000;
        private static readonly object _lockObject = new object();

        // Event for external log monitoring
        public static event Action<LogEntry> OnLogEntry;

        static EntitySystemLogger()
        {
            _logFilePath = System.IO.Path.Combine(Application.persistentDataPath, "EntitySystemLog.txt");
        }

        /// <summary>
        /// Configure the logger settings
        /// </summary>
        public static void Configure(LogLevel minLevel = LogLevel.Info, bool enableFileLogging = false, int maxHistorySize = 1000)
        {
            _minLogLevel = minLevel;
            _enableFileLogging = enableFileLogging;
            _maxHistorySize = maxHistorySize;

            if (_enableFileLogging)
            {
                try
                {
                    // Create log file header
                    var header = $"Entity System Log - Started at {DateTime.Now}\n" +
                                $"Unity Version: {Application.unityVersion}\n" +
                                $"Platform: {Application.platform}\n" +
                                "=====================================\n\n";
                    System.IO.File.WriteAllText(_logFilePath, header);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"EntitySystemLogger: Failed to initialize log file: {ex.Message}");
                    _enableFileLogging = false;
                }
            }

            Log(LogLevel.Info, "EntitySystemLogger", "Logger configured", null);
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        public static void LogDebug(string context, string message, Exception exception = null)
        {
            Log(LogLevel.Debug, context, message, exception);
        }

        /// <summary>
        /// Log an info message
        /// </summary>
        public static void LogInfo(string context, string message, Exception exception = null)
        {
            Log(LogLevel.Info, context, message, exception);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void LogWarning(string context, string message, Exception exception = null)
        {
            Log(LogLevel.Warning, context, message, exception);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public static void LogError(string context, string message, Exception exception = null)
        {
            Log(LogLevel.Error, context, message, exception);
        }

        /// <summary>
        /// Log a critical error message
        /// </summary>
        public static void LogCritical(string context, string message, Exception exception = null)
        {
            Log(LogLevel.Critical, context, message, exception);
        }

        /// <summary>
        /// Core logging method
        /// </summary>
        private static void Log(LogLevel level, string context, string message, Exception exception)
        {
            if (level < _minLogLevel) return;

            var logEntry = new LogEntry
            {
                Level = level,
                Context = context ?? "Unknown",
                Message = message ?? "No message",
                Exception = exception,
                Timestamp = DateTime.Now,
                ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId
            };

            lock (_lockObject)
            {
                // Add to history
                _logHistory.Enqueue(logEntry);
                while (_logHistory.Count > _maxHistorySize)
                {
                    _logHistory.Dequeue();
                }

                // Write to Unity console
                WriteToUnityConsole(logEntry);

                // Write to file if enabled
                if (_enableFileLogging)
                {
                    WriteToFile(logEntry);
                }

                // Notify listeners
                OnLogEntry?.Invoke(logEntry);
            }
        }

        /// <summary>
        /// Write log entry to Unity console
        /// </summary>
        private static void WriteToUnityConsole(LogEntry entry)
        {
            var formattedMessage = FormatLogEntry(entry, false);

            switch (entry.Level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    Debug.Log(formattedMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    if (entry.Exception != null)
                    {
                        Debug.LogException(entry.Exception);
                    }
                    else
                    {
                        Debug.LogError(formattedMessage);
                    }
                    break;
            }
        }

        /// <summary>
        /// Write log entry to file
        /// </summary>
        private static void WriteToFile(LogEntry entry)
        {
            try
            {
                var formattedMessage = FormatLogEntry(entry, true);
                System.IO.File.AppendAllText(_logFilePath, formattedMessage + "\n");
            }
            catch (Exception ex)
            {
                Debug.LogError($"EntitySystemLogger: Failed to write to log file: {ex.Message}");
                _enableFileLogging = false; // Disable file logging on error
            }
        }

        /// <summary>
        /// Format a log entry for display
        /// </summary>
        private static string FormatLogEntry(LogEntry entry, bool includeTimestamp)
        {
            var message = includeTimestamp 
                ? $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] [{entry.Context}] {entry.Message}"
                : $"[{entry.Level}] [{entry.Context}] {entry.Message}";

            if (entry.Exception != null)
            {
                message += $"\nException: {entry.Exception}";
            }

            return message;
        }

        /// <summary>
        /// Get recent log entries
        /// </summary>
        public static LogEntry[] GetRecentLogs(int count = 100)
        {
            lock (_lockObject)
            {
                var logs = new List<LogEntry>(_logHistory);
                var startIndex = Math.Max(0, logs.Count - count);
                return logs.GetRange(startIndex, logs.Count - startIndex).ToArray();
            }
        }

        /// <summary>
        /// Get log entries by level
        /// </summary>
        public static LogEntry[] GetLogsByLevel(LogLevel level)
        {
            lock (_lockObject)
            {
                var filteredLogs = new List<LogEntry>();
                foreach (var entry in _logHistory)
                {
                    if (entry.Level == level)
                    {
                        filteredLogs.Add(entry);
                    }
                }
                return filteredLogs.ToArray();
            }
        }

        /// <summary>
        /// Clear log history
        /// </summary>
        public static void ClearHistory()
        {
            lock (_lockObject)
            {
                _logHistory.Clear();
            }
        }

        /// <summary>
        /// Get logging statistics
        /// </summary>
        public static LoggingStats GetStats()
        {
            lock (_lockObject)
            {
                var stats = new LoggingStats();
                foreach (var entry in _logHistory)
                {
                    switch (entry.Level)
                    {
                        case LogLevel.Debug:
                            stats.DebugCount++;
                            break;
                        case LogLevel.Info:
                            stats.InfoCount++;
                            break;
                        case LogLevel.Warning:
                            stats.WarningCount++;
                            break;
                        case LogLevel.Error:
                            stats.ErrorCount++;
                            break;
                        case LogLevel.Critical:
                            stats.CriticalCount++;
                            break;
                    }
                }
                stats.TotalEntries = _logHistory.Count;
                return stats;
            }
        }

        /// <summary>
        /// Export logs to file
        /// </summary>
        public static bool ExportLogs(string filePath)
        {
            try
            {
                lock (_lockObject)
                {
                    var logs = new List<string>();
                    logs.Add($"Entity System Log Export - {DateTime.Now}");
                    logs.Add("==========================================");
                    logs.Add("");

                    foreach (var entry in _logHistory)
                    {
                        logs.Add(FormatLogEntry(entry, true));
                    }

                    System.IO.File.WriteAllLines(filePath, logs);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogError("EntitySystemLogger", $"Failed to export logs: {ex.Message}", ex);
                return false;
            }
        }
    }

    /// <summary>
    /// Represents a single log entry
    /// </summary>
    [Serializable]
    public struct LogEntry
    {
        public EntitySystemLogger.LogLevel Level;
        public string Context;
        public string Message;
        public Exception Exception;
        public DateTime Timestamp;
        public int ThreadId;
    }

    /// <summary>
    /// Statistics about logging activity
    /// </summary>
    [Serializable]
    public struct LoggingStats
    {
        public int TotalEntries;
        public int DebugCount;
        public int InfoCount;
        public int WarningCount;
        public int ErrorCount;
        public int CriticalCount;
    }
}
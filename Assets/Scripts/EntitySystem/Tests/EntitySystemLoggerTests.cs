using System;
using System.Linq;
using NUnit.Framework;
using GalacticVentures.EntitySystem.Core;

namespace GalacticVentures.EntitySystem.Tests
{
    /// <summary>
    /// Unit tests for the EntitySystemLogger
    /// </summary>
    public class EntitySystemLoggerTests
    {
        private LogEntry[] _capturedLogs;
        private bool _eventReceived;

        [SetUp]
        public void Setup()
        {
            EntitySystemLogger.Configure(EntitySystemLogger.LogLevel.Debug, false, 100);
            EntitySystemLogger.ClearHistory();
            _capturedLogs = null;
            _eventReceived = false;

            // Subscribe to log events
            EntitySystemLogger.OnLogEntry += OnLogEntryReceived;
        }

        [TearDown]
        public void TearDown()
        {
            EntitySystemLogger.OnLogEntry -= OnLogEntryReceived;
            EntitySystemLogger.ClearHistory();
        }

        private void OnLogEntryReceived(LogEntry entry)
        {
            _eventReceived = true;
        }

        [Test]
        public void LogDebug_ValidMessage_LogsCorrectly()
        {
            // Act
            EntitySystemLogger.LogDebug("TestContext", "Test debug message");

            // Assert
            var logs = EntitySystemLogger.GetRecentLogs(1);
            Assert.AreEqual(1, logs.Length);
            Assert.AreEqual(EntitySystemLogger.LogLevel.Debug, logs[0].Level);
            Assert.AreEqual("TestContext", logs[0].Context);
            Assert.AreEqual("Test debug message", logs[0].Message);
            Assert.IsTrue(_eventReceived);
        }

        [Test]
        public void LogInfo_ValidMessage_LogsCorrectly()
        {
            // Act
            EntitySystemLogger.LogInfo("TestContext", "Test info message");

            // Assert
            var logs = EntitySystemLogger.GetRecentLogs(1);
            Assert.AreEqual(1, logs.Length);
            Assert.AreEqual(EntitySystemLogger.LogLevel.Info, logs[0].Level);
            Assert.AreEqual("TestContext", logs[0].Context);
            Assert.AreEqual("Test info message", logs[0].Message);
        }

        [Test]
        public void LogWarning_ValidMessage_LogsCorrectly()
        {
            // Act
            EntitySystemLogger.LogWarning("TestContext", "Test warning message");

            // Assert
            var logs = EntitySystemLogger.GetRecentLogs(1);
            Assert.AreEqual(1, logs.Length);
            Assert.AreEqual(EntitySystemLogger.LogLevel.Warning, logs[0].Level);
            Assert.AreEqual("TestContext", logs[0].Context);
            Assert.AreEqual("Test warning message", logs[0].Message);
        }

        [Test]
        public void LogError_ValidMessage_LogsCorrectly()
        {
            // Act
            EntitySystemLogger.LogError("TestContext", "Test error message");

            // Assert
            var logs = EntitySystemLogger.GetRecentLogs(1);
            Assert.AreEqual(1, logs.Length);
            Assert.AreEqual(EntitySystemLogger.LogLevel.Error, logs[0].Level);
            Assert.AreEqual("TestContext", logs[0].Context);
            Assert.AreEqual("Test error message", logs[0].Message);
        }

        [Test]
        public void LogCritical_ValidMessage_LogsCorrectly()
        {
            // Act
            EntitySystemLogger.LogCritical("TestContext", "Test critical message");

            // Assert
            var logs = EntitySystemLogger.GetRecentLogs(1);
            Assert.AreEqual(1, logs.Length);
            Assert.AreEqual(EntitySystemLogger.LogLevel.Critical, logs[0].Level);
            Assert.AreEqual("TestContext", logs[0].Context);
            Assert.AreEqual("Test critical message", logs[0].Message);
        }

        [Test]
        public void LogWithException_ValidException_IncludesException()
        {
            // Arrange
            var testException = new Exception("Test exception");

            // Act
            EntitySystemLogger.LogError("TestContext", "Test error with exception", testException);

            // Assert
            var logs = EntitySystemLogger.GetRecentLogs(1);
            Assert.AreEqual(1, logs.Length);
            Assert.AreEqual(testException, logs[0].Exception);
        }

        [Test]
        public void Configure_MinLogLevel_FiltersLogs()
        {
            // Arrange
            EntitySystemLogger.Configure(EntitySystemLogger.LogLevel.Warning, false, 100);

            // Act
            EntitySystemLogger.LogDebug("TestContext", "Debug message");
            EntitySystemLogger.LogInfo("TestContext", "Info message");
            EntitySystemLogger.LogWarning("TestContext", "Warning message");
            EntitySystemLogger.LogError("TestContext", "Error message");

            // Assert
            var logs = EntitySystemLogger.GetRecentLogs(10);
            Assert.AreEqual(3, logs.Length); // Only Warning, Error, and the Configure log
            Assert.IsTrue(logs.Any(l => l.Level == EntitySystemLogger.LogLevel.Warning));
            Assert.IsTrue(logs.Any(l => l.Level == EntitySystemLogger.LogLevel.Error));
            Assert.IsFalse(logs.Any(l => l.Level == EntitySystemLogger.LogLevel.Debug));
        }

        [Test]
        public void GetLogsByLevel_SpecificLevel_ReturnsFilteredLogs()
        {
            // Arrange
            EntitySystemLogger.LogDebug("TestContext", "Debug message");
            EntitySystemLogger.LogInfo("TestContext", "Info message");
            EntitySystemLogger.LogWarning("TestContext", "Warning message");
            EntitySystemLogger.LogError("TestContext", "Error message");

            // Act
            var warningLogs = EntitySystemLogger.GetLogsByLevel(EntitySystemLogger.LogLevel.Warning);

            // Assert
            Assert.AreEqual(1, warningLogs.Length);
            Assert.AreEqual(EntitySystemLogger.LogLevel.Warning, warningLogs[0].Level);
            Assert.AreEqual("Warning message", warningLogs[0].Message);
        }

        [Test]
        public void GetStats_WithLogs_ReturnsCorrectStats()
        {
            // Arrange
            EntitySystemLogger.LogDebug("TestContext", "Debug message");
            EntitySystemLogger.LogInfo("TestContext", "Info message 1");
            EntitySystemLogger.LogInfo("TestContext", "Info message 2");
            EntitySystemLogger.LogWarning("TestContext", "Warning message");
            EntitySystemLogger.LogError("TestContext", "Error message");

            // Act
            var stats = EntitySystemLogger.GetStats();

            // Assert
            Assert.AreEqual(6, stats.TotalEntries); // Including the Configure log
            Assert.AreEqual(1, stats.DebugCount);
            Assert.AreEqual(3, stats.InfoCount); // Including the Configure log
            Assert.AreEqual(1, stats.WarningCount);
            Assert.AreEqual(1, stats.ErrorCount);
            Assert.AreEqual(0, stats.CriticalCount);
        }

        [Test]
        public void ClearHistory_WithLogs_ClearsAllLogs()
        {
            // Arrange
            EntitySystemLogger.LogInfo("TestContext", "Test message");
            var logsBefore = EntitySystemLogger.GetRecentLogs(10);
            Assert.Greater(logsBefore.Length, 0);

            // Act
            EntitySystemLogger.ClearHistory();

            // Assert
            var logsAfter = EntitySystemLogger.GetRecentLogs(10);
            Assert.AreEqual(0, logsAfter.Length);
        }

        [Test]
        public void GetRecentLogs_WithLimit_ReturnsCorrectCount()
        {
            // Arrange
            for (int i = 0; i < 10; i++)
            {
                EntitySystemLogger.LogInfo("TestContext", $"Message {i}");
            }

            // Act
            var recentLogs = EntitySystemLogger.GetRecentLogs(5);

            // Assert
            Assert.AreEqual(5, recentLogs.Length);
            // Should return the most recent logs
            Assert.AreEqual("Message 9", recentLogs[4].Message);
            Assert.AreEqual("Message 8", recentLogs[3].Message);
        }

        [Test]
        public void LogEntry_NullContext_UsesDefaultContext()
        {
            // Act
            EntitySystemLogger.LogInfo(null, "Test message");

            // Assert
            var logs = EntitySystemLogger.GetRecentLogs(1);
            Assert.AreEqual("Unknown", logs[0].Context);
        }

        [Test]
        public void LogEntry_NullMessage_UsesDefaultMessage()
        {
            // Act
            EntitySystemLogger.LogInfo("TestContext", null);

            // Assert
            var logs = EntitySystemLogger.GetRecentLogs(1);
            Assert.AreEqual("No message", logs[0].Message);
        }

        [Test]
        public void ExportLogs_ValidPath_ExportsSuccessfully()
        {
            // Arrange
            EntitySystemLogger.LogInfo("TestContext", "Test message for export");
            var tempPath = System.IO.Path.GetTempFileName();

            try
            {
                // Act
                bool result = EntitySystemLogger.ExportLogs(tempPath);

                // Assert
                Assert.IsTrue(result);
                Assert.IsTrue(System.IO.File.Exists(tempPath));
                
                var content = System.IO.File.ReadAllText(tempPath);
                Assert.IsTrue(content.Contains("Test message for export"));
            }
            finally
            {
                // Cleanup
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }

        [Test]
        public void ExportLogs_InvalidPath_ReturnsFalse()
        {
            // Arrange
            var invalidPath = "/invalid/path/that/does/not/exist/log.txt";

            // Act
            bool result = EntitySystemLogger.ExportLogs(invalidPath);

            // Assert
            Assert.IsFalse(result);
        }
    }
}
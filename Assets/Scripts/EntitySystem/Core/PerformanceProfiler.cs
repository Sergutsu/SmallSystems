using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Performance profiling utilities for the Entity System
    /// </summary>
    public static class PerformanceProfiler
    {
        private static Dictionary<string, ProfileData> _profiles = new();
        private static Dictionary<string, Stopwatch> _activeTimers = new();
        private static bool _isEnabled = true;

        /// <summary>
        /// Enable or disable profiling
        /// </summary>
        public static void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            if (!enabled)
            {
                _activeTimers.Clear();
            }
        }

        /// <summary>
        /// Start timing a profile section
        /// </summary>
        public static void BeginProfile(string profileName)
        {
            if (!_isEnabled) return;

            if (!_activeTimers.ContainsKey(profileName))
            {
                _activeTimers[profileName] = new Stopwatch();
            }

            _activeTimers[profileName].Restart();
        }

        /// <summary>
        /// End timing a profile section
        /// </summary>
        public static void EndProfile(string profileName)
        {
            if (!_isEnabled) return;

            if (_activeTimers.TryGetValue(profileName, out var stopwatch))
            {
                stopwatch.Stop();
                RecordProfileData(profileName, stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Profile a code block using a using statement
        /// </summary>
        public static ProfileScope Profile(string profileName)
        {
            return new ProfileScope(profileName);
        }

        /// <summary>
        /// Record profile data
        /// </summary>
        private static void RecordProfileData(string profileName, long elapsedMs)
        {
            if (!_profiles.ContainsKey(profileName))
            {
                _profiles[profileName] = new ProfileData(profileName);
            }

            _profiles[profileName].AddSample(elapsedMs);
        }

        /// <summary>
        /// Get profile data for a specific profile
        /// </summary>
        public static ProfileData GetProfileData(string profileName)
        {
            return _profiles.GetValueOrDefault(profileName);
        }

        /// <summary>
        /// Get all profile data
        /// </summary>
        public static Dictionary<string, ProfileData> GetAllProfileData()
        {
            return new Dictionary<string, ProfileData>(_profiles);
        }

        /// <summary>
        /// Clear all profile data
        /// </summary>
        public static void ClearProfiles()
        {
            _profiles.Clear();
            _activeTimers.Clear();
        }

        /// <summary>
        /// Generate a performance report
        /// </summary>
        public static string GenerateReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("ENTITY SYSTEM PERFORMANCE REPORT");
            report.AppendLine("================================");
            report.AppendLine($"Generated: {DateTime.Now}");
            report.AppendLine();

            if (_profiles.Count == 0)
            {
                report.AppendLine("No profile data available.");
                return report.ToString();
            }

            report.AppendLine("Profile Name                 | Samples | Avg (ms) | Min (ms) | Max (ms) | Total (ms)");
            report.AppendLine("----------------------------|---------|----------|----------|----------|----------");

            foreach (var kvp in _profiles)
            {
                var data = kvp.Value;
                report.AppendLine($"{data.Name,-28} | {data.SampleCount,7} | {data.AverageMs,8:F2} | {data.MinMs,8:F2} | {data.MaxMs,8:F2} | {data.TotalMs,9:F2}");
            }

            return report.ToString();
        }
    }

    /// <summary>
    /// Profile data for a specific profile section
    /// </summary>
    public class ProfileData
    {
        public string Name { get; }
        public int SampleCount { get; private set; }
        public float TotalMs { get; private set; }
        public float AverageMs => SampleCount > 0 ? TotalMs / SampleCount : 0f;
        public float MinMs { get; private set; } = float.MaxValue;
        public float MaxMs { get; private set; }

        public ProfileData(string name)
        {
            Name = name;
        }

        public void AddSample(long elapsedMs)
        {
            SampleCount++;
            TotalMs += elapsedMs;
            MinMs = Mathf.Min(MinMs, elapsedMs);
            MaxMs = Mathf.Max(MaxMs, elapsedMs);
        }

        public void Reset()
        {
            SampleCount = 0;
            TotalMs = 0f;
            MinMs = float.MaxValue;
            MaxMs = 0f;
        }
    }

    /// <summary>
    /// Disposable profile scope for using statements
    /// </summary>
    public struct ProfileScope : IDisposable
    {
        private readonly string _profileName;

        public ProfileScope(string profileName)
        {
            _profileName = profileName;
            PerformanceProfiler.BeginProfile(_profileName);
        }

        public void Dispose()
        {
            PerformanceProfiler.EndProfile(_profileName);
        }
    }
}
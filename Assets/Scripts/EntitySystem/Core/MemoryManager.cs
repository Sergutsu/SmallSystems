using System;
using System.Collections.Generic;
using UnityEngine;

namespace GalacticVentures.EntitySystem.Core
{
    /// <summary>
    /// Memory management utilities for the Entity System
    /// </summary>
    public static class MemoryManager
    {
        private static readonly Dictionary<Type, ObjectPool<object>> _componentPools = new();
        private static readonly ObjectPool<List<GameEntity>> _listPool = new(50, () => new List<GameEntity>(), list => list.Clear());
        private static readonly ObjectPool<HashSet<string>> _hashSetPool = new(50, () => new HashSet<string>(), set => set.Clear());
        
        private static long _lastGCMemory = 0;
        private static float _lastGCTime = 0;
        private static int _gcCollections = 0;

        /// <summary>
        /// Initialize memory management systems
        /// </summary>
        public static void Initialize()
        {
            _lastGCMemory = GC.GetTotalMemory(false);
            _lastGCTime = Time.realtimeSinceStartup;
            _gcCollections = GC.CollectionCount(0);

            EntitySystemLogger.LogInfo("MemoryManager", "Memory management initialized");
        }

        /// <summary>
        /// Get a pooled list for temporary use
        /// </summary>
        public static List<GameEntity> GetTempList()
        {
            return _listPool.Get();
        }

        /// <summary>
        /// Return a list to the pool
        /// </summary>
        public static void ReturnTempList(List<GameEntity> list)
        {
            _listPool.Return(list);
        }

        /// <summary>
        /// Get a pooled HashSet for temporary use
        /// </summary>
        public static HashSet<string> GetTempHashSet()
        {
            return _hashSetPool.Get();
        }

        /// <summary>
        /// Return a HashSet to the pool
        /// </summary>
        public static void ReturnTempHashSet(HashSet<string> hashSet)
        {
            _hashSetPool.Return(hashSet);
        }

        /// <summary>
        /// Force garbage collection (use sparingly)
        /// </summary>
        public static void ForceGarbageCollection()
        {
            var beforeMemory = GC.GetTotalMemory(false);
            var beforeTime = Time.realtimeSinceStartup;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var afterMemory = GC.GetTotalMemory(false);
            var elapsedTime = Time.realtimeSinceStartup - beforeTime;
            var freedMemory = beforeMemory - afterMemory;

            EntitySystemLogger.LogInfo("MemoryManager", 
                $"Forced GC: Freed {freedMemory / 1024f / 1024f:F2} MB in {elapsedTime * 1000f:F2}ms");
        }

        /// <summary>
        /// Get current memory statistics
        /// </summary>
        public static MemoryStats GetMemoryStats()
        {
            var currentMemory = GC.GetTotalMemory(false);
            var currentTime = Time.realtimeSinceStartup;
            var currentGCCollections = GC.CollectionCount(0);

            var stats = new MemoryStats
            {
                TotalMemoryMB = currentMemory / 1024f / 1024f,
                MemoryDeltaMB = (currentMemory - _lastGCMemory) / 1024f / 1024f,
                TimeSinceLastGC = currentTime - _lastGCTime,
                GCCollectionsSinceStart = currentGCCollections - _gcCollections,
                ListPoolAvailable = _listPool.AvailableCount,
                ListPoolTotal = _listPool.TotalCreated,
                HashSetPoolAvailable = _hashSetPool.AvailableCount,
                HashSetPoolTotal = _hashSetPool.TotalCreated
            };

            return stats;
        }

        /// <summary>
        /// Monitor memory usage and trigger cleanup if needed
        /// </summary>
        public static void MonitorMemory()
        {
            var stats = GetMemoryStats();
            
            // Trigger cleanup if memory usage is high
            if (stats.TotalMemoryMB > 500f) // 500 MB threshold
            {
                EntitySystemLogger.LogWarning("MemoryManager", 
                    $"High memory usage detected: {stats.TotalMemoryMB:F2} MB");
                
                CleanupUnusedObjects();
            }

            // Log memory stats periodically
            if (stats.TimeSinceLastGC > 60f) // Every minute
            {
                EntitySystemLogger.LogDebug("MemoryManager", 
                    $"Memory: {stats.TotalMemoryMB:F2} MB, GC Collections: {stats.GCCollectionsSinceStart}");
                
                _lastGCTime = Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// Cleanup unused objects and pools
        /// </summary>
        public static void CleanupUnusedObjects()
        {
            // Clear component pools
            foreach (var pool in _componentPools.Values)
            {
                pool.Clear();
            }

            // Trim collection pools if they're too large
            if (_listPool.TotalCreated > 100)
            {
                _listPool.Clear();
                _listPool.PreWarm(10);
            }

            if (_hashSetPool.TotalCreated > 100)
            {
                _hashSetPool.Clear();
                _hashSetPool.PreWarm(10);
            }

            EntitySystemLogger.LogInfo("MemoryManager", "Cleanup completed");
        }

        /// <summary>
        /// Pre-warm object pools
        /// </summary>
        public static void PreWarmPools()
        {
            _listPool.PreWarm(20);
            _hashSetPool.PreWarm(20);

            EntitySystemLogger.LogInfo("MemoryManager", "Object pools pre-warmed");
        }

        /// <summary>
        /// Get memory usage report
        /// </summary>
        public static string GenerateMemoryReport()
        {
            var stats = GetMemoryStats();
            var report = new System.Text.StringBuilder();

            report.AppendLine("ENTITY SYSTEM MEMORY REPORT");
            report.AppendLine("===========================");
            report.AppendLine($"Generated: {DateTime.Now}");
            report.AppendLine();

            report.AppendLine("MEMORY USAGE");
            report.AppendLine("------------");
            report.AppendLine($"Total Memory: {stats.TotalMemoryMB:F2} MB");
            report.AppendLine($"Memory Delta: {stats.MemoryDeltaMB:F2} MB");
            report.AppendLine($"Time Since Last GC: {stats.TimeSinceLastGC:F2} seconds");
            report.AppendLine($"GC Collections: {stats.GCCollectionsSinceStart}");
            report.AppendLine();

            report.AppendLine("OBJECT POOLS");
            report.AppendLine("------------");
            report.AppendLine($"List Pool: {stats.ListPoolAvailable}/{stats.ListPoolTotal} available");
            report.AppendLine($"HashSet Pool: {stats.HashSetPoolAvailable}/{stats.HashSetPoolTotal} available");
            report.AppendLine($"Component Pools: {_componentPools.Count} types");
            report.AppendLine();

            // Unity-specific memory info
            if (Application.isPlaying)
            {
                report.AppendLine("UNITY MEMORY");
                report.AppendLine("------------");
                report.AppendLine($"Unity Used Memory: {Profiler.GetTotalAllocatedMemory(Profiler.Area.All) / 1024f / 1024f:F2} MB");
                report.AppendLine($"Unity Reserved Memory: {Profiler.GetTotalReservedMemory(Profiler.Area.All) / 1024f / 1024f:F2} MB");
            }

            return report.ToString();
        }
    }

    /// <summary>
    /// Memory usage statistics
    /// </summary>
    [Serializable]
    public struct MemoryStats
    {
        public float TotalMemoryMB;
        public float MemoryDeltaMB;
        public float TimeSinceLastGC;
        public int GCCollectionsSinceStart;
        public int ListPoolAvailable;
        public int ListPoolTotal;
        public int HashSetPoolAvailable;
        public int HashSetPoolTotal;
    }
}
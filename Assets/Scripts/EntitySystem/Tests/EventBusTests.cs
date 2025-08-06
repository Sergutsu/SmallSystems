using System;
using NUnit.Framework;
using UnityEngine;
using GalacticVentures.EntitySystem.Core;
using GalacticVentures.EntitySystem.Events;

namespace GalacticVentures.EntitySystem.Tests
{
    /// <summary>
    /// Unit tests for the EventBus system
    /// </summary>
    public class EventBusTests
    {
        private EventBus _eventBus;
        private bool _eventReceived;
        private IGameEvent _receivedEvent;

        [SetUp]
        public void Setup()
        {
            _eventBus = ScriptableObject.CreateInstance<EventBus>();
            _eventReceived = false;
            _receivedEvent = null;
        }

        [TearDown]
        public void TearDown()
        {
            _eventBus?.Clear();
            if (_eventBus != null)
            {
                ScriptableObject.DestroyImmediate(_eventBus);
            }
        }

        [Test]
        public void Subscribe_ValidHandler_HandlerRegistered()
        {
            // Arrange
            Action<ComponentAddedEvent> handler = (evt) => { _eventReceived = true; };

            // Act
            _eventBus.Subscribe(handler);
            var stats = _eventBus.GetStats();

            // Assert
            Assert.AreEqual(1, stats.HandlerCount);
            Assert.AreEqual(1, stats.RegisteredEventTypes);
        }

        [Test]
        public void TriggerEvent_CriticalPriority_ProcessedImmediately()
        {
            // Arrange
            var testEvent = new TestCriticalEvent();
            _eventBus.Subscribe<TestCriticalEvent>((evt) => { 
                _eventReceived = true; 
                _receivedEvent = evt;
            });

            // Act
            _eventBus.TriggerEvent(testEvent);

            // Assert
            Assert.IsTrue(_eventReceived);
            Assert.AreEqual(testEvent, _receivedEvent);
        }

        [Test]
        public void TriggerEvent_NormalPriority_QueuedForLaterProcessing()
        {
            // Arrange
            var testEvent = new TestNormalEvent();
            _eventBus.Subscribe<TestNormalEvent>((evt) => { 
                _eventReceived = true; 
                _receivedEvent = evt;
            });

            // Act
            _eventBus.TriggerEvent(testEvent);

            // Assert - Event should not be processed yet
            Assert.IsFalse(_eventReceived);
            
            // Process queued events
            _eventBus.ProcessQueuedEvents();
            
            // Assert - Now event should be processed
            Assert.IsTrue(_eventReceived);
            Assert.AreEqual(testEvent, _receivedEvent);
        }

        [Test]
        public void ProcessQueuedEvents_MultipleEvents_ProcessedInPriorityOrder()
        {
            // Arrange
            var processOrder = new System.Collections.Generic.List<string>();
            
            _eventBus.Subscribe<TestHighPriorityEvent>((evt) => processOrder.Add("High"));
            _eventBus.Subscribe<TestNormalEvent>((evt) => processOrder.Add("Normal"));
            _eventBus.Subscribe<TestLowPriorityEvent>((evt) => processOrder.Add("Low"));

            // Add events in reverse priority order
            _eventBus.TriggerEvent(new TestLowPriorityEvent());
            _eventBus.TriggerEvent(new TestNormalEvent());
            _eventBus.TriggerEvent(new TestHighPriorityEvent());

            // Act
            _eventBus.ProcessQueuedEvents();

            // Assert - Should be processed in priority order (High, Normal, Low)
            Assert.AreEqual(3, processOrder.Count);
            Assert.AreEqual("High", processOrder[0]);
            Assert.AreEqual("Normal", processOrder[1]);
            Assert.AreEqual("Low", processOrder[2]);
        }

        [Test]
        public void TriggerEvent_HandlerThrowsException_OtherHandlersContinueProcessing()
        {
            // Arrange
            var handler1Called = false;
            var handler2Called = false;
            var handler3Called = false;

            _eventBus.Subscribe<TestNormalEvent>((evt) => { handler1Called = true; });
            _eventBus.Subscribe<TestNormalEvent>((evt) => { throw new Exception("Test exception"); });
            _eventBus.Subscribe<TestNormalEvent>((evt) => { handler3Called = true; });

            // Act
            _eventBus.TriggerEvent(new TestNormalEvent());
            _eventBus.ProcessQueuedEvents();

            // Assert - First and third handlers should have been called despite exception in second
            Assert.IsTrue(handler1Called);
            Assert.IsTrue(handler3Called);
        }

        [Test]
        public void Clear_WithHandlersAndEvents_RemovesAllHandlersAndEvents()
        {
            // Arrange
            _eventBus.Subscribe<TestNormalEvent>((evt) => { });
            _eventBus.TriggerEvent(new TestNormalEvent());
            
            var statsBefore = _eventBus.GetStats();
            Assert.Greater(statsBefore.HandlerCount, 0);
            Assert.Greater(statsBefore.QueuedEventCount, 0);

            // Act
            _eventBus.Clear();

            // Assert
            var statsAfter = _eventBus.GetStats();
            Assert.AreEqual(0, statsAfter.HandlerCount);
            Assert.AreEqual(0, statsAfter.QueuedEventCount);
            Assert.AreEqual(0, statsAfter.RegisteredEventTypes);
        }

        // Test event classes
        private class TestCriticalEvent : GameEvent
        {
            public TestCriticalEvent() : base(null, EventPriority.Critical) { }
        }

        private class TestHighPriorityEvent : GameEvent
        {
            public TestHighPriorityEvent() : base(null, EventPriority.High) { }
        }

        private class TestNormalEvent : GameEvent
        {
            public TestNormalEvent() : base(null, EventPriority.Normal) { }
        }

        private class TestLowPriorityEvent : GameEvent
        {
            public TestLowPriorityEvent() : base(null, EventPriority.Low) { }
        }
    }
}
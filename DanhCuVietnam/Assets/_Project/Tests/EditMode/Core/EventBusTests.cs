using System;
using System.Collections.Generic;
using DanhCu.Core.Diagnostics;
using DanhCu.Core.Events;
using NUnit.Framework;

namespace DanhCu.Tests.EditMode.Core
{
    public sealed class EventBusTests
    {
        private readonly struct Ping
        {
            public Ping(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        private sealed class CapturingSink : ILogSink
        {
            public List<Exception> Exceptions { get; } = new List<Exception>();

            public void Info(string message)
            {
            }

            public void Warning(string message)
            {
            }

            public void Error(string message)
            {
            }

            public void Exception(Exception exception) => Exceptions.Add(exception);
        }

        private ILogSink _previousSink;
        private CapturingSink _sink;

        [SetUp]
        public void SetUp()
        {
            _previousSink = Log.Sink;
            _sink = new CapturingSink();
            Log.Sink = _sink;
        }

        [TearDown]
        public void TearDown()
        {
            Log.Sink = _previousSink;
        }

        [Test]
        public void Publish_DeliversToSubscribers()
        {
            var bus = new EventBus();
            int received = 0;
            bus.Subscribe<Ping>(p => received = p.Value);

            bus.Publish(new Ping(42));

            Assert.That(received, Is.EqualTo(42));
        }

        [Test]
        public void Publish_WithoutSubscribers_DoesNothing()
        {
            Assert.DoesNotThrow(() => new EventBus().Publish(new Ping(1)));
        }

        [Test]
        public void DisposingSubscription_StopsDelivery()
        {
            var bus = new EventBus();
            int count = 0;
            IDisposable subscription = bus.Subscribe<Ping>(_ => count++);

            subscription.Dispose();
            subscription.Dispose();
            bus.Publish(new Ping(1));

            Assert.That(count, Is.EqualTo(0));
            Assert.That(bus.SubscriberCount<Ping>(), Is.EqualTo(0));
        }

        [Test]
        public void ThrowingHandler_DoesNotBlockOthers_AndIsLogged()
        {
            var bus = new EventBus();
            bool secondCalled = false;
            bus.Subscribe<Ping>(_ => throw new InvalidOperationException("boom"));
            bus.Subscribe<Ping>(_ => secondCalled = true);

            bus.Publish(new Ping(1));

            Assert.That(secondCalled, Is.True);
            Assert.That(_sink.Exceptions, Has.Count.EqualTo(1));
        }

        [Test]
        public void UnsubscribingDuringPublish_IsSafe_AndTakesEffectNextPublish()
        {
            var bus = new EventBus();
            int secondCount = 0;
            IDisposable second = null;
            bus.Subscribe<Ping>(_ => second?.Dispose());
            second = bus.Subscribe<Ping>(_ => secondCount++);

            bus.Publish(new Ping(1));
            bus.Publish(new Ping(2));

            Assert.That(secondCount, Is.EqualTo(1));
        }

        [Test]
        public void EventsAreRoutedByType()
        {
            var bus = new EventBus();
            int pings = 0;
            int strings = 0;
            bus.Subscribe<Ping>(_ => pings++);
            bus.Subscribe<string>(_ => strings++);

            bus.Publish(new Ping(1));

            Assert.That(pings, Is.EqualTo(1));
            Assert.That(strings, Is.EqualTo(0));
        }
    }
}

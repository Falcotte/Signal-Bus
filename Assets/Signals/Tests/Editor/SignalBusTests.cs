using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace AngryKoala.Signals.Tests
{
    public sealed class SignalBusTests
    {
        private readonly struct TestSignal : ISignal
        {
            public int Value { get; }

            public TestSignal(int value)
            {
                Value = value;
            }
        }

        private readonly struct OtherTestSignal : ISignal
        {
        }

        private sealed class Subscriber
        {
            public int CallCount { get; private set; }

            public void HandleSignal(TestSignal signal)
            {
                CallCount++;
            }
        }

        private int _callCount;
        private int _lastValue;

        [SetUp]
        public void SetUp()
        {
            SignalBus.ClearAll();

            _callCount = 0;
            _lastValue = 0;
        }

        [TearDown]
        public void TearDown()
        {
            SignalBus.ClearAll();
        }

        [Test]
        public void Publish_CallsSubscriberWithSignal()
        {
            SignalBus.Subscribe<TestSignal>(HandleSignal);

            SignalBus.Publish(new TestSignal(7));

            Assert.That(_callCount, Is.EqualTo(1));
            Assert.That(_lastValue, Is.EqualTo(7));
        }

        [Test]
        public void Publish_DoesNothing_WhenNobodySubscribed()
        {
            SignalBus.Publish(new TestSignal(1));

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void Publish_KeepsSignalTypesApart()
        {
            SignalBus.Subscribe<TestSignal>(HandleSignal);

            SignalBus.Publish(new OtherTestSignal());

            Assert.That(_callCount, Is.Zero);
        }

        [Test]
        public void Publish_DoesNotAllocate()
        {
            var signal = new TestSignal(1);
            SignalBus.Subscribe<TestSignal>(HandleSignal);
            SignalBus.Publish(signal);

            Assert.That(() => SignalBus.Publish(signal), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Publish_KeepsCallingOthers_WhenOneSubscriberThrows()
        {
            SignalBus.Subscribe<TestSignal>(_ => throw new InvalidOperationException("Test"));
            SignalBus.Subscribe<TestSignal>(HandleSignal);

            LogAssert.Expect(LogType.Error, new Regex(nameof(TestSignal)));
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException"));
            SignalBus.Publish(new TestSignal(1));

            Assert.That(_callCount, Is.EqualTo(1));
        }

        [Test]
        public void Publish_SkipsSubscriberRemovedDuringPublish()
        {
            SignalBus.Subscribe<TestSignal>(_ => SignalBus.Unsubscribe<TestSignal>(HandleSignal));
            SignalBus.Subscribe<TestSignal>(HandleSignal);

            SignalBus.Publish(new TestSignal(1));

            Assert.That(_callCount, Is.Zero);
        }

        [Test]
        public void Publish_SkipsRemainingSubscribers_WhenClearedDuringPublish()
        {
            SignalBus.Subscribe<TestSignal>(_ => SignalBus.Clear<TestSignal>());
            SignalBus.Subscribe<TestSignal>(HandleSignal);

            SignalBus.Publish(new TestSignal(1));
            SignalBus.Publish(new TestSignal(1));

            Assert.That(_callCount, Is.Zero);
        }

        [Test]
        public void Publish_CallsSubscriberAddedDuringPublish_OnlyFromNextPublish()
        {
            SignalBus.SubscribeOneShot<TestSignal>(_ => SignalBus.Subscribe<TestSignal>(HandleSignal));

            SignalBus.Publish(new TestSignal(1));
            Assert.That(_callCount, Is.Zero);

            SignalBus.Publish(new TestSignal(1));
            Assert.That(_callCount, Is.EqualTo(1));
        }

        [Test]
        public void Publish_SupportsPublishingInsideHandler()
        {
            var values = new List<int>();
            SignalBus.Subscribe<TestSignal>(signal =>
            {
                values.Add(signal.Value);

                if (signal.Value < 3)
                {
                    SignalBus.Publish(new TestSignal(signal.Value + 1));
                }
            });

            SignalBus.Publish(new TestSignal(1));
            SignalBus.Publish(new TestSignal(10));

            Assert.That(values, Is.EqualTo(new[] { 1, 2, 3, 10 }));
        }

        [Test]
        public void Subscribe_Throws_WhenCallbackIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => SignalBus.Subscribe<TestSignal>(null));
        }

        [Test]
        public void Subscribe_IgnoresSameHandlerTwice_WithWarning()
        {
            SignalBus.Subscribe<TestSignal>(HandleSignal);

            LogAssert.Expect(LogType.Warning, new Regex("already subscribed"));
            SignalBus.Subscribe<TestSignal>(HandleSignal);

            SignalBus.Publish(new TestSignal(1));

            Assert.That(_callCount, Is.EqualTo(1));
        }

        [Test]
        public void Unsubscribe_StopsCalls()
        {
            SignalBus.Subscribe<TestSignal>(HandleSignal);
            SignalBus.Unsubscribe<TestSignal>(HandleSignal);

            SignalBus.Publish(new TestSignal(1));

            Assert.That(_callCount, Is.Zero);
        }

        [Test]
        public void UnsubscribeAll_RemovesEveryHandlerOfTarget()
        {
            var subscriber = new Subscriber();
            var otherSubscriber = new Subscriber();
            SignalBus.Subscribe<TestSignal>(subscriber.HandleSignal);
            SignalBus.Subscribe<TestSignal>(otherSubscriber.HandleSignal);

            SignalBus.UnsubscribeAll(subscriber);
            SignalBus.Publish(new TestSignal(1));

            Assert.That(subscriber.CallCount, Is.Zero);
            Assert.That(otherSubscriber.CallCount, Is.EqualTo(1));
        }

        [Test]
        public void SubscribeOneShot_CallsOnce()
        {
            SignalBus.SubscribeOneShot<TestSignal>(HandleSignal);

            SignalBus.Publish(new TestSignal(1));
            SignalBus.Publish(new TestSignal(2));

            Assert.That(_callCount, Is.EqualTo(1));
            Assert.That(_lastValue, Is.EqualTo(1));
        }

        [Test]
        public void SubscribeOneShot_CallsOnce_WhenHandlerPublishesAgain()
        {
            int callCount = 0;
            SignalBus.SubscribeOneShot<TestSignal>(_ =>
            {
                callCount++;
                SignalBus.Publish(new TestSignal(2));
            });

            SignalBus.Publish(new TestSignal(1));

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void Unsubscribe_RemovesOneShot()
        {
            SignalBus.SubscribeOneShot<TestSignal>(HandleSignal);
            SignalBus.Unsubscribe<TestSignal>(HandleSignal);

            SignalBus.Publish(new TestSignal(1));

            Assert.That(_callCount, Is.Zero);
        }

        [Test]
        public void UnsubscribeAll_RemovesOneShot()
        {
            var subscriber = new Subscriber();
            SignalBus.SubscribeOneShot<TestSignal>(subscriber.HandleSignal);

            SignalBus.UnsubscribeAll(subscriber);
            SignalBus.Publish(new TestSignal(1));

            Assert.That(subscriber.CallCount, Is.Zero);
        }

        [Test]
        public void Clear_RemovesSubscribersOfOneSignal()
        {
            int otherCallCount = 0;
            SignalBus.Subscribe<TestSignal>(HandleSignal);
            SignalBus.Subscribe<OtherTestSignal>(_ => otherCallCount++);

            SignalBus.Clear<TestSignal>();
            SignalBus.Publish(new TestSignal(1));
            SignalBus.Publish(new OtherTestSignal());

            Assert.That(_callCount, Is.Zero);
            Assert.That(otherCallCount, Is.EqualTo(1));
        }

        [Test]
        public void PublishRetained_ReachesCurrentSubscribers()
        {
            SignalBus.Subscribe<TestSignal>(HandleSignal);

            SignalBus.PublishRetained(new TestSignal(5));

            Assert.That(_callCount, Is.EqualTo(1));
            Assert.That(_lastValue, Is.EqualTo(5));
        }

        [Test]
        public void SubscribeWithRetained_CallsBackAtOnce_WithLatestRetainedSignal()
        {
            SignalBus.PublishRetained(new TestSignal(5));
            SignalBus.PublishRetained(new TestSignal(6));

            SignalBus.SubscribeWithRetained<TestSignal>(HandleSignal);

            Assert.That(_callCount, Is.EqualTo(1));
            Assert.That(_lastValue, Is.EqualTo(6));
        }

        [Test]
        public void SubscribeWithRetained_StaysSubscribed()
        {
            SignalBus.PublishRetained(new TestSignal(5));
            SignalBus.SubscribeWithRetained<TestSignal>(HandleSignal);

            SignalBus.Publish(new TestSignal(8));

            Assert.That(_callCount, Is.EqualTo(2));
            Assert.That(_lastValue, Is.EqualTo(8));
        }

        [Test]
        public void SubscribeWithRetained_DoesNotCallBack_WhenNothingIsRetained()
        {
            SignalBus.Publish(new TestSignal(5));

            SignalBus.SubscribeWithRetained<TestSignal>(HandleSignal);

            Assert.That(_callCount, Is.Zero);
        }

        [Test]
        public void Subscribe_DoesNotReceiveRetainedSignal()
        {
            SignalBus.PublishRetained(new TestSignal(5));

            SignalBus.Subscribe<TestSignal>(HandleSignal);

            Assert.That(_callCount, Is.Zero);
        }

        [Test]
        public void TryGetRetained_ReturnsLatestRetainedSignal()
        {
            Assert.That(SignalBus.TryGetRetained(out TestSignal _), Is.False);

            SignalBus.PublishRetained(new TestSignal(5));
            SignalBus.Publish(new TestSignal(9));

            Assert.That(SignalBus.TryGetRetained(out TestSignal signal), Is.True);
            Assert.That(signal.Value, Is.EqualTo(5));
        }

        [Test]
        public void TryGetRetained_SeesNewSignal_InsideItsOwnHandlers()
        {
            int retainedValue = 0;
            SignalBus.Subscribe<TestSignal>(_ =>
            {
                SignalBus.TryGetRetained(out TestSignal retainedSignal);
                retainedValue = retainedSignal.Value;
            });

            SignalBus.PublishRetained(new TestSignal(5));

            Assert.That(retainedValue, Is.EqualTo(5));
        }

        [Test]
        public void ClearRetained_ForgetsSignal_AndKeepsSubscribers()
        {
            SignalBus.Subscribe<TestSignal>(HandleSignal);
            SignalBus.PublishRetained(new TestSignal(5));

            SignalBus.ClearRetained<TestSignal>();
            SignalBus.Publish(new TestSignal(6));

            Assert.That(SignalBus.TryGetRetained(out TestSignal _), Is.False);
            Assert.That(_callCount, Is.EqualTo(2));
        }

        [Test]
        public void Clear_KeepsRetainedSignal()
        {
            SignalBus.PublishRetained(new TestSignal(5));

            SignalBus.Clear<TestSignal>();

            Assert.That(SignalBus.TryGetRetained(out TestSignal _), Is.True);
        }

        [Test]
        public void ClearAll_RemovesSubscribersAndRetainedSignals()
        {
            SignalBus.Subscribe<TestSignal>(HandleSignal);
            SignalBus.PublishRetained(new TestSignal(5));

            SignalBus.ClearAll();
            SignalBus.Publish(new TestSignal(6));

            Assert.That(SignalBus.TryGetRetained(out TestSignal _), Is.False);
            Assert.That(_callCount, Is.EqualTo(1));
        }

        [Test]
        public void ResetStatics_RemovesSubscribersAndRetainedSignals()
        {
            SignalBus.Subscribe<TestSignal>(HandleSignal);
            SignalBus.PublishRetained(new TestSignal(5));

            SignalBus.ResetStatics();
            SignalBus.Publish(new TestSignal(6));

            Assert.That(SignalBus.TryGetRetained(out TestSignal _), Is.False);
            Assert.That(_callCount, Is.EqualTo(1));
        }

        [Test]
        public void PublishRetained_DoesNotAllocate()
        {
            var signal = new TestSignal(1);
            SignalBus.Subscribe<TestSignal>(HandleSignal);
            SignalBus.PublishRetained(signal);

            Assert.That(() => SignalBus.PublishRetained(signal), Is.Not.AllocatingGCMemory());
        }

        private void HandleSignal(TestSignal signal)
        {
            _callCount++;
            _lastValue = signal.Value;
        }
    }
}

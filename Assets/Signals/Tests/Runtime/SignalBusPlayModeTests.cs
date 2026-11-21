using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AngryKoala.Signals.Tests
{
    public sealed class SignalBusPlayModeTests
    {
        private readonly List<GameObject> _gameObjects = new();

        [SetUp]
        public void SetUp()
        {
            SignalBus.ClearAll();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject gameObject in _gameObjects)
            {
                if (gameObject != null)
                {
                    Object.Destroy(gameObject);
                }
            }

            _gameObjects.Clear();

            yield return null;

            SignalBus.ClearAll();
        }

        [UnityTest]
        public IEnumerator Publish_CallsLiveSubscriber()
        {
            PlayModeTestSubscriber subscriber = Create();
            subscriber.Subscribe();

            yield return null;
            SignalBus.Publish(new PlayModeTestSignal());

            Assert.That(subscriber.CallCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Publish_SkipsDestroyedSubscriber_WithOneWarning()
        {
            PlayModeTestSubscriber destroyedSubscriber = Create();
            PlayModeTestSubscriber subscriber = Create();
            destroyedSubscriber.Subscribe();
            subscriber.Subscribe();

            Object.Destroy(destroyedSubscriber.gameObject);
            yield return null;

            LogAssert.Expect(LogType.Warning, new Regex(nameof(PlayModeTestSubscriber)));
            SignalBus.Publish(new PlayModeTestSignal());
            SignalBus.Publish(new PlayModeTestSignal());

            Assert.That(destroyedSubscriber.CallCount, Is.Zero);
            Assert.That(subscriber.CallCount, Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();
        }

        private PlayModeTestSubscriber Create()
        {
            var gameObject = new GameObject(nameof(PlayModeTestSubscriber));
            _gameObjects.Add(gameObject);

            return gameObject.AddComponent<PlayModeTestSubscriber>();
        }
    }
}

using System;
using System.Collections.Generic;

namespace AngryKoala.Signals
{
    public sealed class SignalBus
    {
        private readonly Dictionary<Type, ISubscriberCollection> _subscribers = new();

        private static readonly SignalBus Instance = new();

        private SignalBus()
        {
        }

        public static void Subscribe<TSignal>(Action<TSignal> callback) where TSignal : ISignal
        {
            Instance.SubscribeInternal(callback);
        }

        public static void Unsubscribe<TSignal>(Action<TSignal> callback) where TSignal : ISignal
        {
            Instance.UnsubscribeInternal(typeof(TSignal), callback);
        }

        public static void Publish<TSignal>(TSignal signal) where TSignal : ISignal
        {
            Instance.PublishInternal(signal);
        }

        #region Internal Methods

        private void SubscribeInternal<TSignal>(Action<TSignal> callback) where TSignal : ISignal
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var type = typeof(TSignal);

            if (!_subscribers.TryGetValue(type, out var subscriberCollection))
            {
                subscriberCollection = new SubscriberCollection<TSignal>();
                _subscribers[type] = subscriberCollection;
            }

            ((SubscriberCollection<TSignal>)subscriberCollection).Add(callback);
        }

        private void UnsubscribeInternal(Type type, Delegate callback)
        {
            if (type == null || callback == null)
            {
                return;
            }

            if (!_subscribers.TryGetValue(type, out var subscriberCollection))
            {
                return;
            }

            subscriberCollection.Remove(callback);

            if (subscriberCollection.Count == 0)
            {
                _subscribers.Remove(type);
            }
        }

        private void PublishInternal<TSignal>(TSignal signal) where TSignal : ISignal
        {
            if (!_subscribers.TryGetValue(typeof(TSignal), out var subscriberCollection))
            {
                return;
            }

            subscriberCollection.Publish(signal);
        }

        #endregion
    }
}
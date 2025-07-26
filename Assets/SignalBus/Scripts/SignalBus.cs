using System;
using System.Collections.Generic;

namespace AngryKoala.Signals
{
    public sealed class SignalBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

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
            Instance.UnsubscribeInternal(callback);
        }

        public static void Publish<TSignal>(TSignal signal) where TSignal : ISignal
        {
            Instance.PublishInternal(signal);
        }

        #region Internal Methods

        private void SubscribeInternal<TSignal>(Action<TSignal> callback) where TSignal : ISignal
        {
            var type = typeof(TSignal);

            if (!_subscribers.ContainsKey(type))
            {
                _subscribers[type] = new List<Delegate>();
            }

            _subscribers[type].Add(callback);
        }

        private void UnsubscribeInternal<TSignal>(Action<TSignal> callback) where TSignal : ISignal
        {
            var type = typeof(TSignal);

            if (!_subscribers.TryGetValue(type, out var list)) return;

            list.Remove(callback);
            if (list.Count == 0)
            {
                _subscribers.Remove(type);
            }
        }

        private void PublishInternal<TSignal>(TSignal signal) where TSignal : ISignal
        {
            var type = typeof(TSignal);

            if (!_subscribers.TryGetValue(type, out var list)) return;

            var copy = list.ToArray();

            foreach (Action<TSignal> callback in copy)
            {
                callback?.Invoke(signal);
            }
        }

        #endregion
    }
}
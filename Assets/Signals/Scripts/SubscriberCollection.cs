using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AngryKoala.Signals
{
    internal sealed class SubscriberCollection<TSignal> : ISubscriberCollection where TSignal : ISignal
    {
        public Type SignalType => typeof(TSignal);

        public int Count => _count;

        public bool HasRetainedSignal => _hasRetainedSignal;

        private readonly List<Subscription> _subscriptions = new();
        private int _count;

        private int _publishDepth;
        private bool _hasRemovedSubscriptions;

        private TSignal _retainedSignal;
        private bool _hasRetainedSignal;

        private static readonly Predicate<Subscription> _isRemoved = subscription => subscription.Callback == null;

        public bool Add(Action<TSignal> callback, bool isOneShot)
        {
            if (IndexOf(callback) >= 0)
            {
                Debug.LogWarning(
                    $"{GetSubscriberName(callback)} is already subscribed to {typeof(TSignal).Name}. The second subscription is ignored.",
                    callback.Target as Object);
                return false;
            }

            _subscriptions.Add(new Subscription(callback, isOneShot));
            _count++;

            return true;
        }

        public void Remove(Action<TSignal> callback)
        {
            int index = IndexOf(callback);

            if (index >= 0)
            {
                RemoveAt(index);
            }
        }

        public void RemoveAll(object target)
        {
            for (int i = _subscriptions.Count - 1; i >= 0; i--)
            {
                Action<TSignal> callback = _subscriptions[i].Callback;

                if (callback != null && ReferenceEquals(callback.Target, target))
                {
                    RemoveAt(i);
                }
            }
        }

        public void Clear()
        {
            for (int i = _subscriptions.Count - 1; i >= 0; i--)
            {
                if (_subscriptions[i].Callback != null)
                {
                    RemoveAt(i);
                }
            }
        }

        public void Publish(TSignal signal)
        {
            // Subscriptions added while publishing sit past this count and are first called by the next publish.
            int subscriptionCount = _subscriptions.Count;

            _publishDepth++;

            try
            {
                for (int i = 0; i < subscriptionCount; i++)
                {
                    Subscription subscription = _subscriptions[i];
                    Action<TSignal> callback = subscription.Callback;

                    if (callback == null)
                    {
                        continue;
                    }

                    if (callback.Target is Object unityTarget && unityTarget == null)
                    {
                        RemoveAt(i);
                        Debug.LogWarning(
                            $"{GetSubscriberName(callback)} was subscribed to {typeof(TSignal).Name} on a destroyed object and has been removed. Unsubscribe in OnDisable or OnDestroy.");
                        continue;
                    }

                    if (subscription.IsOneShot)
                    {
                        RemoveAt(i);
                    }

                    try
                    {
                        callback.Invoke(signal);
                    }
                    catch (Exception exception)
                    {
                        LogSubscriberException(callback, exception);
                    }
                }
            }
            finally
            {
                _publishDepth--;

                if (_publishDepth == 0 && _hasRemovedSubscriptions)
                {
                    _subscriptions.RemoveAll(_isRemoved);
                    _hasRemovedSubscriptions = false;
                }
            }
        }

        public void SetRetained(TSignal signal)
        {
            _retainedSignal = signal;
            _hasRetainedSignal = true;
        }

        public bool TryGetRetained(out TSignal signal)
        {
            signal = _retainedSignal;
            return _hasRetainedSignal;
        }

        public void ClearRetained()
        {
            _retainedSignal = default;
            _hasRetainedSignal = false;
        }

        public void GetCallbacks(List<Delegate> results)
        {
            results.Clear();

            foreach (Subscription subscription in _subscriptions)
            {
                if (subscription.Callback != null)
                {
                    results.Add(subscription.Callback);
                }
            }
        }

        private int IndexOf(Action<TSignal> callback)
        {
            for (int i = 0; i < _subscriptions.Count; i++)
            {
                if (_subscriptions[i].Callback == callback)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RemoveAt(int index)
        {
            _count--;

            if (_publishDepth == 0)
            {
                _subscriptions.RemoveAt(index);
                return;
            }

            // A publish is walking the list by index, so the slot is emptied now and dropped when it ends.
            _subscriptions[index] = default;
            _hasRemovedSubscriptions = true;
        }

        private static void LogSubscriberException(Action<TSignal> callback, Exception exception)
        {
            string message = $"{GetSubscriberName(callback)} threw an exception while handling {typeof(TSignal).Name}";

            if (callback.Target is Object unityTarget)
            {
                Debug.LogError(message, unityTarget);
                Debug.LogException(exception, unityTarget);
                return;
            }

            Debug.LogError(message);
            Debug.LogException(exception);
        }

        private static string GetSubscriberName(Action<TSignal> callback)
        {
            return $"{callback.Method.DeclaringType?.Name}.{callback.Method.Name}";
        }

        private readonly struct Subscription
        {
            public readonly Action<TSignal> Callback;
            public readonly bool IsOneShot;

            public Subscription(Action<TSignal> callback, bool isOneShot)
            {
                Callback = callback;
                IsOneShot = isOneShot;
            }
        }
    }
}

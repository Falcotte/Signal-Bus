using System;
using System.Collections.Generic;
using UnityEngine;

namespace AngryKoala.Signals
{
    public static class SignalBus
    {
        private static readonly Dictionary<Type, ISubscriberCollection> _collectionsByType = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStatics()
        {
            ClearAll();
            _collectionsByType.Clear();
        }

        public static void Subscribe<TSignal>(Action<TSignal> callback) where TSignal : ISignal
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            GetOrCreateCollection<TSignal>().Add(callback, isOneShot: false);
        }

        /// <summary>
        /// Subscribes for one publish only. The subscription can be removed like any other before it is called.
        /// </summary>
        public static void SubscribeOneShot<TSignal>(Action<TSignal> callback) where TSignal : ISignal
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            GetOrCreateCollection<TSignal>().Add(callback, isOneShot: true);
        }

        /// <summary>
        /// Subscribes and, when a signal of this type was published with <see cref="PublishRetained{TSignal}"/>,
        /// calls back with it at once.
        /// </summary>
        public static void SubscribeWithRetained<TSignal>(Action<TSignal> callback) where TSignal : ISignal
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            SubscriberCollection<TSignal> collection = GetOrCreateCollection<TSignal>();

            if (collection.Add(callback, isOneShot: false) && collection.TryGetRetained(out TSignal signal))
            {
                collection.Invoke(callback, signal);
            }
        }

        public static void Unsubscribe<TSignal>(Action<TSignal> callback) where TSignal : ISignal
        {
            if (callback == null)
            {
                return;
            }

            if (TryGetCollection(out SubscriberCollection<TSignal> collection))
            {
                collection.Remove(callback);
            }
        }

        /// <summary>
        /// Removes every subscription whose handler belongs to the given object, across all signal types. Subscribe
        /// with a method of the object: a lambda that captures a local belongs to a hidden object and is not found.
        /// </summary>
        public static void UnsubscribeAll(object target)
        {
            if (target == null)
            {
                return;
            }

            foreach (ISubscriberCollection collection in _collectionsByType.Values)
            {
                collection.RemoveAll(target);
            }
        }

        public static void Publish<TSignal>(TSignal signal) where TSignal : ISignal
        {
            if (TryGetCollection(out SubscriberCollection<TSignal> collection))
            {
                collection.Publish(signal);
            }
        }

        /// <summary>
        /// Publishes the signal and keeps it as the current one of its type, for later
        /// <see cref="SubscribeWithRetained{TSignal}"/> and <see cref="TryGetRetained{TSignal}"/> calls. Only for
        /// signals that say what is true now. The signal is held until <see cref="ClearRetained{TSignal}"/>,
        /// <see cref="ClearAll"/> or the next Play session, together with anything it references.
        /// </summary>
        public static void PublishRetained<TSignal>(TSignal signal) where TSignal : ISignal
        {
            SubscriberCollection<TSignal> collection = GetOrCreateCollection<TSignal>();

            collection.SetRetained(signal);
            collection.Publish(signal);
        }

        public static bool TryGetRetained<TSignal>(out TSignal signal) where TSignal : ISignal
        {
            if (TryGetCollection(out SubscriberCollection<TSignal> collection))
            {
                return collection.TryGetRetained(out signal);
            }

            signal = default;
            return false;
        }

        public static void ClearRetained<TSignal>() where TSignal : ISignal
        {
            if (TryGetCollection(out SubscriberCollection<TSignal> collection))
            {
                collection.ClearRetained();
            }
        }

        /// <summary>
        /// Removes every subscriber of one signal type. A retained signal of that type is kept.
        /// </summary>
        public static void Clear<TSignal>() where TSignal : ISignal
        {
            if (TryGetCollection(out SubscriberCollection<TSignal> collection))
            {
                collection.Clear();
            }
        }

        /// <summary>
        /// Removes every subscriber and every retained signal.
        /// </summary>
        public static void ClearAll()
        {
            foreach (ISubscriberCollection collection in _collectionsByType.Values)
            {
                collection.Clear();
                collection.ClearRetained();
            }
        }

        internal static void GetCollections(List<ISubscriberCollection> results)
        {
            results.Clear();
            results.AddRange(_collectionsByType.Values);
        }

        private static SubscriberCollection<TSignal> GetOrCreateCollection<TSignal>() where TSignal : ISignal
        {
            if (TryGetCollection(out SubscriberCollection<TSignal> collection))
            {
                return collection;
            }

            collection = new SubscriberCollection<TSignal>();
            _collectionsByType[typeof(TSignal)] = collection;

            return collection;
        }

        private static bool TryGetCollection<TSignal>(out SubscriberCollection<TSignal> collection)
            where TSignal : ISignal
        {
            if (_collectionsByType.TryGetValue(typeof(TSignal), out ISubscriberCollection registeredCollection))
            {
                collection = (SubscriberCollection<TSignal>)registeredCollection;
                return true;
            }

            collection = null;
            return false;
        }
    }
}

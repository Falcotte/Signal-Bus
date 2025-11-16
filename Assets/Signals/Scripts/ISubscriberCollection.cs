using System;

namespace AngryKoala.Signals
{
    public interface ISubscriberCollection
    {
        int Count { get; }

        void Remove(Delegate callback);

        void RemoveAll(object target);
        
        void Publish(object signal);
    }
}
using System;
using System.Collections.Generic;

namespace AngryKoala.Signals
{
    internal interface ISubscriberCollection
    {
        Type SignalType { get; }

        int Count { get; }

        bool HasRetainedSignal { get; }

        void RemoveAll(object target);

        void Clear();

        void ClearRetained();

        void GetCallbacks(List<Delegate> results);
    }
}

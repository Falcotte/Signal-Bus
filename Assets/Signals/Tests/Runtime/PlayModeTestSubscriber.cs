using UnityEngine;

namespace AngryKoala.Signals.Tests
{
    public sealed class PlayModeTestSubscriber : MonoBehaviour
    {
        private int _callCount;
        public int CallCount => _callCount;

        public void Subscribe()
        {
            SignalBus.Subscribe<PlayModeTestSignal>(HandleSignal);
        }

        private void HandleSignal(PlayModeTestSignal signal)
        {
            _callCount++;
        }
    }
}

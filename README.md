# Angry Koala Signals

A static, typed publish and subscribe bus for Unity. A system publishes a signal when something happened, and every listener subscribed to that signal type receives it. The publisher does not need to know who listens. The module has no dependencies on other modules.

**Use it for**

- Announcing that something happened, to listeners the publisher does not know, for example `PlayerDiedSignal` or `LevelLoadedSignal`.
- Keeping the current state of something for late subscribers, with `PublishRetained`. Only for signals that say what is true now (see the rules below).

**Do not use it for**

- Asking a system for something, or telling one system to do something. That is a service. Use the Service Locator instead.
- Per-frame data flow.
- Request and response. A publisher gets no reply from its subscribers.

## Install

Add the package to the project's `Packages/manifest.json`:

```json
"com.angrykoala.signals": "https://github.com/Falcotte/Signal-Bus.git?path=/Assets/Signals#v2.0.0"
```

Unity does not resolve git dependencies between packages. Each AngryKoala module must be listed in the project's own `manifest.json`.

Minimum Unity version: 6000.3 (from `package.json`). Developed and tested on Unity 6000.6.

## Public interface

```csharp
public static class SignalBus
{
    // Adds a handler for TSignal. A handler that is already subscribed is ignored with a warning.
    public static void Subscribe<TSignal>(Action<TSignal> callback) where TSignal : ISignal;

    // Adds a handler that is called for the next publish of TSignal only.
    public static void SubscribeOneShot<TSignal>(Action<TSignal> callback) where TSignal : ISignal;

    // Adds a handler, and calls it at once with the retained TSignal if there is one.
    public static void SubscribeWithRetained<TSignal>(Action<TSignal> callback) where TSignal : ISignal;

    // Removes the handler for TSignal. Does nothing if it is not subscribed.
    public static void Unsubscribe<TSignal>(Action<TSignal> callback) where TSignal : ISignal;

    // Removes every handler whose target is the given object, across all signal types.
    public static void UnsubscribeAll(object target);

    // Calls every handler subscribed to TSignal with the signal.
    public static void Publish<TSignal>(TSignal signal) where TSignal : ISignal;

    // Publishes the signal and keeps it as the current TSignal, for SubscribeWithRetained and TryGetRetained.
    public static void PublishRetained<TSignal>(TSignal signal) where TSignal : ISignal;

    // Returns true and the retained TSignal if one has been published and not cleared.
    public static bool TryGetRetained<TSignal>(out TSignal signal) where TSignal : ISignal;

    // Forgets the retained TSignal. Subscribers stay.
    public static void ClearRetained<TSignal>() where TSignal : ISignal;

    // Removes every subscriber of TSignal. The retained TSignal is kept.
    public static void Clear<TSignal>() where TSignal : ISignal;

    // Removes every subscriber and every retained signal, across all signal types.
    public static void ClearAll();
}

public interface ISignal
{
    // Marker interface. Any type that implements it can be published.
}
```

## Example

A signal is a `readonly struct`, nested in a static class that groups the signals of one area.

```csharp
using AngryKoala.Signals;
using UnityEngine;

public static class PlayerSignals
{
    public readonly struct PlayerDiedSignal : ISignal
    {
        public int PlayerId { get; }
        public string Cause { get; }

        public PlayerDiedSignal(int playerId, string cause)
        {
            PlayerId = playerId;
            Cause = cause;
        }
    }
}

public sealed class Health : MonoBehaviour
{
    private void Die()
    {
        SignalBus.Publish(new PlayerSignals.PlayerDiedSignal(42, "Lava"));
    }
}

public sealed class Scoreboard : MonoBehaviour
{
    private void OnEnable()
    {
        SignalBus.Subscribe<PlayerSignals.PlayerDiedSignal>(OnPlayerDied);
    }

    private void OnDisable()
    {
        SignalBus.UnsubscribeAll(this);
    }

    private void OnPlayerDied(PlayerSignals.PlayerDiedSignal signal)
    {
        Debug.Log($"Player {signal.PlayerId} died because of {signal.Cause}");
    }
}
```

Use `SubscribeOneShot` when a handler should run for the next publish only:

```csharp
SignalBus.SubscribeOneShot<PlayerSignals.PlayerDiedSignal>(OnFirstDeath);
```

Use a retained signal for a state that late subscribers must see. The publisher keeps the latest value. `SubscribeWithRetained` calls the handler at once if a value exists, and then on every later publish:

```csharp
public static class DataSignals
{
    public readonly struct DataLoadedSignal : ISignal
    {
        public int ItemCount { get; }

        public DataLoadedSignal(int itemCount)
        {
            ItemCount = itemCount;
        }
    }
}

// Publisher, when loading finishes
SignalBus.PublishRetained(new DataSignals.DataLoadedSignal(128));

// Subscriber, which may start after loading has finished
SignalBus.SubscribeWithRetained<DataSignals.DataLoadedSignal>(OnDataLoaded);

// Reads the current value without subscribing
if (SignalBus.TryGetRetained(out DataSignals.DataLoadedSignal signal))
{
    Debug.Log(signal.ItemCount);
}
```

## Rules

- A signal is a `readonly struct` that implements `ISignal`. Name it with the `Signal` suffix.
- Subscribe in `OnEnable` and unsubscribe in `OnDisable` with `UnsubscribeAll(this)`. Use `Clear` and `ClearAll` only in tests.
- A subscriber added during a publish is first called by the next publish of that signal.
- A subscriber removed during a publish is not called in that publish.
- A publish may run inside a handler. The nested publish reaches every subscriber, and the outer publish carries on.
- Subscribing the same handler twice is ignored and logs a warning. Each lambda is a new delegate, so keep a reference to a handler if you need to unsubscribe it later.
- A subscriber on a destroyed Unity object is removed when a publish reaches it, and a warning is logged. This applies only when the handler's target is that Unity object. A lambda that captures local variables has no Unity target, so it is not detected and is kept.
- An exception in one subscriber is logged and the other subscribers still run. The immediate call made by `SubscribeWithRetained` is handled the same way.
- Publishing allocates nothing.
- **Retained signals are only for signals that say what is true now**, such as "data is loaded" or "the game is in this state". Do not use them for events, such as "a player died", which happened once. A retained event is replayed to every subscriber that arrives later, long after it happened.
- `Clear<T>` keeps the retained signal of type `T`. `ClearAll` removes subscribers and retained signals.
- The bus's state is reset at the start of every Play session, so it works with domain reload disabled.
- Tests call `SignalBus.ClearAll()` in setup and teardown, so one test does not leave subscribers for the next.

## Signal Bus Inspector

Open it from **Angry Koala > Signals > Signal Bus Inspector**.

The window has two panels:

- **Publish a Signal** lists the signal types in two groups: Active (types with a subscriber or a retained signal) and All (every non-abstract `ISignal` type in the loaded assemblies). A search box filters both. Select a type to edit its public fields and properties, or to pick a constructor for a struct, then press Publish.
- **Subscribers** lists each active signal type with whether it has a retained signal, the number of subscribers, and each handler's target and method. A Ping button selects the Unity object that owns a handler.

The toolbar has Refresh and an Auto-refresh toggle. Auto-refresh is on by default and refreshes every 0.5 seconds. The window shows subscriptions in the running game, so use it in Play Mode.

## Requirements

- Unity 6000.3 or later.
- No other AngryKoala modules and no UPM packages.
- Tests need `com.unity.test-framework`.

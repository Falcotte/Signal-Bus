# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Workflow

This is a Unity project. There are no build scripts or CLI commands. Building, running, and testing all happen inside the Unity Editor. Open `Signal Bus.sln` in your IDE for code editing with full IntelliSense support.

Work happens on the `development` branch. `main` is the release branch.

To run the editor inspection tool: **Angry Koala → Signals → Signal Bus Inspector** (Play Mode only).

Tests live in `Assets/Signals/Tests/Editor` (Edit Mode) and `Assets/Signals/Tests/Runtime` (Play Mode).

## Distribution

The module is a UPM package. Its root is `Assets/Signals`, which holds `package.json`. Consumers install it from git with `?path=/Assets/Signals#<tag>`.

## Architecture

The module lives entirely under `Assets/Signals/` and implements a static, typed publish/subscribe bus. Its namespace is `AngryKoala.Signals`. The project is Unity 6000.6.4f1 (`ProjectSettings/ProjectVersion.txt`), and `package.json` requires 6000.3 or later.

### Assemblies

- `AngryKoala.Signals`: runtime code in `Scripts/`.
- `AngryKoala.Signals.Editor`: `Scripts/Editor/`, Editor only.
- `AngryKoala.Signals.Tests.Editor`: `Tests/Editor/`, Edit Mode tests.
- `AngryKoala.Signals.Tests.Runtime`: `Tests/Runtime/`, Play Mode tests.

`Scripts/AssemblyInfo.cs` grants internal access to the Editor and test assemblies. `SignalBus.ResetStatics` and the internal collection types are visible to them for that reason.

### Core Abstractions

**`ISignal`**: Marker interface. Any type that implements it can be published. Signals are `readonly struct` types with the `Signal` suffix.

**`SignalBus`** (static class): The public facade. Holds a `Dictionary<Type, ISubscriberCollection>` keyed by signal type. Methods: `Subscribe`, `SubscribeOneShot`, `SubscribeWithRetained`, `Unsubscribe`, `UnsubscribeAll`, `Publish`, `PublishRetained`, `TryGetRetained`, `ClearRetained`, `Clear`, and `ClearAll`. A collection is created on first use and stays in the dictionary, even after its last subscriber leaves. `ClearAll` empties it but keeps the entry. `ResetStatics` runs at subsystem registration, so every Play session starts empty.

**`ISubscriberCollection`** (internal): Non-generic interface the bus and the Editor window use to reach collections of any signal type. It exposes `SignalType`, `Count`, `HasRetainedSignal`, `RemoveAll`, `Clear`, `ClearRetained` and `GetCallbacks`.

**`SubscriberCollection<TSignal>`** (internal, sealed): Holds a list of subscriptions, each a callback and a one-shot flag, and the retained signal. It rejects a handler that is already present, with a warning. Removal during a publish leaves an empty slot that is compacted when the outermost publish ends.

### Editor Tool

**`SignalInspectorEditorWindow`** (`Scripts/Editor/`): An EditorWindow with two panels. Publish a Signal lists the active and all `ISignal` types, and publishes an instance of a chosen type. Subscribers lists each active type, whether it has a retained signal, and its handlers. It reads the bus through the internal `ISubscriberCollection` interface. Auto-refresh every 0.5 s is on by default.

### How a Publish Works

```
SignalBus.Publish<TSignal>(signal)
  → looks up SubscriberCollection<TSignal>; does nothing if none
  → records the subscription count, so subscribers added during the publish wait for the next one
  → for each subscription up to that count:
      - skips an empty slot (removed earlier in this publish)
      - if the target is a destroyed Unity object, removes it and logs a warning
      - if it is one-shot, removes it before the call
      - invokes the callback inside try/catch; an exception is logged and the loop carries on
  → at the end of the outermost publish, compacts the removed slots
```

`SubscribeWithRetained` calls the handler at once when a retained signal exists, and an exception from that call is caught and logged like one in a publish. `PublishRetained` stores the signal before it publishes, so a handler that reads `TryGetRetained` sees the new value.

### Lifecycle contract

Always subscribe in `OnEnable` and unsubscribe in `OnDisable` using `UnsubscribeAll(this)` for bulk cleanup. Use `Clear<TSignal>()` / `ClearAll()` only in test setup and teardown. Use retained signals only for state that is true now, never for events.

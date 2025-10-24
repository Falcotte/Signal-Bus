# Signal Bus

A lightweight, type-safe **Signal Bus** for Unity that provides a robust publish/subscribe event system with no dependencies and full runtime introspection via an **Inspector Window**.

This system enables a clean, decoupled architecture — where gameplay systems can communicate through typed signals without explicit object references.

---

## ✨ Features

- **Type-safe publish/subscribe** model (no strings or reflection hacks)
- **One-shot subscriptions** for single-use event handlers
- **Automatic cleanup** (`UnsubscribeAll(target)`) for MonoBehaviours
- **Per-signal subscriber tracking**
- **Runtime Inspector Window** for exploring and publishing signals live
- **Thread-safe and exception-isolated publishing**

---

## 🧩 Architecture Overview

### Core Components

#### `ISignal`

A marker interface that defines a signal type:

```
public interface ISignal { }
```

Any struct or class implementing `ISignal` can be published through the bus

#### `SignalBus`

The central hub for managing subscriptions and publishing events:

```
SignalBus.Subscribe<MySignal>(OnSignal);

SignalBus.Unsubscribe<MySignal>(OnSignal);

SignalBus.Publish(new MySignal());
```

It stores subscriber collections by type, provides helper methods for one-shot or bulk unsubscriptions, and ensures safety during iteration

Key methods:

* `Subscribe<TSignal>(Action<TSignal> callback)`

* `SubscribeOneShot<TSignal>(Action<TSignal> callback)`

* `Unsubscribe<TSignal>(Action<TSignal> callback)`

* `UnsubscribeAll(object target)`

* `Clear<TSignal>() / ClearAll()`

* `Publish<TSignal>(TSignal signal)`

#### `SubscriberCollection<TSignal>`

Holds all delegates for a specific signal type:

* Invokes all registered callbacks safely

* Removes null or dead targets automatically

Catches and logs exceptions per-callback without breaking dispatch

#### `SignalInspectorEditorWindow`

A powerful in-editor visualization tool for debugging signals at runtime.

Open via:

```Menu → Angry Koala → Signals → Signal Bus Inspector```

Features:

* Lists active and all discovered signal types

* Allows manual publishing of any signal type

* Shows all subscribers to each signal

* Supports auto-refresh, search, and expand/collapse by namespace

* Pings Unity objects tied to subscriptions

* Displays live reflection data of subscriber targets and methods

---

## 🧠 Usage Example

### 1. Define a Signal

```
using AngryKoala.Signals;

public struct PlayerDiedSignal : ISignal
{
    public int PlayerId;
    public string Cause;
}
```

### 2. Subscribe and Handle It

```
private void OnEnable()
{
    SignalBus.Subscribe<PlayerDiedSignal>(OnPlayerDied);
}

private void OnDisable()
{
    SignalBus.UnsubscribeAll(this);
}

private void OnPlayerDied(PlayerDiedSignal signal)
{
    Debug.Log($"Player {signal.PlayerId} died because of {signal.Cause}");
}
```

### 3. Publish the Signal

```
SignalBus.Publish(new PlayerDiedSignal
{
    PlayerId = 42,
    Cause = "Lava"
});
```

### 4. Inspect in Editor

While the game is running, open:

```Menu → Angry Koala → Signals → Signal Bus Inspector```

You can view all active signals, inspect subscribers, and even **publish signals manually** to test runtime reactions.

---

## 🧱 Extending the System

You can extend the bus by:

* Implementing custom editors that use reflection over `_subscribers`.

* Building higher-level abstractions like domain events, gameplay messages, or animation triggers.

* Hooking into the Inspector’s data through reflection for automation or tooling.

---

## 🪲 Debugging Tips

* **No callbacks?** Ensure your subscriber is subscribed *after* domain initialization (e.g., not during static constructors).

* **Signal not showing?** It only appears once at least one subscriber exists.

* **Reflection warnings in Inspector?** Likely caused by inaccessible internal fields or generic reflection edge cases.

* **Accidental double subscriptions?** Use `Unsubscribe` before `Subscribe` if you attach the same callback multiple times.
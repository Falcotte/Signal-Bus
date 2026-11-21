# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2.0.0] - Unreleased

### Added

- `SignalBus.SubscribeWithRetained<TSignal>(Action<TSignal>)`, which subscribes and calls back at once with the retained signal of that type, if there is one.
- `SignalBus.PublishRetained<TSignal>(TSignal)`, which publishes and keeps the signal as the current one of its type.
- `SignalBus.TryGetRetained<TSignal>(out TSignal)`, which reads the retained signal without subscribing.
- `SignalBus.ClearRetained<TSignal>()`, which forgets the retained signal of that type and keeps its subscribers.
- Edit Mode and Play Mode tests in `Tests/Editor` and `Tests/Runtime`.
- Assembly definitions `AngryKoala.Signals`, `AngryKoala.Signals.Editor`, `AngryKoala.Signals.Tests.Editor` and `AngryKoala.Signals.Tests.Runtime`.
- `package.json` for distribution as a UPM package from `Assets/Signals`.
- The Signal Bus Inspector shows whether each signal type has a retained signal.

### Changed

- `SignalBus` is a static class. The private singleton instance is gone.
- Subscribing the same handler twice is ignored with a warning. Before, the handler was called twice.
- A subscriber added during a publish is first called by the next publish. A nested publish counts as the next one.
- The Signal Bus Inspector reads the bus through internal APIs instead of reflecting into `SignalBus` private fields.

### Fixed

- Publishing no longer allocates. Each publish used to copy the subscriber list into a new array and box a struct signal.
- A subscriber removed during a publish is no longer called in that publish. The same holds for `Clear` and `ClearAll` called from a handler.
- A one-shot subscription can be removed with `Unsubscribe` and `UnsubscribeAll` before it is called. Before, it was wrapped in a hidden delegate that neither could find.
- A subscriber on a destroyed Unity object is no longer called. It is removed when a publish reaches it, and a warning names the handler.
- Subscribers and retained signals are cleared at the start of every Play session, so nothing survives between sessions when domain reload is disabled.

### Removed

- `ISubscriberCollection` and `SubscriberCollection<TSignal>` are internal. They are no longer part of the public API.
- `ISubscriberCollection.Publish(object)` and `ISubscriberCollection.Remove(Delegate)`.
- Reflection access to the `_instance` and `_subscribersByType` fields from the Signal Bus Inspector.

## Upgrading from 1.x

- `SignalBus` is static. Remove any code that used an instance of it.
- Code that sits in its own assembly definition must add a reference to `AngryKoala.Signals`.
- Do not use `ISubscriberCollection` or `SubscriberCollection<TSignal>`. They are internal now. Use `SignalBus` instead.
- If a handler was subscribed twice and relied on being called twice, subscribe it once.
- If a handler unsubscribes another subscriber during a publish and relied on that subscriber still running in that publish, move the unsubscribe after the publish.
- Stop reading `SignalBus` private fields through reflection. Use the public API.

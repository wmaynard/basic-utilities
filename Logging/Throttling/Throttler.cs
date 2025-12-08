using System.Collections.Concurrent;
using Maynard.Time;

namespace Maynard.Logging.Throttling;

public class Throttler<T>
{
    private readonly ConcurrentDictionary<int, LinkedList<long>> _map = new();
    private readonly ConcurrentDictionary<int, int> _counts = new();
    private readonly Lock _door = new();
    internal int Threshold { get; set; }
    internal int WindowInSeconds { get; set; }

    /// <summary>
    /// Allows for throttling objects in a customizable sliding window.  For memory efficiency, any non-numeric objects
    /// are detected via hash codes; collisions are therefore possible.
    /// </summary>
    /// <param name="value">The string to evaluate.</param>
    /// <param name="callback">A callback to handle the result.</param>
    /// <returns>True if the object should be ignored; false if the object should be actioned on.</returns>
    public Task Check(T value, Action<ThrottleEventArgs<T>> callback)
    {
        ThrottleEventArgs<T> args = new()
        {
            Value = value,
            Status = ThrottleStatus.NotSuppressed,
            ObjectsSuppressed = 0
        };
        if (value == null || Threshold == 0 || WindowInSeconds == 0)
        {
            callback.Invoke(args);
            return Task.CompletedTask;
        }
        
        int hash = value.GetHashCode();
        long now = TimestampMs.Now;
        long cutoff = now - WindowInSeconds * 1_000;

        LinkedList<long> timestamps = _map.GetOrAdd(hash, new LinkedList<long>());
        timestamps.AddLast(now);
        int total = timestamps.Count;
        
        LinkedListNode<long> node = timestamps.First;
        while (node != null && node.Value <= cutoff)
        {
            LinkedListNode<long> toRemove = node;
            node = node.Next;
            timestamps.Remove(toRemove);
        }

        int removed = total - timestamps.Count;
        int tracked = _counts.AddOrUpdate(hash, removed, (_, previous) => previous + removed);

        if (total <= Threshold)                         // The string is not throttled.
            args.Status = ThrottleStatus.NotSuppressed;
        else if (timestamps.Count > Threshold)          // We are still throttled from before.
            args.Status = ThrottleStatus.Suppressed;
        else                                            // The string was throttled previously, but no longer is.
        {
            args.Status = ThrottleStatus.PreviouslySuppressed;
            args.ObjectsSuppressed = tracked;
            _counts[hash] = 0;
        }
        callback.Invoke(args);
        return Task.CompletedTask;
    }

    public async Task Check(T value, Action<T> onValid) => await Check(value, callback: args =>
    {
        if (args.Status != ThrottleStatus.Suppressed)
            onValid?.Invoke(args.Value);
    });
    public async Task Check(T value, Action<T> onValid, Action<T> onSuppressed) => await Check(value, callback: args =>
    {
        if (args.Status == ThrottleStatus.Suppressed)
            onSuppressed?.Invoke(args.Value);
        else
            onValid?.Invoke(args.Value);
    });
}

public class ThrottleEventArgs<T>
{
    public ThrottleStatus Status { get; set; }
    public int ObjectsSuppressed { get; set; }
    public T Value { get; set; }
}
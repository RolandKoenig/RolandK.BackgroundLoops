using System;
using System.Threading;

namespace RolandK.BackgroundLoops;

/// <summary>
/// Synchronization object for threads within <see cref="BackgroundLoop"/> class.
/// </summary>
public class BackgroundLoopSynchronizationContext : SynchronizationContext
{
    private BackgroundLoop _owner;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundLoopSynchronizationContext"/> class.
    /// </summary>
    /// <param name="owner">The owner of this context.</param>
    internal BackgroundLoopSynchronizationContext(BackgroundLoop owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// When overridden in a derived class, dispatches an asynchronous message to a synchronization context.
    /// </summary>
    /// <param name="d">The <see cref="T:System.Threading.SendOrPostCallback"/> delegate to call.</param>
    /// <param name="state">The object passed to the delegate.</param>
    public override void Post(SendOrPostCallback d, object? state)
    {
        _owner.InvokeAsync(() => d(state));
    }

    /// <summary>
    /// When overridden in a derived class, dispatches a synchronous message to a synchronization context.
    /// </summary>
    /// <param name="d">The <see cref="T:System.Threading.SendOrPostCallback"/> delegate to call.</param>
    /// <param name="state">The object passed to the delegate.</param>
    public override void Send(SendOrPostCallback d, object? state)
    {
        throw new InvalidOperationException($"Synchronous messages not supported on {nameof(BackgroundLoop)}!");
    }
}
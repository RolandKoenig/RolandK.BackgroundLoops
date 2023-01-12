using System;

namespace RolandK.BackgroundLoops;

public class BackgroundLoopExceptionEventArgs : EventArgs
{
    /// <summary>
    /// Gets the occurred exception.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Gets current state of the thread.
    /// </summary>
    public BackgroundLoopState State { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundLoopExceptionEventArgs"/> class.
    /// </summary>
    /// <param name="threadState">The current state of the <see cref="BackgroundLoop"/>.</param>
    /// <param name="innerException">The inner exception.</param>
    public BackgroundLoopExceptionEventArgs(BackgroundLoopState threadState, Exception innerException)
    {
        this.Exception = innerException;
        this.State = threadState;
    }
}
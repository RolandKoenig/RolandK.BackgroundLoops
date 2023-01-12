namespace RolandK.BackgroundLoops;

/// <summary>
/// Enumeration containing all possible states of a BackgroundLoop object.
/// </summary>
public enum BackgroundLoopState
{
    /// <summary>
    /// There is no thread created at the moment.
    /// </summary>
    None,

    /// <summary>
    /// The thread is starting.
    /// </summary>
    Starting,

    /// <summary>
    /// The thread is running.
    /// </summary>
    Running,

    /// <summary>
    /// The thread is stopping.
    /// </summary>
    Stopping
}
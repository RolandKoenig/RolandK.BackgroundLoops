using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using RolandK.BackgroundLoops.Checking;
using RolandK.BackgroundLoops.Exceptions;

namespace RolandK.BackgroundLoops;

public class BackgroundLoop
{
    [field: ThreadStatic]
    public static BackgroundLoop? CurrentBackgroundLoop
    {
        get;
        private set;
    }

    // Members for thread runtime
    private volatile BackgroundLoopState _currentState;
    private volatile BackgroundLoopState _targetState;
    private Thread? _mainThread;
    private CultureInfo _culture;
    private CultureInfo _uiCulture;

    // Threading resources
    private BackgroundLoopSynchronizationContext _syncContext;
    private ConcurrentQueue<Action> _taskQueue;
    private SemaphoreSlim _mainLoopSynchronizeObject;
    private SemaphoreSlim? _threadStopSynchronizeObject;

    /// <summary>
    /// Gets the current SynchronizationContext object.
    /// </summary>
    public SynchronizationContext SyncContext => _syncContext;

    /// <summary>
    /// Gets the current state of the background loop.
    /// </summary>
    public BackgroundLoopState CurrentState => _currentState;

    public bool IsStartingOrRunning =>
        (_currentState == BackgroundLoopState.Starting) ||
        (_currentState == BackgroundLoopState.Running);

    /// <summary>
    /// Gets the name of this thread.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the thread's heartbeat.
    /// </summary>
    protected TimeSpan HeartBeat { get; set; }

    /// <summary>
    /// Called when the thread ist starting.
    /// </summary>
    public event EventHandler? Starting;

    /// <summary>
    /// Called when the thread is stopping.
    /// </summary>
    public event EventHandler? Stopping;

    /// <summary>
    /// Called when an unhandled exception occurred.
    /// </summary>
    public event EventHandler<BackgroundLoopExceptionEventArgs>? ThreadException;

    /// <summary>
    /// Called on each heartbeat.
    /// </summary>
    public event EventHandler<BackgroundLoopTickEventArgs>? Tick;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundLoop"/> class.
    /// </summary>
    /// <param name="name">The name of the generated thread.</param>
    /// <param name="heartBeatMS">The initial heartbeat of the BackgroundLoop in milliseconds.</param>
    public BackgroundLoop(string name = "", int heartBeatMS = 500)
        : this(name, TimeSpan.FromMilliseconds(heartBeatMS))
    {

    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundLoop"/> class.
    /// </summary>
    /// <param name="name">The name of the generated thread.</param>
    /// <param name="heartBeat">The initial heartbeat of the BackgroundLoop.</param>
    public BackgroundLoop(string name, TimeSpan heartBeat)
    {
        _taskQueue = new ConcurrentQueue<Action>();
        _mainLoopSynchronizeObject = new SemaphoreSlim(1);

        this.Name = name;
        this.HeartBeat = heartBeat;

        _syncContext = new BackgroundLoopSynchronizationContext(this);

        _culture = Thread.CurrentThread.CurrentCulture;
        _uiCulture = Thread.CurrentThread.CurrentUICulture;
    }

    /// <summary>
    /// Starts the thread.
    /// </summary>
    public void Start()
    {
        if (_currentState != BackgroundLoopState.None) { throw new InvalidOperationException("Unable to start thread: Illegal state: " + _currentState + "!"); }

        // Ensure that one single pass of the main loop is made at once
        _mainLoopSynchronizeObject.Release();

        // Create stop semaphore
        if (_threadStopSynchronizeObject != null)
        {
            _threadStopSynchronizeObject.Dispose();
            _threadStopSynchronizeObject = null;
        }

        _threadStopSynchronizeObject = new SemaphoreSlim(0);

        // Go into starting state
        _currentState = BackgroundLoopState.Starting;
        _targetState = BackgroundLoopState.Running;

        _mainThread = new Thread(this.BackgroundLoopMainMethod)
        {
            IsBackground = true,
            Name = this.Name
        };

        _mainThread.Start();
    }

    /// <summary>
    /// Waits until this BackgroundLoop has stopped.
    /// </summary>
    public Task WaitUntilStoppedAsync()
    {
        switch (_currentState)
        {
            case BackgroundLoopState.None:
            case BackgroundLoopState.Stopping:
                return Task.Delay(100);

            case BackgroundLoopState.Running:
            case BackgroundLoopState.Starting:
                var taskSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                this.Stopping += (_, _) =>
                {
                    taskSource.TrySetResult(null);
                };
                return taskSource.Task;

            default:
                throw new BackgroundLoopException($"Unhandled {nameof(BackgroundLoopState)} {_currentState}!");
        }
    }

    /// <summary>
    /// Starts this thread. The returned task is completed when starting is finished.
    /// </summary>
    public Task StartAsync()
    {
        this.Start();

        return this.InvokeAsync(() => { });
    }

    /// <summary>
    /// Stops this instance.
    /// </summary>
    public void Stop()
    {
        if (_currentState != BackgroundLoopState.Running) { throw new InvalidOperationException($"Unable to stop thread: Illegal state: {_currentState}!"); }
        _targetState = BackgroundLoopState.Stopping;

        // Trigger next update
        this.Trigger();
    }

    /// <summary>
    /// Stops the asynchronous.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        this.Stop();

        if (_threadStopSynchronizeObject != null)
        {
            await _threadStopSynchronizeObject.WaitAsync(cancellationToken);

            _threadStopSynchronizeObject.Dispose();
            _threadStopSynchronizeObject = null;
        }
    }

    /// <summary>
    /// Triggers a new heartbeat.
    /// </summary>
    public virtual void Trigger()
    {
        var synchronizationObject = _mainLoopSynchronizeObject;
        synchronizationObject.Release();
    }

    /// <summary>
    /// Invokes the given delegate within the thread of this object.
    /// </summary>
    /// <param name="actionToInvoke">The delegate to invoke.</param>
    public Task InvokeAsync(Action actionToInvoke)
    {
        actionToInvoke.EnsureNotNull(nameof(actionToInvoke));

        // Enqueue the given action
        var taskCompletionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        _taskQueue.Enqueue(() =>
        {
            try
            {
                actionToInvoke();
                taskCompletionSource.SetResult(null);
            }
            catch (Exception ex)
            {
                taskCompletionSource.SetException(ex);
            }
        });

        Task result = taskCompletionSource.Task;

        // Triggers the main loop
        this.Trigger();

        // Returns the result
        return result;
    }

    /// <summary>
    /// Invokes the given delegate within the thread of this object.
    /// This method works in a fire-and-forget way. We don't wait for the action to finish.
    /// </summary>
    /// <param name="actionToInvoke">The delegate to invoke.</param>
    public void BeginInvoke(Action actionToInvoke)
    {
        actionToInvoke.EnsureNotNull(nameof(actionToInvoke));

        _taskQueue.Enqueue(actionToInvoke);
        
        // Triggers the main loop
        this.Trigger();
    }

    /// <summary>
    /// Thread is starting.
    /// </summary>
    protected virtual void OnStarting(EventArgs eArgs)
    {
        this.Starting?.Invoke(this, eArgs);
    }

    /// <summary>
    /// Called on each tick.
    /// </summary>
    protected virtual void OnTick(BackgroundLoopTickEventArgs eArgs)
    {
        this.Tick?.Invoke(this, eArgs);
    }

    /// <summary>
    /// Called on each occurred exception.
    /// </summary>
    protected virtual void OnThreadException(BackgroundLoopExceptionEventArgs eArgs)
    {
        this.ThreadException?.Invoke(this, eArgs);
    }

    /// <summary>
    /// Thread is stopping.
    /// </summary>
    protected virtual void OnStopping(EventArgs eArgs)
    {
        this.Stopping?.Invoke(this, eArgs);
    }

    /// <summary>
    /// The thread's main method.
    /// </summary>
    private void BackgroundLoopMainMethod()
    {
        if (_mainThread == null) { return; }
        if (_mainThread != Thread.CurrentThread) { return; }

        try
        {
            _mainThread.CurrentCulture = _culture;
            _mainThread.CurrentUICulture = _uiCulture;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            // Set synchronization context for this thread
            SynchronizationContext.SetSynchronizationContext(_syncContext);

            // Notify start process
            try
            {
                CurrentBackgroundLoop = this;
                this.OnStarting(EventArgs.Empty);
            }
            catch (Exception ex)
            {
                this.OnThreadException(new BackgroundLoopExceptionEventArgs(_currentState, ex));
                _currentState = BackgroundLoopState.None;
                _targetState = BackgroundLoopState.None;
                CurrentBackgroundLoop = null;
                return;
            }

            // Run main-thread
            if (_currentState != BackgroundLoopState.None)
            {
                _currentState = _targetState;
                while (_currentState == BackgroundLoopState.Running)
                {
                    try
                    {
                        // Wait for next action to perform
                        _mainLoopSynchronizeObject.Wait(this.HeartBeat);
                        
                        _currentState = _targetState;
                        if (_currentState != BackgroundLoopState.Running) { break; }

                        // Measure current time
                        stopWatch.Stop();
                        var elapsedTicks = stopWatch.Elapsed.Ticks;
                        stopWatch.Reset();
                        stopWatch.Start();

                        // Get current task queue
                        var localTaskQueue = new List<Action>();
                        while (_taskQueue.TryDequeue(out var dummyAction))
                        {
                            localTaskQueue.Add(dummyAction);
                        }

                        // Execute all tasks
                        foreach (var actTask in localTaskQueue)
                        {
                            try
                            {
                                actTask();
                            }
                            catch (Exception ex)
                            {
                                this.OnThreadException(new BackgroundLoopExceptionEventArgs(_currentState, ex));
                            }
                        }

                        // Performs a tick
                        this.OnTick(new BackgroundLoopTickEventArgs(elapsedTicks));
                    }
                    catch (Exception ex)
                    {
                        this.OnThreadException(new BackgroundLoopExceptionEventArgs(_currentState, ex));
                    }

                    _currentState = _targetState;
                }

                // Notify stop process
                try
                {
                    this.OnStopping(EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    this.OnThreadException(new BackgroundLoopExceptionEventArgs(_currentState, ex));
                }
                CurrentBackgroundLoop = null;
            }

            // Reset state to none
            _currentState = BackgroundLoopState.None;
            _targetState = BackgroundLoopState.None;

            stopWatch.Stop();
            stopWatch = null;
        }
        catch (Exception ex)
        {
            this.OnThreadException(new BackgroundLoopExceptionEventArgs(_currentState, ex));
            _currentState = BackgroundLoopState.None;
            _targetState = BackgroundLoopState.None;
        }

        // Notify thread stop event
        try { _threadStopSynchronizeObject?.Release(); }
        catch (Exception)
        {
            // ignored
        }
    }
}
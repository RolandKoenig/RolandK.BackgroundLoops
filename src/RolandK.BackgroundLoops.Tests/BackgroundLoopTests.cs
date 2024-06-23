namespace RolandK.BackgroundLoops.Tests;

public class BackgroundLoopTests
{
    [Fact]
    public async Task StartAndStop()
    {
        // Arrange
        var firstTickTaskSource = new TaskCompletionSource<object?>();

        var startingCalled = false;
        var tickCalled = false;
        var stoppingCalled = false;

        var backgroundLoop = new BackgroundLoop();
        backgroundLoop.Starting += (_, _) => { startingCalled = true; };
        backgroundLoop.Tick += (_, _) =>
        {
            tickCalled = true;
            firstTickTaskSource.TrySetResult(null);
        };
        backgroundLoop.Stopping += (_, _) => { stoppingCalled = true; };

        // Act
        await backgroundLoop.StartAsync();
        await firstTickTaskSource.Task;

        var cancelTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await backgroundLoop.StopAsync(cancelTokenSource.Token);

        // Assert
        Assert.True(startingCalled, nameof(startingCalled));
        Assert.True(tickCalled, nameof(tickCalled));
        Assert.True(stoppingCalled, nameof(stoppingCalled));
    }

    [Fact]
    public async Task StartAndStop_StateIsSetCorrectly()
    {
        // Arrange
        var firstTickTaskSource = new TaskCompletionSource<object?>();

        var stateOnStarting = BackgroundLoopState.None;
        var stateOnTick = BackgroundLoopState.None;
        var stateOnStopping = BackgroundLoopState.None;

        var backgroundLoop = new BackgroundLoop();
        backgroundLoop.Starting += (_, _) => { stateOnStarting = backgroundLoop.CurrentState; };
        backgroundLoop.Tick += (_, _) =>
        {
            stateOnTick = backgroundLoop.CurrentState;
            firstTickTaskSource.TrySetResult(null);
        };
        backgroundLoop.Stopping += (_, _) => { stateOnStopping = backgroundLoop.CurrentState; };

        // Act
        await backgroundLoop.StartAsync();
        await firstTickTaskSource.Task;
        
        var cancelTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await backgroundLoop.StopAsync(cancelTokenSource.Token);

        // Assert
        Assert.Equal(BackgroundLoopState.Starting, stateOnStarting);
        Assert.Equal(BackgroundLoopState.Running, stateOnTick);
        Assert.Equal(BackgroundLoopState.Stopping, stateOnStopping);
    }
    
    [Fact]
    public async Task TicksMoreTimes()
    {
        // Arrange
        var firstTickTaskSource = new TaskCompletionSource<object?>();

        var tickCount = 0;

        var backgroundLoop = new BackgroundLoop(string.Empty, 10);
        backgroundLoop.Tick += (_, _) =>
        {
            tickCount++;
            if (tickCount == 5)
            {
                firstTickTaskSource.TrySetResult(null);
            }
        };

        // Act
        await backgroundLoop.StartAsync();
        await firstTickTaskSource.Task;
        
        var cancelTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await backgroundLoop.StopAsync(cancelTokenSource.Token);

        // Assert
        Assert.True(tickCount >= 5, nameof(tickCount));
    }

    [Fact]
    public async Task IsSynchronizationContextSet_OnStart()
    {
        // Arrange
        var isSyncContextSet = false;

        var backgroundLoop = new BackgroundLoop();
        backgroundLoop.Starting += (_, _) =>
        {
            isSyncContextSet =
                SynchronizationContext.Current is BackgroundLoopSynchronizationContext;
        };

        // Act
        await backgroundLoop.StartAsync();
        
        var cancelTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await backgroundLoop.StopAsync(cancelTokenSource.Token);

        // Assert
        Assert.True(isSyncContextSet, nameof(isSyncContextSet));
    }
    
    [Fact]
    public async Task IsSynchronizationContextSet_OnTick()
    {
        // Arrange
        var firstTickTaskSource = new TaskCompletionSource<object?>();

        var isSyncContextSet = false;

        var backgroundLoop = new BackgroundLoop();
        backgroundLoop.Tick += (_, _) =>
        {
            isSyncContextSet =
                SynchronizationContext.Current is BackgroundLoopSynchronizationContext;
            firstTickTaskSource.TrySetResult(null);
        };

        // Act
        await backgroundLoop.StartAsync();
        await firstTickTaskSource.Task;
        
        var cancelTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await backgroundLoop.StopAsync(cancelTokenSource.Token);

        // Assert
        Assert.True(isSyncContextSet, nameof(isSyncContextSet));
    }
    
    [Fact]
    public async Task IsSynchronizationContextSet_OnStop()
    {
        // Arrange
        var isSyncContextSet = false;

        var backgroundLoop = new BackgroundLoop();
        backgroundLoop.Stopping += (_, _) =>
        {
            isSyncContextSet =
                SynchronizationContext.Current is BackgroundLoopSynchronizationContext;
        };

        // Act
        await backgroundLoop.StartAsync();
        
        var cancelTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await backgroundLoop.StopAsync(cancelTokenSource.Token);

        // Assert
        Assert.True(isSyncContextSet, nameof(isSyncContextSet));
    }
    
    [Fact]
    public async Task IsSynchronizationContextSet_OnInvokeAsync()
    {
        // Arrange
        var isSyncContextSet = false;

        var backgroundLoop = new BackgroundLoop();
        var invokeTask = backgroundLoop.InvokeAsync(() =>
        {
            isSyncContextSet =
                SynchronizationContext.Current is BackgroundLoopSynchronizationContext;
        });
        
        // Act
        await backgroundLoop.StartAsync();
        await invokeTask;
        
        var cancelTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await backgroundLoop.StopAsync(cancelTokenSource.Token);

        // Assert
        Assert.True(isSyncContextSet, nameof(isSyncContextSet));
    }
    
    [Fact]
    public async Task IsSynchronizationContextSet_OnBeginInvoke()
    {
        // Arrange
        var firstTickTaskSource = new TaskCompletionSource<object?>();

        var isSyncContextSet = false;

        var backgroundLoop = new BackgroundLoop();
        backgroundLoop.BeginInvoke(() =>
        {
            isSyncContextSet =
                SynchronizationContext.Current is BackgroundLoopSynchronizationContext;
            firstTickTaskSource.TrySetResult(null);
        });

        // Act
        await backgroundLoop.StartAsync();
        await firstTickTaskSource.Task;
        
        var cancelTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await backgroundLoop.StopAsync(cancelTokenSource.Token);

        // Assert
        Assert.True(isSyncContextSet, nameof(isSyncContextSet));
    }

    [Fact]
    public async Task IsThreadNameSet()
    {
        // Arrange
        var firstTickTaskSource = new TaskCompletionSource<object?>();

        var isThreadNameSet = false;

        var backgroundLoop = new BackgroundLoop("TestThreadName", 10);
        backgroundLoop.Tick += (_, _) =>
        {
            isThreadNameSet = Thread.CurrentThread.Name == "TestThreadName";
            firstTickTaskSource.TrySetResult(null);
        };

        // Acct
        await backgroundLoop.StartAsync();
        await firstTickTaskSource.Task;
        
        var cancelTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await backgroundLoop.StopAsync(cancelTokenSource.Token);

        // Assert
        Assert.True(isThreadNameSet, nameof(isThreadNameSet));
    }

    [Fact]
    public async Task InvokeMethod_AfterStart()
    {
        // Arrange
        var firstTickTaskSource = new TaskCompletionSource<object?>();

        var firstTickPassed = false;

        var backgroundLoop = new BackgroundLoop();
        backgroundLoop.Tick += (_, _) =>
        {
            if (!firstTickPassed)
            {
                firstTickPassed = true;
                firstTickTaskSource.TrySetResult(null);
            }
        };

        // Act
        await backgroundLoop.StartAsync();
        await firstTickTaskSource.Task;

        var methodInvoked = false;
        await backgroundLoop.InvokeAsync(() => methodInvoked = true);

        var cancelTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await backgroundLoop.StopAsync(cancelTokenSource.Token);

        // Assert
        Assert.True(methodInvoked, nameof(methodInvoked));
    }

    [Fact]
    public async Task InvokeMethod_BeforeStart()
    {
        // Arrange
        var backgroundLoop = new BackgroundLoop();

        // Act
        var methodInvoked = false;
        var invokeTask = backgroundLoop.InvokeAsync(() => methodInvoked = true);
        await backgroundLoop.StartAsync();
        await invokeTask;
        
        var cancelTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await backgroundLoop.StopAsync(cancelTokenSource.Token);

        // Assert
        Assert.True(methodInvoked, nameof(methodInvoked));
    }
}
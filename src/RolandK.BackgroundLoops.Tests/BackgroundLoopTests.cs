namespace RolandK.BackgroundLoops.Tests;

public class BackgroundLoopTests
{
    [Fact]
    public async Task StartAndStop()
    {
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

        await backgroundLoop.StartAsync();
        await firstTickTaskSource.Task;
        await backgroundLoop.StopAsync(5000);

        Assert.True(startingCalled, nameof(startingCalled));
        Assert.True(tickCalled, nameof(tickCalled));
        Assert.True(stoppingCalled, nameof(stoppingCalled));
    }

    [Fact]
    public async Task TicksMoreTimes()
    {
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

        await backgroundLoop.StartAsync();
        await firstTickTaskSource.Task;
        await backgroundLoop.StopAsync(5000);

        Assert.True(tickCount >= 5, nameof(tickCount));
    }

    [Fact]
    public async Task IsSynchronizationContextSet()
    {
        var firstTickTaskSource = new TaskCompletionSource<object?>();

        var isSyncContextSet = false;

        var backgroundLoop = new BackgroundLoop(string.Empty, 500);
        backgroundLoop.Tick += (_, _) =>
        {
            isSyncContextSet =
                SynchronizationContext.Current is BackgroundLoopSynchronizationContext;
            firstTickTaskSource.TrySetResult(null);
        };

        await backgroundLoop.StartAsync();
        await firstTickTaskSource.Task;
        await backgroundLoop.StopAsync(5000);

        Assert.True(isSyncContextSet, nameof(isSyncContextSet));
    }

    [Fact]
    public async Task IsThreadNameSet()
    {
        var firstTickTaskSource = new TaskCompletionSource<object?>();

        var isThreadNameSet = false;

        var backgroundLoop = new BackgroundLoop("TestThreadName", 10);
        backgroundLoop.Tick += (_, _) =>
        {
            isThreadNameSet = Thread.CurrentThread.Name == "TestThreadName";
            firstTickTaskSource.TrySetResult(null);
        };

        await backgroundLoop.StartAsync();
        await firstTickTaskSource.Task;
        await backgroundLoop.StopAsync(5000);

        Assert.True(isThreadNameSet, nameof(isThreadNameSet));
    }

    [Fact]
    public async Task InvokeMethod_AfterStart()
    {
        var firstTickTaskSource = new TaskCompletionSource<object?>();

        var firstTickPassed = false;

        var backgroundLoop = new BackgroundLoop(string.Empty, 500);
        backgroundLoop.Tick += (_, _) =>
        {
            if (!firstTickPassed)
            {
                firstTickPassed = true;
                firstTickTaskSource.TrySetResult(null);
            }
        };

        await backgroundLoop.StartAsync();
        await firstTickTaskSource.Task;

        var methodInvoked = false;
        await backgroundLoop.InvokeAsync(() => methodInvoked = true);

        await backgroundLoop.StopAsync(5000);

        Assert.True(methodInvoked, nameof(methodInvoked));
    }

    [Fact]
    public async Task InvokeMethod_BeforeStart()
    {
        var backgroundLoop = new BackgroundLoop(string.Empty, 500);

        var methodInvoked = false;
        var invokeTask = backgroundLoop.InvokeAsync(() => methodInvoked = true);
        await backgroundLoop.StartAsync();
        await invokeTask;
        await backgroundLoop.StopAsync(5000);

        Assert.True(methodInvoked, nameof(methodInvoked));
    }
}
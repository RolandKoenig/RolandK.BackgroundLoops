namespace RolandK.BackgroundLoops;

public readonly struct BackgroundLoopTickEventArgs
{
    public readonly long ElapsedTicks;

    public BackgroundLoopTickEventArgs(long elapsedTicks)
    {
        this.ElapsedTicks = elapsedTicks;
    }
}
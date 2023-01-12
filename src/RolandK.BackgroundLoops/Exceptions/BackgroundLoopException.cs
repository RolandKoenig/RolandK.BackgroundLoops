using System;

namespace RolandK.BackgroundLoops.Exceptions;

public class BackgroundLoopException : Exception
{
    public BackgroundLoopException(string message)
        : base(message)
    {
        
    }

    public BackgroundLoopException(string message, Exception innerException)
        : base(message, innerException)
    {
        
    }
}
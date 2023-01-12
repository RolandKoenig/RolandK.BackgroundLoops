using System;

namespace RolandK.BackgroundLoops.Exceptions;

public class BackgroundLoopCheckException : Exception
{
    public BackgroundLoopCheckException(string message)
        : base(message)
    {
        
    }

    public BackgroundLoopCheckException(string message, Exception innerException)
        : base(message, innerException)
    {
        
    }
}
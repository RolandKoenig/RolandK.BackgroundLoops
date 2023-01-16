# RolandK.BackgroundLoops <img src="assets/Logo_128.png" width="32" />
A .NET Standard library which provides a base class for background loops. Those loops work on a dedicated thread and provide a SynchronizationContext

## Features
 - BackgroundLoop class with Start, Stop and Tick events
 - SynchronizationContext on the BackgroundLoop

## Build
[![Continuous integration](https://github.com/RolandKoenig/RolandK.BackgroundLoops/actions/workflows/continuous-integration.yml/badge.svg)](https://github.com/RolandKoenig/RolandK.BackgroundLoops/actions/workflows/continuous-integration.yml)

## Nuget
| Package                 | Link |
|-------------------------|------|
| RolandK.BackgroundLoops |      |

## Samples
The following snipped creates a BackgroundLoop named TestThread and
a heartbeat time of 500 milliseconds. Tick is call at least
each time the heartbeat time passes after processing the previous tick.

```csharp
var backgroundLoop = new BackgroundLoop(
    "TestThread", 
    TimeSpan.FromMilliseconds(500));
backgroundLoop.Tick += (_, _) =>
{
    Console.WriteLine("BackgroundLoop ticked");
};

await backgroundLoop.StartAsync();
```
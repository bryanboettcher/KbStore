using NUnit.Framework;

// Disable parallel test fixture initialization to prevent deadlocks
// when multiple MassTransit test harnesses start simultaneously
[assembly: Parallelizable(ParallelScope.None)]

using NUnit.Framework;

// Enable parallel test execution at the fixture level
// Each fixture has its own harness lifecycle (OneTimeSetUp/OneTimeTearDown)
// Tests within the same fixture run sequentially (safe for shared harness)
// Different fixtures can run in parallel (each has isolated harness)
[assembly: Parallelizable(ParallelScope.Fixtures)]

// Use 4 workers for parallel execution (adjust based on available cores)
[assembly: LevelOfParallelism(4)]

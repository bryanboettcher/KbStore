using KbStore.Abstractions;
using NUnit.Framework;

namespace KbStore.Catalog.Tests;

[SetUpFixture]
public class GlobalTestSetup
{
    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        // Use timestamp-based macro namespace for test isolation
        // Each test run gets unique namespace → unique GUIDs → no saga collisions
        var testMacro = (int)(DateTime.UtcNow.Ticks & 0xFFFFFFFF);
        DeterministicGuid.SetMacroNamespace(testMacro);

        TestContext.Progress.WriteLine($"Test MacroNamespace: 0x{testMacro:X8}");
    }

    [OneTimeTearDown]
    public void RunAfterAllTests()
    {
        DeterministicGuid.ResetToProduction();
    }
}

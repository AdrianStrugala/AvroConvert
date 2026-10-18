using NUnit.Framework;

// A new test class instance per test (as in xUnit), so tests can run in parallel without sharing fields.
[assembly: FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[assembly: Parallelizable(ParallelScope.All)]

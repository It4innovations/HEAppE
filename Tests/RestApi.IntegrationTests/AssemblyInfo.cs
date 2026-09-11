using Xunit;

// Disable parallel test execution across integration test classes to avoid
// race conditions on the shared SQL database and static service provider state.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

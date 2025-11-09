using Xunit;

// Disable xUnit parallelization for the whole test assembly. This prevents concurrent
// execution of TestApp integration tests (which initialize global/native GPU resources)
// with other tests. If you prefer to keep parallel execution for most tests, we can
// instead annotate only the TestApp tests and mark other tests as part of the same
// collection, but that requires touching many files.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

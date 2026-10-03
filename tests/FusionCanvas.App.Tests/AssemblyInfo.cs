using Xunit;

// Avalonia headless tests share a dispatcher, so test collections must not execute
// concurrently within this assembly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

using Xunit;

namespace Unityctl.Cli.Tests;

/// <summary>
/// Ensures test classes that capture Console.Out run sequentially to avoid race conditions.
/// </summary>
[CollectionDefinition("ConsoleOutput")]
public sealed class ConsoleOutputCollection : ICollectionFixture<object>
{
}

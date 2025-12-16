using RAG.IntegrationTests.Infrastructure;
using Xunit;

namespace RAG.IntegrationTests;

/// <summary>
/// Collection definition for tests that require no authentication.
/// DisableParallelization ensures these tests don't run alongside auth-enabled tests,
/// preventing environment variable pollution.
/// </summary>
[CollectionDefinition("NoAuth Tests", DisableParallelization = true)]
public class NoAuthTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}

/// <summary>
/// Collection definition for tests that require API key authentication.
/// DisableParallelization ensures these tests don't run alongside non-auth tests,
/// preventing environment variable pollution.
/// </summary>
[CollectionDefinition("Auth Tests", DisableParallelization = true)]
public class AuthTestCollection : ICollectionFixture<AuthEnabledWebApplicationFactory>
{
}

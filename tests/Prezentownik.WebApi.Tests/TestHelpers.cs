using Microsoft.Extensions.Time.Testing;

namespace Prezentownik.WebApi.Tests;

internal static class TestHelpers
{
    public static FakeTimeProvider CreateFakeTimeProvider(DateTimeOffset? initialTime = null)
    {
        return new FakeTimeProvider(initialTime ?? DateTimeOffset.Parse("2026-08-31T09:24:01.2345Z"));
    }
}

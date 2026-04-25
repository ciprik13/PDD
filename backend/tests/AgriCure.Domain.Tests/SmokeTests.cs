namespace AgriCure.Domain.Tests;

public class SmokeTests
{
    [Fact]
    public void DomainAssembly_loads()
    {
        // Domain has no types yet; this test exists so `dotnet test` proves the project wiring.
        var assembly = typeof(SmokeTests).Assembly;
        assembly.Should().NotBeNull();
    }
}

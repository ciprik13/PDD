namespace AgriCure.Application.Tests;

public class SmokeTests
{
    [Fact]
    public void ApplicationAssembly_loads()
    {
        // Application has no types yet; this test exists so `dotnet test` proves the project wiring.
        var assembly = typeof(SmokeTests).Assembly;
        assembly.Should().NotBeNull();
    }
}

namespace Spectre.Console.Tests.Unit;

public sealed class ProfileTests
{
    [Fact]
    public void AddEnricher_Null_Throws()
    {
        // Given
        using var console = new TestConsole();
        var profile = console.Profile;

        // When / Then
        Assert.Throws<ArgumentNullException>(() => profile.AddEnricher(null!));
    }

    [Fact]
    public void AddEnricher_AddsToEnrichers()
    {
        // Given
        using var console = new TestConsole();
        var profile = console.Profile;

        // When
        profile.AddEnricher("TestEnricher");

        // Then
        profile.Enrichers.Should().Contain("TestEnricher");
    }

    [Fact]
    public void AddEnricher_Multiple_AllPresent()
    {
        // Given
        using var console = new TestConsole();
        var profile = console.Profile;

        // When
        profile.AddEnricher("First");
        profile.AddEnricher("Second");

        // Then
        profile.Enrichers.Should().Contain("First");
        profile.Enrichers.Should().Contain("Second");
    }
}

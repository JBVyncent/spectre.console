namespace Spectre.Console.Tests.Unit;

public sealed class ProgressTaskTests
{
    [Fact]
    public void StartTime_Is_Set_From_TimeProvider_When_AutoStart_Is_True()
    {
        // Given
        var timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
        var expectedStartTime = timeProvider.GetLocalNow().LocalDateTime;

        // When
        var task = new ProgressTask(1, "Foo", 100, autoStart: true, timeProvider);

        // Then
        task.StartTime.Should().Be(expectedStartTime);
    }

    [Fact]
    public void StartTime_Is_Null_When_AutoStart_Is_False()
    {
        // Given
        var timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));

        // When
        var task = new ProgressTask(1, "Foo", 100, autoStart: false, timeProvider);

        // Then
        task.StartTime.Should().BeNull();
    }

    [Fact]
    public void StartTask_Uses_TimeProvider()
    {
        // Given
        var timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
        var task = new ProgressTask(1, "Foo", 100, autoStart: false, timeProvider);
        var expectedStartTime = timeProvider.GetLocalNow().LocalDateTime;

        // When
        task.StartTask();

        // Then
        task.StartTime.Should().Be(expectedStartTime);
    }

    [Fact]
    public void StopTask_Uses_TimeProvider()
    {
        // Given
        var timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
        var task = new ProgressTask(1, "Foo", 100, autoStart: true, timeProvider);
        timeProvider.Advance(TimeSpan.FromSeconds(30));
        var expectedStopTime = timeProvider.GetLocalNow().LocalDateTime;

        // When
        task.StopTask();

        // Then
        task.StopTime.Should().Be(expectedStopTime);
    }

    [Fact]
    public void ElapsedTime_Uses_TimeProvider()
    {
        // Given
        var timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
        var task = new ProgressTask(1, "Foo", 100, autoStart: true, timeProvider);

        // When
        timeProvider.Advance(TimeSpan.FromSeconds(42));

        // Then
        task.ElapsedTime.Should().Be(TimeSpan.FromSeconds(42));
    }

    [Fact]
    public void ElapsedTime_Is_Fixed_After_StopTask()
    {
        // Given
        var timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
        var task = new ProgressTask(1, "Foo", 100, autoStart: true, timeProvider);
        timeProvider.Advance(TimeSpan.FromSeconds(10));
        task.StopTask();

        // When
        timeProvider.Advance(TimeSpan.FromSeconds(100));

        // Then
        task.ElapsedTime.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Speed_Uses_TimeProvider()
    {
        // Given
        var timeProvider = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
        var task = new ProgressTask(1, "Foo", 100, autoStart: true, timeProvider);

        // When
        task.Increment(25);
        timeProvider.Advance(TimeSpan.FromSeconds(5));
        task.Increment(25);

        // Then
        task.Speed.Should().NotBeNull();
        task.Speed!.Value.Should().BeApproximately(10.0, 0.001);
    }
}
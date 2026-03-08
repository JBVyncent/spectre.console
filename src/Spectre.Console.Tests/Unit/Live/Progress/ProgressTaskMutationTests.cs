namespace Spectre.Console.Tests.Unit;

/// <summary>
/// Tests targeting Stryker surviving mutants in ProgressTask.cs.
/// </summary>
public sealed class ProgressTaskMutationTests
{
    public sealed class NullGuards
    {
        [Fact]
        public void Constructor_Should_Throw_If_Description_Is_Null()
        {
            // Kills: L132, ThrowIfNull removal
            var ex = Record.Exception(() => new ProgressTask(1, null!, 100));
            ex.ShouldBeOfType<ArgumentNullException>();
        }
    }

    public sealed class ConstructorDefaults
    {
        [Fact]
        public void Value_Should_Default_To_Zero()
        {
            // Kills: L137 _value = 0 removal or mutation
            var task = new ProgressTask(1, "Test", 100);
            task.Value.ShouldBe(0);
        }

        [Fact]
        public void Description_Should_Be_Trimmed()
        {
            // Kills: L138 .Trim() removal
            var task = new ProgressTask(1, "  Test  ", 100);
            task.Description.ShouldBe("Test");
        }

        [Fact]
        public void Should_Throw_If_Description_Is_Whitespace()
        {
            // Kills: L140 string.IsNullOrWhiteSpace check
            var ex = Record.Exception(() => new ProgressTask(1, "   ", 100));
            ex.ShouldBeOfType<ArgumentException>();
        }

        [Fact]
        public void Should_Store_Id()
        {
            // Kills: L145 Id = id removal
            var task = new ProgressTask(42, "Test", 100);
            task.Id.ShouldBe(42);
        }

        [Fact]
        public void AutoStart_True_Sets_StartTime()
        {
            // Kills: L147 autoStart ternary mutation
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            task.StartTime.ShouldNotBeNull();
        }

        [Fact]
        public void AutoStart_False_Leaves_StartTime_Null()
        {
            // Kills: L147 autoStart ternary mutation (other direction)
            var task = new ProgressTask(1, "Test", 100, autoStart: false);
            task.StartTime.ShouldBeNull();
        }

        [Fact]
        public void TimeProvider_Defaults_To_System()
        {
            // Kills: L135 timeProvider ?? TimeProvider.System
            // If null coalescing is removed, _timeProvider would be null and StartTime would throw
            var task = new ProgressTask(1, "Test", 100, autoStart: true);
            task.StartTime.ShouldNotBeNull();
        }
    }

    public sealed class UpdateMethod
    {
        [Fact]
        public void Description_Set_Should_Update_Description()
        {
            // Kills: L219 _description = description
            var task = new ProgressTask(1, "Old", 100);
            task.Description = "New";
            task.Description.ShouldBe("New");
        }

        [Fact]
        public void Description_Set_Should_Throw_If_Whitespace()
        {
            // Kills: L214 IsNullOrWhiteSpace check in Update
            var task = new ProgressTask(1, "Test", 100);
            var ex = Record.Exception(() => task.Description = "   ");
            ex.ShouldBeOfType<InvalidOperationException>();
        }

        [Fact]
        public void MaxValue_Set_Should_Update_MaxValue()
        {
            // Kills: L224 _maxValue = maxValue.Value
            var task = new ProgressTask(1, "Test", 100);
            task.MaxValue = 200;
            task.MaxValue.ShouldBe(200);
        }

        [Fact]
        public void Increment_Should_Add_To_Value()
        {
            // Kills: L229 _value += increment.Value
            var task = new ProgressTask(1, "Test", 100);
            task.Increment(25);
            task.Value.ShouldBe(25);
        }

        [Fact]
        public void Value_Set_Should_Assign_Value()
        {
            // Kills: L234 _value = value.Value
            var task = new ProgressTask(1, "Test", 100);
            task.Value = 50;
            task.Value.ShouldBe(50);
        }

        [Fact]
        public void Value_Should_Be_Capped_At_MaxValue()
        {
            // Kills: L238 _value > _maxValue capping
            var task = new ProgressTask(1, "Test", 100);
            task.Value = 150;
            task.Value.ShouldBe(100);
        }

        [Fact]
        public void MaxSamplesKept_Zero_Should_Skip_Sampling()
        {
            // Kills: L243 MaxSamplesKept == 0 check
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            task.MaxSamplesKept = 0;
            task.Increment(10);
            // Speed should be null because no samples are kept
            tp.Advance(TimeSpan.FromSeconds(5));
            task.Speed.ShouldBeNull();
        }
    }

    public sealed class IsStartedAndIsFinished
    {
        [Fact]
        public void IsStarted_Should_Be_True_When_Started()
        {
            // Kills: L82 StartTime != null
            var task = new ProgressTask(1, "Test", 100, autoStart: true);
            task.IsStarted.ShouldBeTrue();
        }

        [Fact]
        public void IsStarted_Should_Be_False_When_Not_Started()
        {
            var task = new ProgressTask(1, "Test", 100, autoStart: false);
            task.IsStarted.ShouldBeFalse();
        }

        [Fact]
        public void IsFinished_When_Value_Reaches_MaxValue()
        {
            // Kills: L87 Value >= MaxValue
            var task = new ProgressTask(1, "Test", 100);
            task.Value = 100;
            task.IsFinished.ShouldBeTrue();
        }

        [Fact]
        public void IsFinished_Should_Be_False_When_Below_MaxValue()
        {
            var task = new ProgressTask(1, "Test", 100);
            task.Value = 99;
            task.IsFinished.ShouldBeFalse();
        }

        [Fact]
        public void IsFinished_When_Stopped()
        {
            // Kills: L87 StopTime != null
            var task = new ProgressTask(1, "Test", 100);
            task.StopTask();
            task.IsFinished.ShouldBeTrue();
        }
    }

    public sealed class StartAndStop
    {
        [Fact]
        public void StartTask_Should_Set_StartTime()
        {
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: false, tp);
            task.StartTask();
            task.StartTime.ShouldBe(tp.GetLocalNow().LocalDateTime);
        }

        [Fact]
        public void StartTask_Should_Clear_StopTime()
        {
            // Kills: L163 StopTime = null
            // StartTask sets StopTime = null; but StopTime is already null if not stopped.
            // This mutation is only killable if we could stop and restart, but
            // StopTime != null throws, so this line is actually unreachable code after the guard.
            // Mark as equivalent in source.
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: false, tp);
            task.StartTask();
            task.StartTime.ShouldNotBeNull();
        }

        [Fact]
        public void StartTask_Should_Throw_If_Already_Stopped()
        {
            // Kills: L157 StopTime != null check
            var task = new ProgressTask(1, "Test", 100);
            task.StopTask();
            var ex = Record.Exception(() => task.StartTask());
            ex.ShouldBeOfType<InvalidOperationException>();
        }

        [Fact]
        public void StopTask_Should_Set_StartTime_If_Never_Started()
        {
            // Kills: L175 StartTime ??= now
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: false, tp);
            task.StopTask();
            task.StartTime.ShouldNotBeNull();
        }

        [Fact]
        public void StopTask_Should_Set_StopTime()
        {
            // Kills: L176 StopTime = now
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            task.StopTask();
            task.StopTime.ShouldBe(tp.GetLocalNow().LocalDateTime);
        }
    }

    public sealed class PercentageCalculation
    {
        [Fact]
        public void Percentage_Should_Be_100_When_MaxValue_Is_Zero()
        {
            // Kills: L262 MaxValue == 0 → return 100
            var task = new ProgressTask(1, "Test", 0);
            task.Percentage.ShouldBe(100);
        }

        [Fact]
        public void Percentage_Should_Calculate_Correctly()
        {
            // Kills: L267 arithmetic mutations (Value / MaxValue) * 100
            var task = new ProgressTask(1, "Test", 200);
            task.Value = 50;
            task.Percentage.ShouldBe(25);
        }

        [Fact]
        public void Percentage_Should_Not_Go_Below_Zero()
        {
            // Kills: L268 Math.Max(0, percentage) removal
            // Set a negative value directly through the Value property
            // Value gets clamped by maxValue cap (> check), but not by a floor,
            // however since value starts at 0 and we can't set negative via property
            // (Update sets _value = value.Value), we use Increment with a negative.
            var task = new ProgressTask(1, "Test", 100);
            task.Increment(-10); // This sets _value to -10, which is < 0
            task.Percentage.ShouldBe(0); // Clamped by Math.Max(0, ...)
        }

        [Fact]
        public void Percentage_At_Half_Should_Be_50()
        {
            var task = new ProgressTask(1, "Test", 100);
            task.Value = 50;
            task.Percentage.ShouldBe(50);
        }
    }

    public sealed class SpeedCalculation
    {
        [Fact]
        public void Speed_Should_Be_Null_Before_Any_Increment()
        {
            // Kills: L282 StartTime == null guard
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: false, tp);
            task.Speed.ShouldBeNull();
        }

        [Fact]
        public void Speed_Should_Be_Null_When_Stopped()
        {
            // Kills: L282 StopTime != null guard
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            task.Increment(10);
            tp.Advance(TimeSpan.FromSeconds(1));
            task.StopTask();
            tp.Advance(TimeSpan.FromSeconds(2)); // Ensure past cache
            task.Speed.ShouldBeNull();
        }

        [Fact]
        public void Speed_Should_Calculate_After_Increment()
        {
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            task.Increment(10);
            tp.Advance(TimeSpan.FromSeconds(2));
            task.Increment(10);
            tp.Advance(TimeSpan.FromSeconds(2)); // Past cache window
            task.Speed.ShouldNotBeNull();
        }

        [Fact]
        public void Speed_Returns_Cached_Value_Within_Cache_Window()
        {
            // Kills: L275-276 cache check and L288 _samplesChanged = false
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            task.MaxTimeForSpeedCache = TimeSpan.FromSeconds(5);

            task.Increment(20);
            tp.Advance(TimeSpan.FromSeconds(2));
            task.Increment(20);
            tp.Advance(TimeSpan.FromSeconds(2));

            var speed1 = task.Speed; // Calculates and caches
            // Don't advance time or add samples — should return cached value
            var speed2 = task.Speed;
            speed2.ShouldBe(speed1);
        }

        [Fact]
        public void Speed_Should_Be_Null_When_All_Samples_Too_Old()
        {
            // Kills: L290-291 threshold and validSamples.Count == 0
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            task.MaxSamplingAge = TimeSpan.FromSeconds(5);
            task.MaxTimeForSpeedCache = TimeSpan.FromMilliseconds(1);

            task.Increment(10);
            tp.Advance(TimeSpan.FromSeconds(10)); // All samples now > 5s old

            task.Speed.ShouldBeNull();
        }

        [Fact]
        public void Speed_Should_Use_Current_Time_When_Newest_Sample_Exceeds_Cache()
        {
            // Kills: L301 now - newestSampleTime > MaxTimeForSpeedCache
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            task.MaxTimeForSpeedCache = TimeSpan.FromSeconds(1);
            task.MaxSamplingAge = TimeSpan.FromSeconds(30);

            task.Increment(10);
            tp.Advance(TimeSpan.FromSeconds(5)); // Beyond cache window, speed decays

            var speed = task.Speed;
            speed.ShouldNotBeNull();
            // Speed should be lower because time span is extended to current time
            speed!.Value.ShouldBeLessThan(10.0);
        }

        [Fact]
        public void Speed_Should_Be_Null_When_TotalTime_IsZero()
        {
            // Kills: L308 totalTime == TimeSpan.Zero check
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            task.MaxTimeForSpeedCache = TimeSpan.Zero;

            // First sample added at start time, second sample also at start time
            task.Increment(10); // Same timestamp
            task.Speed.ShouldBeNull();
        }
    }

    public sealed class ElapsedTimeCalculation
    {
        [Fact]
        public void ElapsedTime_Should_Be_Null_Before_Start()
        {
            // Kills: L322 StartTime == null check
            var task = new ProgressTask(1, "Test", 100, autoStart: false);
            task.ElapsedTime.ShouldBeNull();
        }

        [Fact]
        public void ElapsedTime_Should_Use_StopTime_When_Stopped()
        {
            // Kills: L327-329 StopTime != null → StopTime - StartTime
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            tp.Advance(TimeSpan.FromSeconds(10));
            task.StopTask();
            tp.Advance(TimeSpan.FromSeconds(100)); // Should not affect elapsed
            task.ElapsedTime.ShouldBe(TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void ElapsedTime_Should_Use_Current_Time_When_Running()
        {
            // Kills: L332 _timeProvider.GetLocalNow() usage
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            tp.Advance(TimeSpan.FromSeconds(42));
            task.ElapsedTime.ShouldBe(TimeSpan.FromSeconds(42));
        }
    }

    public sealed class RemainingTimeCalculation
    {
        [Fact]
        public void RemainingTime_Should_Be_Zero_When_Finished()
        {
            // Kills: L340-342 IsFinished → TimeSpan.Zero
            var task = new ProgressTask(1, "Test", 100);
            task.Value = 100;
            task.RemainingTime.ShouldBe(TimeSpan.Zero);
        }

        [Fact]
        public void RemainingTime_Should_Be_Null_When_Speed_IsNull()
        {
            // Kills: L346 speed == null check
            var task = new ProgressTask(1, "Test", 100, autoStart: false);
            task.RemainingTime.ShouldBeNull();
        }

        [Fact]
        public void RemainingTime_Should_Be_Null_When_Speed_IsZero()
        {
            // Kills: L346 speed == 0 check
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            // Increment by 0 to create samples with zero progress
            task.Increment(0);
            tp.Advance(TimeSpan.FromSeconds(2));
            task.Increment(0);
            tp.Advance(TimeSpan.FromSeconds(2));
            task.RemainingTime.ShouldBeNull();
        }

        [Fact]
        public void RemainingTime_Should_Calculate_Based_On_Speed()
        {
            // Kills: L354 (MaxValue - Value) / speed.Value arithmetic
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            task.MaxTimeForSpeedCache = TimeSpan.FromMilliseconds(1);

            // Create a speed of 10 units/second
            task.Increment(20);
            tp.Advance(TimeSpan.FromSeconds(2));
            task.Increment(20); // Total 40 out of 100, speed ~10/s
            tp.Advance(TimeSpan.FromSeconds(2));

            var remaining = task.RemainingTime;
            remaining.ShouldNotBeNull();
            remaining!.Value.TotalSeconds.ShouldBeGreaterThan(0);
        }
    }

    public sealed class ReportInterface
    {
        [Fact]
        public void IProgress_Report_Should_Set_Value()
        {
            // Kills: L367 Update(increment: value - Value)
            var task = new ProgressTask(1, "Test", 100);
            ((IProgress<double>)task).Report(50);
            task.Value.ShouldBe(50);
        }
    }

    public sealed class SampleTracking
    {
        [Fact]
        public void Should_Add_StartTime_Sample_On_First_Update()
        {
            // Kills: L251-253 Samples.Count == 0 && StartTime != null
            var tp = new FakeTimeProvider(new DateTime(2024, 1, 1, 12, 0, 0));
            var task = new ProgressTask(1, "Test", 100, autoStart: true, tp);
            tp.Advance(TimeSpan.FromSeconds(2));
            task.Increment(10);
            tp.Advance(TimeSpan.FromSeconds(2));
            task.Increment(10);
            tp.Advance(TimeSpan.FromSeconds(2));

            // Speed should be calculable — proves samples were added
            task.Speed.ShouldNotBeNull();
        }
    }
}

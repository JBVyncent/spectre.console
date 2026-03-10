namespace Spectre.Console.Tests.Unit.Internal;

public sealed class InteractionDetectorTests
{
    public sealed class TheIsInteractiveMethod
    {
        [Fact]
        public void Should_Return_True_When_Support_Is_Yes()
        {
            // Arrange & Act
            var result = InteractionDetector.IsInteractive(InteractionSupport.Yes);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Should_Return_False_When_Support_Is_No()
        {
            // Arrange & Act
            var result = InteractionDetector.IsInteractive(InteractionSupport.No);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Should_Return_True_When_Detect_And_Env_Var_Is_1()
        {
            // Arrange
            Environment.SetEnvironmentVariable(
                InteractionDetector.ForceInteractiveEnvVar, "1");
            try
            {
                // Act
                var result = InteractionDetector.IsInteractive(InteractionSupport.Detect);

                // Assert
                result.Should().BeTrue();
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    InteractionDetector.ForceInteractiveEnvVar, null);
            }
        }

        [Fact]
        public void Should_Return_True_When_Detect_And_Env_Var_Is_True_Lowercase()
        {
            // Arrange
            Environment.SetEnvironmentVariable(
                InteractionDetector.ForceInteractiveEnvVar, "true");
            try
            {
                // Act
                var result = InteractionDetector.IsInteractive(InteractionSupport.Detect);

                // Assert
                result.Should().BeTrue();
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    InteractionDetector.ForceInteractiveEnvVar, null);
            }
        }

        [Fact]
        public void Should_Return_True_When_Detect_And_Env_Var_Is_True_Titlecase()
        {
            // Arrange
            Environment.SetEnvironmentVariable(
                InteractionDetector.ForceInteractiveEnvVar, "True");
            try
            {
                // Act
                var result = InteractionDetector.IsInteractive(InteractionSupport.Detect);

                // Assert
                result.Should().BeTrue();
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    InteractionDetector.ForceInteractiveEnvVar, null);
            }
        }

        [Fact]
        public void Should_Return_True_When_Detect_And_Env_Var_Is_TRUE_Uppercase()
        {
            // Arrange
            Environment.SetEnvironmentVariable(
                InteractionDetector.ForceInteractiveEnvVar, "TRUE");
            try
            {
                // Act
                var result = InteractionDetector.IsInteractive(InteractionSupport.Detect);

                // Assert
                result.Should().BeTrue();
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    InteractionDetector.ForceInteractiveEnvVar, null);
            }
        }

        [Fact]
        public void Should_Not_Force_Interactive_When_Env_Var_Is_Invalid()
        {
            // Arrange
            Environment.SetEnvironmentVariable(
                InteractionDetector.ForceInteractiveEnvVar, "yes");
            try
            {
                // Act — falls through to System.Console.IsInputRedirected check
                // In a test process, input IS redirected, so this should return false
                var result = InteractionDetector.IsInteractive(InteractionSupport.Detect);

                // Assert
                result.Should().BeFalse();
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    InteractionDetector.ForceInteractiveEnvVar, null);
            }
        }

        [Fact]
        public void Should_Not_Force_Interactive_When_Env_Var_Is_0()
        {
            // Arrange
            Environment.SetEnvironmentVariable(
                InteractionDetector.ForceInteractiveEnvVar, "0");
            try
            {
                // Act
                var result = InteractionDetector.IsInteractive(InteractionSupport.Detect);

                // Assert — falls through to IsInputRedirected which is true in test
                result.Should().BeFalse();
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    InteractionDetector.ForceInteractiveEnvVar, null);
            }
        }

        [Fact]
        public void Should_Ignore_Env_Var_When_Support_Is_Yes()
        {
            // Arrange — env var says nothing, but support is forced
            Environment.SetEnvironmentVariable(
                InteractionDetector.ForceInteractiveEnvVar, null);
            try
            {
                // Act
                var result = InteractionDetector.IsInteractive(InteractionSupport.Yes);

                // Assert
                result.Should().BeTrue();
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    InteractionDetector.ForceInteractiveEnvVar, null);
            }
        }

        [Fact]
        public void Should_Ignore_Env_Var_When_Support_Is_No()
        {
            // Arrange — env var says force interactive, but support says No
            Environment.SetEnvironmentVariable(
                InteractionDetector.ForceInteractiveEnvVar, "1");
            try
            {
                // Act
                var result = InteractionDetector.IsInteractive(InteractionSupport.No);

                // Assert
                result.Should().BeFalse();
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    InteractionDetector.ForceInteractiveEnvVar, null);
            }
        }

        [Fact]
        public void ForceInteractiveEnvVar_Has_Expected_Name()
        {
            // Assert
            InteractionDetector.ForceInteractiveEnvVar
                .Should().Be("SPECTRE_CONSOLE_FORCE_INTERACTIVE");
        }
    }
}

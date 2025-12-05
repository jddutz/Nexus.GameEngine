using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    /// <summary>
    /// Integration tests that run TestApp and verify all tests pass.
    /// TestApp returns exit code 0 on success, or the number of failed tests on failure.
    /// </summary>
    public class IntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public IntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void RunAllTests()
        {
            // Capture console output for diagnostics
            var originalOut = Console.Out;
            using var sw = new StringWriter();
            Console.SetOut(sw);

            try
            {
                // Locate TestApp.Program.Main via reflection
                var programType = Type.GetType("TestApp.Program, TestApp");
                if (programType == null)
                {
                    // Fallback: try to find assembly and type manually
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (string.Equals(asm.GetName().Name, "TestApp", StringComparison.OrdinalIgnoreCase))
                        {
                            programType = asm.GetType("TestApp.Program");
                            if (programType != null) break;
                        }
                    }
                }

                Assert.NotNull(programType);

                var mainMethod = programType!.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(mainMethod);

                // Reset exit code before run
                Environment.ExitCode = 0;

                // Run TestApp - it will execute all tests and set exit code
                var args = Array.Empty<string>();
                mainMethod!.Invoke(null, new object[] { args });

                // TestApp sets Environment.ExitCode to the number of failed tests
                // Exit code 0 = all tests passed
                Assert.Equal(0, Environment.ExitCode);
            }
            catch (TargetInvocationException tie)
            {
                // Unwrap the inner exception for clearer failure output
                var inner = tie.InnerException ?? tie;
                throw new Xunit.Sdk.XunitException($"TestApp invocation failed: {inner.GetType().Name}: {inner.Message}\nConsole output:\n{sw}\n");
            }
            finally
            {
                // Forward captured console output to xUnit's output
                try
                {
                    var captured = sw.ToString();
                    if (!string.IsNullOrEmpty(captured))
                    {
                        _output.WriteLine(captured);
                    }
                }
                catch { }

                // Restore console output
                Console.SetOut(originalOut);
            }
        }
    }
}

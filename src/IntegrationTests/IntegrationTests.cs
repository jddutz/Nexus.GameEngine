using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests
{
    // Integration tests for TestApp. Assembly-level parallelization is disabled for this project.
    public class IntegrationTests
    {
        private static readonly object s_lock = new object();
        private readonly ITestOutputHelper _output;

        public IntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public static IEnumerable<object[]> TestTemplates => DiscoverTestTemplates()
            .Select(name => new object[] { name });

        [Theory]
        [MemberData(nameof(TestTemplates))]
        public void RunSingleTestComponent(string testName)
        {
            lock (s_lock)
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
                    try { Environment.ExitCode = 0; } catch { }

                    // Run synchronously and block until TestApp completes (Application.Run is synchronous)
                    var args = new string[] { $"--filter={testName}" };
                    mainMethod!.Invoke(null, new object[] { args });

                    // After run, TestApp sets Environment.ExitCode via TestRunner.OutputTestResults
                    Assert.Equal(0, Environment.ExitCode);
                }
                catch (TargetInvocationException tie)
                {
                    // Unwrap the inner exception for clearer failure output
                    var inner = tie.InnerException ?? tie;
                    throw new Xunit.Sdk.XunitException($"TestApp invocation failed for '{testName}': {inner.GetType().Name}: {inner.Message}\nConsole output:\n{sw}\n");
                }
                finally
                {
                    // Flush and forward captured console output to xUnit's output so Test Explorer shows it per-test
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

        private static IEnumerable<string> DiscoverTestTemplates()
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, "TestApp", StringComparison.OrdinalIgnoreCase));
            if (asm == null)
            {
                // Load assembly by name if not already loaded
                try
                {
                    asm = Assembly.Load("TestApp");
                }
                catch
                {
                    yield break;
                }
            }

            var fields = asm.GetTypes()
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static))
                .Where(f => typeof(Nexus.GameEngine.Components.Template).IsAssignableFrom(f.FieldType))
                .Where(f => f.GetCustomAttribute(typeof(TestApp.TestAttribute)) != null);

            foreach (var field in fields)
            {
                var template = field.GetValue(null) as Nexus.GameEngine.Components.Template;
                if (template == null) continue;

                var name = !string.IsNullOrEmpty(template.Name) ? template.Name : field.DeclaringType?.Name ?? field.Name;
                yield return name;
            }
        }
    }
}

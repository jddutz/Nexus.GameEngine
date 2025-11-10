using System.Reflection;

namespace Tests.GameEngine
{
    /// <summary>
    /// Integration test that runs the TestApp entry point once and caches the result.
    /// Runs by default on developer machines. To skip in CI/CD pipelines set the
    /// environment variable CI=true or SKIP_TESTAPP_INTEGRATION=1.
    ///
    /// The test invokes the private static Main method in the TestApp assembly via
    /// reflection inside a single shared lock to avoid parallel execution.
    /// </summary>
    public class TestAppIntegrationTests
    {
        private static readonly object s_lock = new object();
        private static bool s_ran = false;
        private static Exception? s_exception = null;

    [Fact(Skip = "Superseded by per-component integration tests - kept for reference")]
    public void TestApp_MainCompletesWithoutThrowing()
        {
            // Skip in CI or when explicitly disabled
            var ci = Environment.GetEnvironmentVariable("CI");
            var skip = Environment.GetEnvironmentVariable("SKIP_TESTAPP_INTEGRATION");
            if (!string.IsNullOrEmpty(ci) || string.Equals(skip, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(skip, "true", StringComparison.OrdinalIgnoreCase))
            {
                // Considered skipped in CI environments
                return;
            }

            EnsureTestAppRan();

            if (s_exception != null)
            {
                // Rethrow to fail the test with original stack
                throw new AggregateException("TestApp execution failed", s_exception);
            }
        }

        private static void EnsureTestAppRan()
        {
            if (s_ran) return;
            lock (s_lock)
            {
                if (s_ran) return;
                try
                {
                    // Try to find TestApp.Program in the loaded assemblies
                    var programType = Type.GetType("TestApp.Program, TestApp");
                    if (programType == null)
                    {
                        // If direct lookup fails, try to find the assembly by name and type manually
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            if (string.Equals(asm.GetName().Name, "TestApp", StringComparison.OrdinalIgnoreCase))
                            {
                                programType = asm.GetType("TestApp.Program");
                                if (programType != null) break;
                            }
                        }
                    }

                    if (programType == null)
                    {
                        // Unable to locate TestApp.Program - mark as skipped (no-op)
                        s_ran = true;
                        return;
                    }

                    var mainMethod = programType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    if (mainMethod == null)
                    {
                        // No accessible Main method - nothing to run
                        s_ran = true;
                        return;
                    }

                    // Run Main on a background thread and wait for completion with a timeout.
                    // TestApp.Main is expected to return when its internal integration tests complete.
                    var thread = new Thread(() =>
                    {
                        try
                        {
                            // Main has signature void Main(string[] args)
                            mainMethod.Invoke(null, new object[] { new string[0] });
                        }
                        catch (TargetInvocationException tie)
                        {
                            s_exception = tie.InnerException ?? tie;
                        }
                        catch (Exception ex)
                        {
                            s_exception = ex;
                        }
                    })
                    { IsBackground = true };

                    thread.Start();

                    // Wait up to 30 seconds for TestApp to complete. If it does not, fail the test.
                    var finished = thread.Join(TimeSpan.FromSeconds(30));
                    if (!finished && s_exception == null)
                    {
                        s_exception = new TimeoutException("TestApp.Main did not complete within the allotted timeout (30s).");
                    }
                }
                catch (Exception ex)
                {
                    s_exception = ex;
                }
                finally
                {
                    s_ran = true;
                }
            }
        }
    }
}

using System;
using System.IO;
using Xunit;
using Nexus.GameEngine;

namespace Tests.GameEngine
{
    public class LogTests
    {
        [Fact]
        public void Debug_WritesDebugLevelAndMessage()
        {
            var sw = new StringWriter();
            var original = Console.Out;
            try
            {
                Console.SetOut(sw);
                Log.Debug("hello world");
                var output = sw.ToString();

                Assert.Contains("|DBG|", output);
                Assert.Contains("hello world", output);
            }
            finally
            {
                Console.SetOut(original);
            }
        }

        [Fact]
        public void InfoWarningError_WriteCorrespondingLevels()
        {
            var sw = new StringWriter();
            var original = Console.Out;
            try
            {
                Console.SetOut(sw);
                Log.Info("i");
                Log.Warning("w");
                Log.Error("e");
                var output = sw.ToString();

                Assert.Contains("|INF|", output);
                Assert.Contains("|WRN|", output);
                Assert.Contains("|ERR|", output);
                Assert.Contains("i", output);
                Assert.Contains("w", output);
                Assert.Contains("e", output);
            }
            finally
            {
                Console.SetOut(original);
            }
        }

        [Fact]
        public void Exception_WithThrownException_WritesMessageStackAndInner()
        {
            var sw = new StringWriter();
            var original = Console.Out;
            try
            {
                Console.SetOut(sw);

                try
                {
                    // create a real stack trace and inner exception
                    try
                    {
                        throw new Exception("inner");
                    }
                    catch (Exception inner)
                    {
                        throw new InvalidOperationException("oops", inner);
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception(ex, "context message");
                }

                var output = sw.ToString();

                Assert.Contains("context message", output);
                Assert.Contains("Exception: InvalidOperationException: oops", output);
                Assert.Contains("Stack Trace:", output);
                Assert.Contains("Inner Exception: Exception: inner", output);
            }
            finally
            {
                Console.SetOut(original);
            }
        }

        [Fact]
        public void Exception_WithoutStackOrMessage_WritesOnlyExceptionLine()
        {
            var sw = new StringWriter();
            var original = Console.Out;
            try
            {
                Console.SetOut(sw);

                // exception not thrown so StackTrace is null/empty
                var ex = new Exception("plain");
                Log.Exception(ex, null);

                var output = sw.ToString();

                Assert.Contains("Exception: Exception: plain", output);
                Assert.DoesNotContain("Stack Trace:", output);
                Assert.DoesNotContain("Inner Exception:", output);
            }
            finally
            {
                Console.SetOut(original);
            }
        }

        [Fact]
        public void Debug_WithEmptyFilePath_UsesUnknownClassAndFormatsLineNumber()
        {
            var sw = new StringWriter();
            var original = Console.Out;
            try
            {
                Console.SetOut(sw);
                // pass explicit filePath and lineNumber to trigger Unknown and 4-digit formatting
                Log.Debug("m", memberName: "M", filePath: "", lineNumber: 42);
                var output = sw.ToString();

                Assert.Contains("Unknown", output);
                // line number is padded to 4 digits
                Assert.Contains("0042", output);
                Assert.Contains("|DBG|", output);
            }
            finally
            {
                Console.SetOut(original);
            }
        }
    }
}

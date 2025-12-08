using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nexus.GameEngine;

/// <summary>
/// Provides simple, consistent logging output to debug channel.
/// Debug-level messages are completely removed in Release builds for zero runtime overhead.
/// Automatically captures caller information (class name, method name, line number).
/// </summary>
public static class Log
{
    /// <summary>
    /// Writes a debug-level message. Completely removed in Release builds.
    /// </summary>
    [Conditional("DEBUG")]
    public static void Debug(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("DBG", message, memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes a formatted debug-level message. Completely removed in Release builds.
    /// </summary>
    [Conditional("DEBUG")]
    public static void Debug(
        string format,
        object? arg0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("DBG", string.Format(format, arg0), memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes a formatted debug-level message. Completely removed in Release builds.
    /// </summary>
    [Conditional("DEBUG")]
    public static void Debug(
        string format,
        object? arg0,
        object? arg1,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("DBG", string.Format(format, arg0, arg1), memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes a formatted debug-level message. Completely removed in Release builds.
    /// </summary>
    [Conditional("DEBUG")]
    public static void Debug(
        string format,
        object? arg0,
        object? arg1,
        object? arg2,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("DBG", string.Format(format, arg0, arg1, arg2), memberName, filePath, lineNumber);
    }



    /// <summary>
    /// Writes an informational message.
    /// </summary>
    public static void Info(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("INF", message, memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes a formatted informational message.
    /// </summary>
    public static void Info(
        string format,
        object? arg0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("INF", string.Format(format, arg0), memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes a formatted informational message.
    /// </summary>
    public static void Info(
        string format,
        object? arg0,
        object? arg1,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("INF", string.Format(format, arg0, arg1), memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes a formatted informational message.
    /// </summary>
    public static void Info(
        string format,
        object? arg0,
        object? arg1,
        object? arg2,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("INF", string.Format(format, arg0, arg1, arg2), memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes a warning message.
    /// </summary>
    public static void Warning(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("WRN", message, memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes a formatted warning message.
    /// </summary>
    public static void Warning(
        string format,
        object? arg0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("WRN", string.Format(format, arg0), memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes a formatted warning message.
    /// </summary>
    public static void Warning(
        string format,
        object? arg0,
        object? arg1,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("WRN", string.Format(format, arg0, arg1), memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes a formatted warning message.
    /// </summary>
    public static void Warning(
        string format,
        object? arg0,
        object? arg1,
        object? arg2,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("WRN", string.Format(format, arg0, arg1, arg2), memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes an error message.
    /// </summary>
    public static void Error(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("ERR", message, memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes a formatted error message.
    /// </summary>
    public static void Error(
        string format,
        object? arg0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("ERR", string.Format(format, arg0), memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes a formatted error message.
    /// </summary>
    public static void Error(
        string format,
        object? arg0,
        object? arg1,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("ERR", string.Format(format, arg0, arg1), memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes a formatted error message.
    /// </summary>
    public static void Error(
        string format,
        object? arg0,
        object? arg1,
        object? arg2,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        WriteLog("ERR", string.Format(format, arg0, arg1, arg2), memberName, filePath, lineNumber);
    }

    /// <summary>
    /// Writes an exception with optional additional context message.
    /// </summary>
    public static void Exception(
        Exception ex,
        string? message = null,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {

        if (message != null)
        {
            WriteLog("ERR", message, memberName, filePath, lineNumber);
        }
        
        WriteLog("ERR", $"Exception: {ex.GetType().Name}: {ex.Message}", memberName, filePath, lineNumber);
        
        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            WriteLog("ERR", $"Stack Trace: {ex.StackTrace}", memberName, filePath, lineNumber);
        }
        if (ex.InnerException != null)
        {
            WriteLog("ERR", $"Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}", memberName, filePath, lineNumber);
        }

    }

    private static void WriteLog(
        string level, 
        string message, 
        string memberName, 
        string filePath, 
        int lineNumber)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var className = GetClassNameFromFilePath(filePath);
        var output = $"{timestamp}|{level}|{lineNumber:D4}|{className}: {message}";

        // Write to both console and debug channel
        Console.WriteLine(output);
    }

    /// <summary>
    /// Extracts the class name from a file path (e.g., "C:\...\ClassName.cs" -> "ClassName").
    /// </summary>
    private static string GetClassNameFromFilePath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return "Unknown";

        var fileName = Path.GetFileNameWithoutExtension(filePath);
        return fileName;
    }
}
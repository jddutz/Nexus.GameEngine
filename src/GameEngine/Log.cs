using System.Runtime.CompilerServices;

namespace Nexus.GameEngine;

/// <summary>
/// Provides simple, consistent logging output to debug channel.
/// All methods are compiled out in Release builds for zero runtime overhead.
/// Automatically captures caller information (class name, method name, line number).
/// </summary>
public static class Log
{
    /// <summary>
    /// Writes a debug-level message.
    /// </summary>
    public static void Debug(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
#if DEBUG
        WriteLog("DBG", message, memberName, filePath, lineNumber);
#endif
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
#if DEBUG
        WriteLog("INF", message, memberName, filePath, lineNumber);
#endif
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
#if DEBUG
        WriteLog("WRN", message, memberName, filePath, lineNumber);
#endif
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
#if DEBUG
        WriteLog("ERR", message, memberName, filePath, lineNumber);
#endif
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
#if DEBUG
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
#endif
    }

#if DEBUG
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
        
        // Write to debug channel only (visible in debugger output and console when attached)
        System.Diagnostics.Debug.WriteLine(output);
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
#endif
}
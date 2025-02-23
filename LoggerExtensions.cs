using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

internal static partial class LoggerExtensions
{
     private static readonly ConditionalWeakTable<string, Stopwatch> _stopwatches = new();
    
    [LoggerMessage(EventId = 0, Level = LogLevel.Critical, Message = "Food recall notice: {foodRecallNotice}")]
    public static partial void FoodRecallNotice(
        this ILogger logger,
        in FoodRecallNotice foodRecallNotice);
        [LoggerMessage(LogLevel.Information, "Food `{name}` price changed to `{price}`.")]
    public static partial void FoodPriceChanged(this ILogger logger, string name, double price);
     [LoggerMessage(LogLevel.Information, "Started `{methodname}` in `{classname}`.")]
    public static partial void LogInformation(this ILogger logger, string classname, string methodname);

    public static void LogInformation(
        this ILogger logger,
        string message,
        LogLevel logLevel = LogLevel.Information,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
    {
       var activity = Activity.Current;
        var className = GetClassName(filePath);
        var method = GetFullMethodName(methodName);
      
        logger.LogInformation(className, method);
    }

     // [LogProperties("UserId", "Action")]
    public static void LogUserAction(
        this ILogger logger,
        string userId,
        string action,
        LogLevel logLevel = LogLevel.Information,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
    {
        var className = GetClassName(filePath);
        var method = GetFullMethodName(methodName);

        logger.Log(logLevel, $"User {userId} performed {action} in {method} of {className}");
    }
     public static void LogStartExecutionTime(
        this ILogger logger,
        string message,
        LogLevel logLevel = LogLevel.Information,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
    {
        var className = GetClassName(filePath);
        var method = GetFullMethodName(methodName);
        //var key = new ExecutionKey(className, method);
        var key = className + method;

        var stopwatch = _stopwatches.Where(x=>x.Key.Contains(key)).FirstOrDefault().Value;
        if (stopwatch != null)
        {
            logger.LogWarning($"Execution timer for {methodName} in {filePath} was already started.");
            return;
        }

        stopwatch = Stopwatch.StartNew();
        _stopwatches.Add(key, stopwatch);
        logger.Log(logLevel, $"Started {methodName} in {filePath}: {message}");
    }

    public static void LogExecutionTime(
        this ILogger logger,
        string message,
        LogLevel logLevel = LogLevel.Information,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
    {
         var className = GetClassName(filePath);
        var method = GetFullMethodName(methodName);
       // var key = new ExecutionKey(className, method);
       var key = className + method;
        var stopwatch = _stopwatches.Where(x=>x.Key.Contains(key)).FirstOrDefault().Value;
        if (stopwatch == null)
        {
            logger.LogWarning($"No execution timer found for {methodName} in {filePath}. Call LogStartExecutionTime first.");
            return;
        }

        stopwatch.Stop();
        _stopwatches.Remove(key);
        logger.Log(logLevel, $"Completed {methodName} in {filePath}: {message} | Execution Time: {stopwatch.ElapsedMilliseconds} ms");
    }

     private static string GetClassName(string filePath)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        return fileName;
    }
     private static string GetFullMethodName(string input)
    {
        // var stackTrace = new StackTrace();
        // var frame = stackTrace.GetFrame(2); // Adjust the frame index as needed
        // var method = frame?.GetMethod();
        // return method.Name;
        return Regex.Replace(input, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
    }
  
}

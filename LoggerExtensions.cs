internal static partial class LoggerExtensions
{
    
    [LoggerMessage(EventId = 0, Level = LogLevel.Critical, Message = "Food recall notice: {foodRecallNotice}")]
    public static partial void FoodRecallNotice(
        this ILogger logger,
        in FoodRecallNotice foodRecallNotice);
        [LoggerMessage(LogLevel.Information, "Food `{name}` price changed to `{price}`.")]
    public static partial void FoodPriceChanged(this ILogger logger, string name, double price);
     [LoggerMessage(LogLevel.Information, "Started `{methodname}` in `{classname}`.")]
    public static partial void LogInformation(this ILogger logger, string classname, string methodname);
  
}

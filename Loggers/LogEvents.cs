namespace Darlin.Logging;

public static class LogEvents
{
    // App lifecycle
    public static readonly EventId AppStart = new(1000, "AppStart");
    public static readonly EventId AppStop = new(1001, "AppStop");

    // Subscriptions
    public static readonly EventId SubscriptionSuccess = new(2000, "SubscriptionSuccess");
    public static readonly EventId SubscriptionRetry = new(2001, "SubscriptionRetry");

    // Ticker init
    public static readonly EventId TickerInitAttempt = new(3000, "TickerInitAttempt");
    public static readonly EventId TickerInitialized = new(3001, "TickerInitialized");
    public static readonly EventId TickerInitFailed = new(3002, "TickerInitFailed");

    // Ticker loop
    public static readonly EventId TickerLoopStart = new(4000, "TickerLoopStart");
    public static readonly EventId TickerLoopError = new(4001, "TickerLoopError");

    // Market data
    public static readonly EventId PriceUpdated = new(5000, "PriceUpdated");
    public static readonly EventId DepthUpdated = new(5001, "DepthUpdated");
    public static readonly EventId KlineUpdated = new(5002, "KlineUpdated");

    // Threshold / volume
    public static readonly EventId ThresholdCalculated = new(6000, "ThresholdCalculated");

    // OrderBlock
    public static readonly EventId PendingLong = new(7000, "PendingLong");
    public static readonly EventId PendingShort = new(7001, "PendingShort");
    public static readonly EventId OpenPosition = new(8000, "OpenPosition");
    public static readonly EventId ClosePosition = new(8001, "ClosePosition");
}
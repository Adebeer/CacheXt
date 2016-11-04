namespace CacheXt.NCache
{
    public interface INCacheConfig
    {
        int MaxRetries { get; }
        int SleepTimeInMilliSeconds { get; }
        int RetryFrequency { get; }
        string CacheName { get; }
    }

    public class NCacheConfig : INCacheConfig
    {
        public int MaxRetries { get; } = 100;
        public int SleepTimeInMilliSeconds { get; } = 20;
        public int RetryFrequency { get; } = 10;
        public string CacheName { get; } = "myCache";
    }
}
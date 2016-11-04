using System;
using System.Collections;
using System.Threading;
using System.Diagnostics;

namespace CacheXt.NCache
{
    using Alachisoft.NCache.Runtime;
    using Alachisoft.NCache.Runtime.Exceptions;
    using Alachisoft.NCache.Web.Caching;
    using CacheXt.Core;

    public class NCacheWrapper : ICacheWrapper, IDisposable
    {
        private readonly DateTime _noAbsoluteExpiry;
        private readonly TimeSpan _noSlidingExpiry;

        private readonly Cache _cache;

        private bool _disposed;
        private readonly INCacheConfig _config;

        public NCacheWrapper(INCacheConfig config)
        {
            _config = config;

            _noAbsoluteExpiry = DateTime.MaxValue;
            _noSlidingExpiry = new TimeSpan(0);

            Debug.WriteLineIf(Tracing.Switch.TraceInfo, $"NCache InitialiseCache - initialising with cacheName: {_config.CacheName}");

            NCache.InitializeCache(_config.CacheName);
            _cache = NCache.Caches[_config.CacheName];
        }

        public void Set(string key, object value)
        {
            _cache.Insert(key, value, _noAbsoluteExpiry, _noSlidingExpiry, CacheItemPriority.Normal);
        }

        public void Set(string key, object value, DateTime expiresAt)
        {
            _cache.Insert(key, value, expiresAt, _noSlidingExpiry, CacheItemPriority.Normal);
        }

        public void Set(string key, object value, TimeSpan validFor)
        {
            var expiresAt = DateTime.UtcNow.Add(validFor);
            Set(key, value, expiresAt);
        }

        public object Get(string key)
        {
            return _cache.Get(key);
        }

        public T UpdateWithLock<T>(string key, Func<T> initialValueWhenNotExist, Func<T, T> updateOperation, TimeSpan timeout) where T : class
        {
            var lockHandle = new LockHandle();
            CacheItem cacheItem = null;

            for (int i = 0; i < _config.MaxRetries; ++i)
            {
                // NOTE: With NCache - if key not in cache, will not acquire lock and return null. 
                // If it is in cache, but someone else has a lock, it'll still return null
                cacheItem = _cache.GetCacheItem(key, timeout, ref lockHandle, true);
                if (cacheItem != null)
                {
                    break;
                }
                // Retry logic... key may simply not be in Cache...so add it if we need to
                int retryCount = i / _config.RetryFrequency;
                Thread.Sleep(_config.SleepTimeInMilliSeconds * retryCount); // first couple of times retryCount = 0 which ensures we at least try a _cache.Add() once before sleeping as it simply may not be in cache yet... 
                if (((i + 1) % _config.RetryFrequency) == 0)
                {
                    T initialValue = initialValueWhenNotExist();
                    if (initialValue == null)
                    {
                        throw new InvalidOperationException("UpdateWithLock: Initial value function should never return null. Key:" + key);
                    }
                    try
                    {
                        _cache.Add(key, initialValueWhenNotExist());
                    }
                    catch (OperationFailedException ex)
                    {
                        Debug.WriteLineIf(Tracing.Switch.TraceVerbose, $"UpdateWithLock: Failed adding Key: {key}...Retrying ({retryCount}) - Reason: {ex.ToString()}");
                    }
                }
            }
            if (cacheItem == null)
            {
                throw new OperationFailedException(string.Format("UpdateWithLock: Failed to access Key: {0}", key));
            }
            T updatedResult = null;
            try
            {
                updatedResult = updateOperation(cacheItem.Value as T) ?? initialValueWhenNotExist();
            }
            catch
            {
                if (updatedResult == null)
                {
                    // previous call threw an exception - make sure we release lock
                    _cache.Unlock(key, lockHandle);
                    throw;
                }
            }
            _cache.Insert(key, new CacheItem(updatedResult), lockHandle, true);

            return updatedResult;
        }

        public IDictionary Get(params string[] keys)
        {
            return _cache.GetBulk(keys);
        }

        public void Remove(string key)
        {
            _cache.Delete(key);
        }

        public void Remove(params string[] keys)
        {
            _cache.DeleteBulk(keys);
        }

        public bool Exists(string key)
        {
            return _cache.Get(key) != null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_cache != null)
                    {
                        _cache.Dispose();
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

}

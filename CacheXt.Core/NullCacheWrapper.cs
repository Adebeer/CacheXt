using System;
using System.Collections;

namespace CacheXt.Core
{
    /// <summary>
    /// An implementation that does nothing (e.g. cache disabled). Useful for testing and/or when want to turn off caching
    /// </summary>
    public class NullCacheWrapper : ICacheWrapper
    {
        public void Set(string key, object value)
        { }

        public void Set(string key, object value, DateTime expiresAt)
        { }

        public void Set(string key, object value, TimeSpan validFor)
        { }

        public object Get(string key)
        {
            return null;
        }

        public T UpdateWithLock<T>(string key, Func<T> initialValueWhenNotExist, Func<T, T> updateOperation, TimeSpan timeout) where T : class
        {
            return updateOperation(initialValueWhenNotExist()) ?? initialValueWhenNotExist();
        }

        public IDictionary Get(params string[] keys)
        {
            return new Hashtable();
        }

        public void Remove(string key)
        { }

        public void Remove(params string[] keys)
        { }

        public bool Exists(string key)
        {
            return false;
        }
    }
}
using System;
using System.Collections;

namespace CacheXt.Core
{
    public interface ICacheWrapper
    {
        void Set(string key, object value);
        void Set(string key, object value, DateTime expiresAt);
        void Set(string key, object value, TimeSpan validFor);

        object Get(string key);
        IDictionary Get(params string[] keys);

        /// <summary>
        /// This operation allows you to update a value in Cache. It will also use locking (where supported by Cache implementation) to ensure that only one process updates the cached value via updateOperation
        /// For example, if the value stored is a list, you can use this method to have several processes add/update values to this this list without fear of loosing updates by other concurrent processes
        /// a) Acquires lock on key 
        /// b) Checks if the key has a value stored - if not, set it to the value returned by the initialValueWhenNotExists() function
        /// c) Call updateOperation() to perform an update on the cached value. If this function returns null, the cache entry will reset to initialValueWhenNotExists()
        /// d) store the updated result in the cache
        /// e) Releases lock
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="updateOperation">Function to call to update existing data. If this function returns null, then <param name="initialValueWhenNotExist"></param> will be used to get a non-null value</param>
        /// <param name="initialValueWhenNotExist">Add this NON-NULL value to the cache if it doesn't exist</param>
        /// <param name="timeout">lock timeout ... best to use a fairly small value in case application terminates unexpectedly to ensure lock is not held indefinately</param>
        /// <returns></returns>
        T UpdateWithLock<T>(string key, Func<T> initialValueWhenNotExist, Func<T, T> updateOperation, TimeSpan timeout) where T : class;

        void Remove(string key);
        void Remove(params string[] keys);

        bool Exists(string key);
    }
}
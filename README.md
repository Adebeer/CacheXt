# CacheXt

A C# Cache Extensions library to: 
- Expose a simple cache abstraction (ICacheWrapper) so different cache providers can be used through same common interface
- Utility and helper method(s) to assist with common/tricky cache operations 

At this stage this project only provides two implementations: 
- NCache from Alachisoft (R)
- Null Cache (i.e. a disabled cache).

## Helper Methods
The following helper methods are currently implemented:
- **UpdateWithLock:**

	A helper method (UpdateWithLock) is also provided to assist with using locking in NCache to update and/or initialize a value in NCache. In particular, the use of locking is poorly documented in NCache and having a concurrent-safe operation took some trial and error.
	
	The following example will run parallel threads to concatenate a list of values 1-16. Without locking, the string would not contain all numbers 1-16; but with locking, all will be there (not necessarily in sequential order!) 
	```
	TimeSpan lockTimeout = new TimeSpan(0,1,0);
	Enumerable.Range(1, 16)
				.AsParallel()
				.WithExecutionMode(ParallelExecutionMode.ForceParallelism)
				.AsUnordered()
				.WithDegreeOfParallelism(16)
				.ForAll(n => TestOperation(key1, n, lockTimeout));
				
	...
	
	private static void TestOperation(string key1, int n, TimeSpan lockTimeout)
	{
		cacheWrapper.UpdateWithLock(
			key1,
			() => string.Empty, 
			x => string.IsNullOrWhiteSpace(x) ? n.ToString() : string.Format("{0};{1}", x, n),
			lockTimeout);
	}
	```
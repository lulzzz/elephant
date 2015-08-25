﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Takenet.Elephant.Specialized
{
    public class CacheSetMap<TKey, TValue> : CacheMap<TKey, ISet<TValue>>, ISetMap<TKey, TValue>
    {
        public CacheSetMap(ISetMap<TKey, TValue> source, ISetMap<TKey, TValue> cache, TimeSpan synchronizationTimeout)
            : base(source, cache, synchronizationTimeout)
        {
        }

        protected CacheSetMap(ISetMap<TKey, TValue> source, ISetMap<TKey, TValue> cache, ISynchronizer<IMap<TKey, ISet<TValue>>> synchronizer)
            : base(source, cache, synchronizer)
        {
        }

        public override async Task<ISet<TValue>> GetValueOrDefaultAsync(TKey key)
        {
            var value = await base.GetValueOrDefaultAsync(key);
            if (value == null) return null;            
            var sourceValue = await Source.GetValueOrDefaultAsync(key).ConfigureAwait(false);
            return new InternalCacheSet(sourceValue, value);
        }


        private class InternalCacheSet : CacheSet<TValue>
        {
            public InternalCacheSet(ISet<TValue> source, ISet<TValue> cache) 
                : base(source, cache, BypassSynchronizer.Instance)
            {
            }
        }

        private class BypassSynchronizer : ISynchronizer<ISet<TValue>>
        {
            public static readonly BypassSynchronizer Instance = new BypassSynchronizer();

            public Task SynchronizeAsync(ISet<TValue> source, ISet<TValue> target)
            {
                return TaskUtil.CompletedTask;
            }
        }
    }
}

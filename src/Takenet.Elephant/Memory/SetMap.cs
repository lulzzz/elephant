﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Takenet.Elephant.Memory
{
    /// <summary>
    /// Implements the <see cref="ISetMap{TKey,TItem}"/> interface using the <see cref="Map{TKey,TValue}"/> and <see cref="Set{T}"/> classes.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TItem"></typeparam>
    public class SetMap<TKey, TItem> : Map<TKey, ISet<TItem>>, ISetMap<TKey, TItem>, IItemSetMap<TKey, TItem>, IQueryableStorage<TItem>, IQueryableStorage<KeyValuePair<TKey, TItem>>, IKeyQueryableMap<TKey, TItem>
    {
        private readonly IEqualityComparer<TItem> _valueEqualityComparer;

        public SetMap()
            : this(EqualityComparer<TItem>.Default)
        {

        }

        public SetMap(IEqualityComparer<TItem> valueEqualityComparer)
            : base(() => new Set<TItem>(valueEqualityComparer))
        {
            _valueEqualityComparer = valueEqualityComparer;
        }

        public override async Task<bool> TryAddAsync(TKey key, ISet<TItem> value, bool overwrite = false)
        {
            var set = ValueFactory();
            var enumerable = await value.AsEnumerableAsync().ConfigureAwait(false);
            await enumerable.ForEachAsync(
                async (i) => await set.AddAsync(i).ConfigureAwait(false), CancellationToken.None)
            .ConfigureAwait(false);
            return await base.TryAddAsync(key, set, overwrite).ConfigureAwait(false);
        }

        public async Task<TItem> GetItemOrDefaultAsync(TKey key, TItem item)
        {
            var items = await GetValueOrDefaultAsync(key).ConfigureAwait(false);
            if (items != null)
            {
                return await
                    (await items.AsEnumerableAsync().ConfigureAwait(false))
                        .FirstOrDefaultAsync(i => _valueEqualityComparer.Equals(i, item)).ConfigureAwait(false);
            }

            return default(TItem);
        }

        public Task<ISet<TItem>> GetValueOrEmptyAsync(TKey key)
        {
            return InternalDictionary.GetOrAdd(key, k => ValueFactory()).AsCompletedTask();
        }

        public Task<QueryResult<TItem>> QueryAsync<TResult>(Expression<Func<TItem, bool>> @where, Expression<Func<TItem, TResult>> @select, int skip, int take, CancellationToken cancellationToken)
        {
            if (@where == null) @where = value => true;
            if (select != null &&
                select.ReturnType != typeof(TItem))
            {
                throw new NotImplementedException("The select parameter is not supported yet");
            }
            var totalValues = InternalDictionary
                .Values
                .Select(v => v.AsEnumerableAsync().Result)
                .SelectMany(v => v)
                .Where(where.Compile());

            var resultValues = totalValues
                .Skip(skip)
                .Take(take)
                .Select(pair => pair);

            return Task.FromResult(
                new QueryResult<TItem>(new AsyncEnumerableWrapper<TItem>(resultValues), totalValues.Count()));
        }

        public Task<QueryResult<KeyValuePair<TKey, TItem>>> QueryAsync<TResult>(Expression<Func<KeyValuePair<TKey, TItem>, bool>> @where, Expression<Func<KeyValuePair<TKey, TItem>, TResult>> @select, int skip, int take, CancellationToken cancellationToken)
        {
            if (@where == null) @where = value => true;
            if (select != null &&
                select.ReturnType != typeof(KeyValuePair<TKey, TItem>))
            {
                throw new NotImplementedException("The select parameter is not supported yet");
            }
            var totalValues = InternalDictionary
                .SelectMany(v => v
                    .Value
                    .AsEnumerableAsync().Result
                    .Select(i => new KeyValuePair<TKey, TItem>(v.Key, i)))
                .Where(pair => where.Compile().Invoke(pair));
            var resultValues = totalValues
                .Skip(skip)
                .Take(take);

            return Task.FromResult(
                new QueryResult<KeyValuePair<TKey, TItem>>(new AsyncEnumerableWrapper<KeyValuePair<TKey, TItem>>(resultValues), totalValues.Count()));
        }

        public Task<QueryResult<TKey>> QueryForKeysAsync<TResult>(Expression<Func<TItem, bool>> @where, Expression<Func<TKey, TResult>> @select, int skip, int take,
            CancellationToken cancellationToken)
        {
            if (@where == null) @where = value => true;
            if (select != null &&
                select.ReturnType != typeof(TKey))
            {
                throw new NotImplementedException("The select parameter is not supported yet");
            }

            var predicate = where.Compile();
            var totalValues = this.InternalDictionary                
                .Where(pair =>
                    pair.Value.AsEnumerableAsync().Result.Any(m =>
                        predicate.Invoke(m)));

            var resultValues = totalValues
                .Skip(skip)
                .Take(take)
                .Select(pair => pair.Key);

            return Task.FromResult(
                new QueryResult<TKey>(new AsyncEnumerableWrapper<TKey>(resultValues), totalValues.Count()));
        }
    }
}

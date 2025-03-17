using System;
using System.Linq.Expressions;
using System.Linq;

namespace Atis.LinqToSql
{
    public static class QueryExtensions
    {
        public static T Table<T>()
        {
            throw new NotImplementedException();
        }

        public static T Schema<T>(this IQueryable<T> source)
        {
            throw new NotImplementedException();
        }

        public static IQueryable<T> From<T>(this IQueryProvider provider, Expression<Func<T>> dataSources)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            return provider.CreateQuery<T>(
                Expression.Call(
                    typeof(QueryExtensions),
                    nameof(From),
                    new Type[] { typeof(T) },
                    Expression.Constant(provider),
                    dataSources));
        }

        public static IQueryable<T> DataSet<T>(this IQueryProvider provider)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            return provider.CreateQuery<T>(
                Expression.Call(
                    typeof(QueryExtensions),
                    nameof(DataSet),
                    new Type[] { typeof(T) },
                    Expression.Constant(provider)));
        }

        public static IQueryable<R> LeftJoin<T, TJoin, R>(this IQueryable<T> source, IQueryable<TJoin> otherSource, Expression<Func<T, TJoin, R>> newSourceFactory, Expression<Func<R, bool>> joinPredicate)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (otherSource is null)
            {
                throw new ArgumentNullException(nameof(otherSource));
            }

            if (newSourceFactory is null)
            {
                throw new ArgumentNullException(nameof(newSourceFactory));
            }

            if (joinPredicate is null)
            {
                throw new ArgumentNullException(nameof(joinPredicate));
            }

            return source.Provider.CreateQuery<R>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, IQueryable<TJoin>, Expression<Func<T, TJoin, R>>, Expression<Func<R, bool>>, IQueryable<R>>(LeftJoin).Method,
                    source.Expression, otherSource.Expression, Expression.Quote(newSourceFactory), Expression.Quote(joinPredicate)));
        }

        public static IQueryable<R> RightJoin<T, TJoin, R>(this IQueryable<T> source, IQueryable<TJoin> otherSource, Expression<Func<T, TJoin, R>> newSourceFactory, Expression<Func<R, bool>> joinPredicate)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (otherSource is null)
            {
                throw new ArgumentNullException(nameof(otherSource));
            }

            if (newSourceFactory is null)
            {
                throw new ArgumentNullException(nameof(newSourceFactory));
            }

            if (joinPredicate is null)
            {
                throw new ArgumentNullException(nameof(joinPredicate));
            }

            return source.Provider.CreateQuery<R>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, IQueryable<TJoin>, Expression<Func<T, TJoin, R>>, Expression<Func<R, bool>>, IQueryable<R>>(RightJoin).Method,
                    source.Expression, otherSource.Expression, Expression.Quote(newSourceFactory), Expression.Quote(joinPredicate)));
        }

        public static IQueryable<R> InnerJoin<T, TJoin, R>(this IQueryable<T> source, IQueryable<TJoin> otherSource, Expression<Func<T, TJoin, R>> newSourceFactory, Expression<Func<R, bool>> joinPredicate)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (otherSource is null)
            {
                throw new ArgumentNullException(nameof(otherSource));
            }

            if (newSourceFactory is null)
            {
                throw new ArgumentNullException(nameof(newSourceFactory));
            }

            if (joinPredicate is null)
            {
                throw new ArgumentNullException(nameof(joinPredicate));
            }

            return source.Provider.CreateQuery<R>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, IQueryable<TJoin>, Expression<Func<T, TJoin, R>>, Expression<Func<R, bool>>, IQueryable<R>>(InnerJoin).Method,
                    source.Expression, otherSource.Expression, Expression.Quote(newSourceFactory), Expression.Quote(joinPredicate)));
        }

        public static IQueryable<T> LeftJoin<T, TJoin>(this IQueryable<T> source, Expression<Func<T, TJoin>> otherSource, Expression<Func<T, bool>> joinPredicate)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (otherSource is null)
            {
                throw new ArgumentNullException(nameof(otherSource));
            }

            if (joinPredicate is null)
            {
                throw new ArgumentNullException(nameof(joinPredicate));
            }

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, TJoin>>, Expression<Func<T, bool>>, IQueryable<T>>(LeftJoin).Method,
                    source.Expression, Expression.Quote(otherSource), Expression.Quote(joinPredicate)));
        }

        public static IQueryable<T> RightJoin<T, TJoin>(this IQueryable<T> source, Expression<Func<T, TJoin>> otherSource, Expression<Func<T, bool>> joinPredicate)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (otherSource is null)
            {
                throw new ArgumentNullException(nameof(otherSource));
            }

            if (joinPredicate is null)
            {
                throw new ArgumentNullException(nameof(joinPredicate));
            }

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, TJoin>>, Expression<Func<T, bool>>, IQueryable<T>>(RightJoin).Method,
                    source.Expression, Expression.Quote(otherSource), Expression.Quote(joinPredicate)));
        }

        public static IQueryable<T> InnerJoin<T, TJoined>(this IQueryable<T> source, Expression<Func<T, TJoined>> otherSource, Expression<Func<T, bool>> joinPredicate)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (otherSource is null)
            {
                throw new ArgumentNullException(nameof(otherSource));
            }
            if (joinPredicate is null)
            {
                throw new ArgumentNullException(nameof(joinPredicate));
            }
            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, TJoined>>, Expression<Func<T, bool>>, IQueryable<T>>(InnerJoin).Method,
                    source.Expression, Expression.Quote(otherSource), Expression.Quote(joinPredicate)));
        }

        public static IQueryable<R> CrossApply<T, TJoin, R>(this IQueryable<T> source, Expression<Func<T, IQueryable<TJoin>>> otherSource, Expression<Func<T, TJoin, R>> newSourceFactory)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (otherSource is null)
            {
                throw new ArgumentNullException(nameof(otherSource));
            }

            if (newSourceFactory is null)
            {
                throw new ArgumentNullException(nameof(newSourceFactory));
            }

            return source.Provider.CreateQuery<R>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, IQueryable<TJoin>>>, Expression<Func<T, TJoin, R>>, IQueryable<R>>(CrossApply).Method,
                    source.Expression, Expression.Quote(otherSource), Expression.Quote(newSourceFactory)));
        }

        public static IQueryable<R> OuterApply<T, TJoin, R>(this IQueryable<T> source, Expression<Func<T, IQueryable<TJoin>>> otherSource, Expression<Func<T, TJoin, R>> newSourceFactory)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (otherSource is null)
            {
                throw new ArgumentNullException(nameof(otherSource));
            }

            if (newSourceFactory is null)
            {
                throw new ArgumentNullException(nameof(newSourceFactory));
            }

            return source.Provider.CreateQuery<R>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, IQueryable<TJoin>>>, Expression<Func<T, TJoin, R>>, IQueryable<R>>(OuterApply).Method,
                    source.Expression, Expression.Quote(otherSource), Expression.Quote(newSourceFactory)));
        }

        //public static IQueryable<IGrouping<T, R>> Having<T, R>(this IQueryable<IGrouping<T, R>> grouping, Expression<Func<IGrouping<T, R>, bool>> predicate)
        //{
        //    if (grouping is null)
        //    {
        //        throw new ArgumentNullException(nameof(grouping));
        //    }

        //    if (predicate is null)
        //    {
        //        throw new ArgumentNullException(nameof(predicate));
        //    }

        //    return grouping.Provider.CreateQuery<IGrouping<T, R>>(
        //        Expression.Call(
        //            null,
        //            new Func<IQueryable<IGrouping<T, R>>, Expression<Func<IGrouping<T, R>, bool>>, IQueryable<IGrouping<T, R>>>(Having).Method,
        //            grouping.Expression, Expression.Quote(predicate)));
        //}

        public static IQueryable<T> Having<T>(this IQueryable<T> grouping, Expression<Func<IGrouping<T, T>, bool>> predicate)
        {
            if (grouping is null)
            {
                throw new ArgumentNullException(nameof(grouping));
            }

            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return grouping.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<IGrouping<T, T>, bool>>, IQueryable<T>>(Having).Method,
                    grouping.Expression, Expression.Quote(predicate)));
        }

        public static IQueryable<T> HavingOr<T>(this IQueryable<T> source, Expression<Func<IGrouping<T, T>, bool>> predicate)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<IGrouping<T, T>, bool>>, IQueryable<T>>(HavingOr).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        public static IQueryable<T> Paging<T>(this IQueryable<T> source, int pageNumber, int pageSize)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, int, int, IQueryable<T>>(Paging).Method,
                    source.Expression, Expression.Constant(pageNumber), Expression.Constant(pageSize)));
        }

        public static IQueryable<T> UnionAll<T>(this IQueryable<T> source, IQueryable<T> otherSource)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (otherSource is null)
            {
                throw new ArgumentNullException(nameof(otherSource));
            }

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, IQueryable<T>, IQueryable<T>>(UnionAll).Method,
                    source.Expression, otherSource.Expression));
        }

        public static IQueryable<T> RecursiveUnion<T>(this IQueryable<T> source, Expression<Func<IQueryable<T>, IQueryable<T>>> recursiveMember)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (recursiveMember is null)
            {
                throw new ArgumentNullException(nameof(recursiveMember));
            }

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<IQueryable<T>, IQueryable<T>>>, IQueryable<T>>(RecursiveUnion).Method,
                    source.Expression, Expression.Quote(recursiveMember)));
        }

        public static IQueryable<TKey> OrderByDesc<TSource, TKey>(this IQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
        {
            // this method is for backward compatibility
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            return source.Provider.CreateQuery<TKey>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, Expression<Func<TSource, TKey>>, IQueryable<TKey>>(OrderByDesc).Method,
                    source.Expression, Expression.Quote(keySelector)));
        }

        public static IQueryable<TSource> Top<TSource>(this IQueryable<TSource> source, int count)
        {
            // same as Take
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    new Func<IQueryable<TSource>, int, IQueryable<TSource>>(Queryable.Take).Method,
                    source.Expression, Expression.Constant(count)));
        }

        public static IQueryable<T> WhereOr<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, bool>>, IQueryable<T>>(WhereOr).Method,
                    source.Expression, Expression.Quote(predicate)));
        }

        public static IQueryable<R> FullOuterJoin<T, TJoin, R>(this IQueryable<T> source, IQueryable<TJoin> otherSource, Expression<Func<T, TJoin, R>> newSourceFactory, Expression<Func<R, bool>> joinPredicate)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (otherSource is null)
            {
                throw new ArgumentNullException(nameof(otherSource));
            }

            if (newSourceFactory is null)
            {
                throw new ArgumentNullException(nameof(newSourceFactory));
            }

            if (joinPredicate is null)
            {
                throw new ArgumentNullException(nameof(joinPredicate));
            }

            return source.Provider.CreateQuery<R>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, IQueryable<TJoin>, Expression<Func<T, TJoin, R>>, Expression<Func<R, bool>>, IQueryable<R>>(FullOuterJoin).Method,
                    source.Expression, otherSource.Expression, Expression.Quote(newSourceFactory), Expression.Quote(joinPredicate)));
        }

        public static IQueryable<T> FullOuterJoin<T>(this IQueryable<T> source, Expression<Func<T, T, bool>> joinPredicate)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (joinPredicate is null)
            {
                throw new ArgumentNullException(nameof(joinPredicate));
            }

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, T, bool>>, IQueryable<T>>(FullOuterJoin).Method,
                    source.Expression, Expression.Quote(joinPredicate)));
        }

        public static int Update<T>(this IQueryable<T> query, Expression<Func<T, T>> tableUpdateFields, Expression<Func<T, bool>> predicate)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query));
            if (tableUpdateFields is null)
                throw new ArgumentNullException(nameof(tableUpdateFields));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));
            var q  = query.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, T>>, Expression<Func<T, bool>>, int>(Update).Method,
                    query.Expression, Expression.Quote(tableUpdateFields), Expression.Quote(predicate)));
            return query.Provider.Execute<int>(q.Expression);
        }

        public static int Update<T, R>(this IQueryable<T> query, Expression<Func<T, R>> tableSelection, Expression<Func<T, R>> tableUpdateFields, Expression<Func<T, bool>> predicate)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query));
            if (tableSelection is null)
                throw new ArgumentNullException(nameof(tableSelection));
            if (tableUpdateFields is null)
                throw new ArgumentNullException(nameof(tableUpdateFields));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));
            var q = query.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, R>>, Expression<Func<T, R>>, Expression<Func<T, bool>>, int>(Update).Method,
                    query.Expression, Expression.Quote(tableSelection), Expression.Quote(tableUpdateFields), Expression.Quote(predicate)));
            return query.Provider.Execute<int>(q.Expression);
        }

        public static int Delete<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));
            var q = query.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, bool>>, int>(Delete).Method,
                    query.Expression, Expression.Quote(predicate)));
            return query.Provider.Execute<int>(q.Expression);
        }

        public static int Delete<T, R>(this IQueryable<T> query, Expression<Func<T, R>> tableSelection, Expression<Func<T, bool>> predicate)
        {
            if (query is null)
                throw new ArgumentNullException(nameof(query));
            if (tableSelection is null)
                throw new ArgumentNullException(nameof(tableSelection));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));
            var q = query.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    new Func<IQueryable<T>, Expression<Func<T, R>>, Expression<Func<T, bool>>, int>(Delete).Method,
                    query.Expression, Expression.Quote(tableSelection), Expression.Quote(predicate)));
            return query.Provider.Execute<int>(q.Expression);
        }
    }
}

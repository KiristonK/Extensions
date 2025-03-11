using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Extensions.EF;

public static class IQueryableExtensions {
    /// <summary>
    /// Constructs a "LEFT JOIN" operator in the final SQL
    /// </summary>
    /// <typeparam name="TOuter"></typeparam>
    /// <typeparam name="TInner"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="outer">Collection to be joined to</param>
    /// <param name="inner">Collection to be joined with</param>
    /// <param name="outerKeySelector">Key selector for the <paramref name="outer"/></param>
    /// <param name="innerKeySelector">Key selector for the <paramref name="inner"/></param>
    /// <param name="resultSelector">Result selector expression</param>
    /// <returns></returns>
    public static IQueryable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
    {
        return outer.GroupJoin(inner, outerKeySelector, innerKeySelector, (group, list) => new { group, list })
            .FinalizeLeftJoin(group => group.list.DefaultIfEmpty(), (group, item) => new { group.group, item }, resultSelector); 
    }

    /// <summary>
    /// Performs the "SelectMany" part of LINQ left join and projects result with provided <paramref name="resultSelector"/>
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TCollection"></typeparam>
    /// <typeparam name="TSelectMany"></typeparam>
    /// <typeparam name="TOuter"></typeparam>
    /// <typeparam name="TInner"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="source"></param>
    /// <param name="collectionSelector"></param>
    /// <param name="joinResultSelector"></param>
    /// <param name="resultSelector"></param>
    /// <returns></returns>
    private static IQueryable<TResult> FinalizeLeftJoin<TSource, TCollection, TSelectMany, TOuter, TInner, TResult>(this IQueryable<TSource> source, 
        Expression<Func<TSource, IEnumerable<TCollection>>> collectionSelector,
        Expression<Func<TSource, TCollection, TSelectMany>> joinResultSelector,
        Expression<Func<TOuter, TInner, TResult>> resultSelector)
    {
        var leftJoin = source.SelectMany(collectionSelector, joinResultSelector);

        var param = Expression.Parameter(leftJoin.ElementType, "j");
        var invokeResultSelectorExp = Expression.Invoke(resultSelector, Expression.Property(param, "group"), Expression.Property(param, "item"));

        // Create a selector expression that invokes provided resultSelector
        var finalSelector = Expression.Lambda<Func<TSelectMany, TResult>>(
            invokeResultSelectorExp,
            param);

        return leftJoin.Select(finalSelector);
    }

}

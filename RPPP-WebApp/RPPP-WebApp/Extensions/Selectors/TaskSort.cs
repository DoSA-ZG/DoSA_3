using RPPP_WebApp.Model;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors;

public static class TaskSort
{
    public static IQueryable<Tasks> ApplySort(this IQueryable<Tasks> query, int sort, bool ascending)
    {
        Expression<Func<Tasks, object>> orderSelector = sort switch
        {
            1 => d => d.IdTask,
            2 => d => d.Task1,
            3 => d => d.IdTaskStatus,
            4 => d => d.IdPerson,
            _ => null
        };

        if (orderSelector != null)
        {
            query = ascending ?
                   query.OrderBy(orderSelector) :
                   query.OrderByDescending(orderSelector);
        }

        return query;
    }
}
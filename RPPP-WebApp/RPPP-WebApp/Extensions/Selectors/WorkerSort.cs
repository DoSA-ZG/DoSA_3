using RPPP_WebApp.Model;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors;

public static class WorkerSort
{
  public static IQueryable<Workers> ApplySort(this IQueryable<Workers> query, int sort, bool ascending)
  {
    Expression<Func<Workers, object>> orderSelector = sort switch
    {
        1 => d => d.IdPerson,
        2 => d => d.IdPersonNavigation.Name, 
        3 => d => d.IdWorkerType,
        4 => d => d.Salary,
        
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

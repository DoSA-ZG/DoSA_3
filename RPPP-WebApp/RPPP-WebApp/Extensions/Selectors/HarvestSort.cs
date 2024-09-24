using RPPP_WebApp.Model;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors;

public static class HarvestSort
{
  public static IQueryable<Harvest> ApplySort(this IQueryable<Harvest> query, int sort, bool ascending)
  {
    Expression<Func<Harvest, object>> orderSelector = sort switch
    {
        1 => d => d.IdHarvest,
        2 => d => d.IdCrop,
        3 => d => d.Quantity,
        4 => d => d.FromDate,
        5 => d => d.ToDate,
        6 => d => d.IdPerson,
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

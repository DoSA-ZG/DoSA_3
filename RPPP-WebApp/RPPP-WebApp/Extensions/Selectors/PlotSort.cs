using RPPP_WebApp.Model;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors;

public static class PlotSort
{
  public static IQueryable<Plot> ApplySort(this IQueryable<Plot> query, int sort, bool ascending)
  {
    Expression<Func<Plot, object>> orderSelector = sort switch
    {
        1 => d => d.IdPlot,
        2 => d => d.IdPerson,
        3 => d => d.IdCrop,
        4 => d => d.CommonName,
        5 => d => d.IdSoilQuality,
        6 => d => d.IdSoilCategory,
        7 => d => d.IdInfrastructure,
        8 => d => d.Size,
        9 => d => d.Gpslocation,
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

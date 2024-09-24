using RPPP_WebApp.Model;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors;

public static class CropSort
{
  public static IQueryable<Crop> ApplySort(this IQueryable<Crop> query, int sort, bool ascending)
  {
    Expression<Func<Crop, object>> orderSelector = sort switch
    {
        1 => d => d.IdCrop,
        2 => d => d.IdSpecies,
        3 => d => d.IdTask,
        4 => d => d.IdStatus,
        5 => d => d.IdPerson,
        6 => d => d.PlantingDate,
        7 => d => d.Quantity,
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

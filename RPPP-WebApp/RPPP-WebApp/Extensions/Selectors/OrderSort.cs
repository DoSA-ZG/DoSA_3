using RPPP_WebApp.Model;
using System.Linq.Expressions;

namespace RPPP_WebApp.Extensions.Selectors;

public static class OrderSort
{
  public static IQueryable<Order> ApplySort(this IQueryable<Order> query, int sort, bool ascending)
  {
    Expression<Func<Order, object>> orderSelector = sort switch
    {
        1 => d => d.IdOrder,
        2 => d => d.IdHarvest,
        3 => d => d.IdPerson,
        4 => d => d.Quantity,
        5 => d => d.Price,
        6 => d => d.DateOfOrder,
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

using RPPP_WebApp.Model;
using System.Collections.Generic;

namespace RPPP_WebApp.ViewModels
{
  public class OrderViewModel
  {
    public IEnumerable<Order2ViewModel> Order { get; set; }
    public PagingInfo PagingInfo { get; set; }
  }
}

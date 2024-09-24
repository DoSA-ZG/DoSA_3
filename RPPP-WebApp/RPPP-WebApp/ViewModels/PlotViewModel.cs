using RPPP_WebApp.Model;
using System.Collections.Generic;

namespace RPPP_WebApp.ViewModels
{
  public class PlotViewModel
  {
    public IEnumerable<Plot2ViewModel> Plot { get; set; }
    public PagingInfo PagingInfo { get; set; }
  }
}

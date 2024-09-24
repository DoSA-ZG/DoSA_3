using RPPP_WebApp.Model;
using System.Collections.Generic;

namespace RPPP_WebApp.ViewModels
{
  public class HarvestViewModel
  {
    public IEnumerable<Harvest2ViewModel> Harvest { get; set; }
    public PagingInfo PagingInfo { get; set; }
  }
}

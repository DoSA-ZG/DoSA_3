using RPPP_WebApp.Model;
using System.Collections.Generic;

namespace RPPP_WebApp.ViewModels
{
  public class WorkerViewModel
  {
    public IEnumerable<Worker2ViewModel> Worker { get; set; }
    public PagingInfo PagingInfo { get; set; }
  }
}

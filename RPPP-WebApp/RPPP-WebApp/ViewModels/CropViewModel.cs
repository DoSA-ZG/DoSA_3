using RPPP_WebApp.Model;
using System.Collections.Generic;

namespace RPPP_WebApp.ViewModels
{
  public class CropViewModel
  {
    public IEnumerable<Crop2ViewModel> Crop { get; set; }
    public PagingInfo PagingInfo { get; set; }
  }
}

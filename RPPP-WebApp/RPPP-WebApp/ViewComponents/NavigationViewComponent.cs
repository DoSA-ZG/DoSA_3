using Microsoft.AspNetCore.Mvc;

namespace RPPP_WebApp.ViewComponents
{
  public class NavigationViewComponent : ViewComponent
  {
    public IViewComponentResult Invoke()
    {
      ViewBag.Controller = RouteData?.Values["controller"];
      return View();
    }
  }
}
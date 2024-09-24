using RPPP_WebApp.Model;


namespace RPPP_WebApp.ViewModels;


public class Harvest2ViewModel
{
    public int IdHarvest { get; set; }
    public int IdCrop { get; set; }
    public double Quantity { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int IdPerson { get; set; }
    public virtual Workers IdPersonNavigation { get; set; }
    public string NamePerson { get; set; }

    public IEnumerable<Order2ViewModel> ItemsH { get; set; }

    public Harvest2ViewModel()
    {
        this.ItemsH = new List<Order2ViewModel>();
    }

}

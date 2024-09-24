using RPPP_WebApp.Model;


namespace RPPP_WebApp.ViewModels;


public class Crop2ViewModel
{
    public int IdCrop { get; set; }

    public int IdSpecies { get; set; }

    public int IdTask { get; set; }

    public int IdStatus { get; set; }

    public int IdPerson { get; set; }
    public DateTime PlantingDate { get; set; }
    public int Quantity { get; set; }
    public string NamePerson { get; set; }
    public string NameSpecies { get; set; }
    public string NameTask { get; set; }
    public string NameStatus { get; set; }

    public IEnumerable<Plot2ViewModel> ItemsC { get; set; }

    public Crop2ViewModel()
    {
        this.ItemsC = new List<Plot2ViewModel>();
    }
}

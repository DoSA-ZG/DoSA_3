using RPPP_WebApp.Model;
using System.ComponentModel.DataAnnotations.Schema;


namespace RPPP_WebApp.ViewModels;


public class Worker2ViewModel
{
    public int IdPerson { get; set; }

    public int IdWorkerType { get; set; }

    public double Salary { get; set; }

    public virtual Person IdPersonNavigation { get; set; }

    public virtual WorkerType IdWorkerTypeNavigation { get; set; }

    public string NamePerson { get; set; }
    public string NameWorkerType { get; set;}

    public PagingInfo PagingInfo { get; set; }

    public IEnumerable<Task2ViewModel> ItemsW { get; set; }

    public Worker2ViewModel()
    {
        this.ItemsW = new List<Task2ViewModel>();
    }

}

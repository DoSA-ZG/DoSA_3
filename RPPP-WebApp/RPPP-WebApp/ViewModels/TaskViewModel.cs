using RPPP_WebApp.Model;
using System.Collections.Generic;

namespace RPPP_WebApp.ViewModels
{
    public class TaskViewModel
    {
        public IEnumerable<Task2ViewModel> Task { get; set; }
        public PagingInfo PagingInfo { get; set; }
    }
}

using RPPP_WebApp.Model;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;


namespace RPPP_WebApp.ViewModels
{
    public class Task2ViewModel
    {

        
        public int IdTask { get; set; }

        public string Task1 { get; set; }

        public int IdTaskStatus { get; set; }

        public int IdPerson { get; set; }

        public virtual Person IdPersonNavigation { get; set; }

        public string NamePerson { get; set; }

        public string Status { get; set; }
    }
}

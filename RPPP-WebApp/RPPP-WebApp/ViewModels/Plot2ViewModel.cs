using RPPP_WebApp.Model;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;


namespace RPPP_WebApp.ViewModels
{
    public class Plot2ViewModel
    {


        public int IdPlot { get; set; }

        public int IdPerson { get; set; }

        public int IdCrop { get; set; }

        public string CommonName { get; set; }

        public int IdSoilQuality { get; set; }

        public int IdSoilCategory { get; set; }

        public int IdInfrastructure { get; set; }

        public double Size { get; set; }

        public string Gpslocation { get; set; }

        public string NamePerson { get; set; }
        public string NameSoilQuality { get; set; }
        public string NameSoilCategory { get;set; }
        public string NameInfrastructure { get; set; }
    }
}

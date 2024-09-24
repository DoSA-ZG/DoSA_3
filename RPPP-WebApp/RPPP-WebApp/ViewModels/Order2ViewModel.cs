using RPPP_WebApp.Model;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;


namespace RPPP_WebApp.ViewModels
{
    public class Order2ViewModel
    {
        public int IdOrder { get; set; }

        public int IdHarvest { get; set; }

        public int IdPerson { get; set; }

        public double Quantity { get; set; }

        public decimal Price { get; set; }

        public DateTime DateOfOrder { get; set; }

        public string NamePerson { get; set; }
    }
}

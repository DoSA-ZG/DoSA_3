﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Model;

public partial class Harvest
{
    public int IdHarvest { get; set; }

    public int IdCrop { get; set; }

    public double Quantity { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public int IdPerson { get; set; }

    public virtual Crop IdCropNavigation { get; set; }

    public virtual Workers IdPersonNavigation { get; set; }

    public virtual ICollection<Order> Order { get; set; } = new List<Order>();
}
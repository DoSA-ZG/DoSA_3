﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Model;

public partial class PlantingSeason
{
    public int IdSeason { get; set; }

    public string Season { get; set; }

    public virtual ICollection<Variant> Variant { get; set; } = new List<Variant>();
}
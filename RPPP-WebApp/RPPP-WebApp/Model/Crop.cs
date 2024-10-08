﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Model;

public partial class Crop
{
    public int IdCrop { get; set; }

    public int IdSpecies { get; set; }

    public int IdTask { get; set; }

    public int IdStatus { get; set; }

    public int IdPerson { get; set; }

    public DateTime PlantingDate { get; set; }

    public int Quantity { get; set; }

    public virtual ICollection<Harvest> Harvest { get; set; } = new List<Harvest>();

    public virtual Person IdPersonNavigation { get; set; }

    public virtual Species IdSpeciesNavigation { get; set; }

    public virtual Status IdStatusNavigation { get; set; }

    public virtual Tasks IdTaskNavigation { get; set; }

    public virtual ICollection<Plot> Plot { get; set; } = new List<Plot>();
}
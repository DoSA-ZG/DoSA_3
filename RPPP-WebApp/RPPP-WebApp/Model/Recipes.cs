﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Model;

public partial class Recipes
{
    public int IdRecipes { get; set; }

    public int IdVariant { get; set; }

    public string Name { get; set; }

    public virtual Variant IdVariantNavigation { get; set; }
}
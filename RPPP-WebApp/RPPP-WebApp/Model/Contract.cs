﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Model;

public partial class Contract
{
    public int IdContract { get; set; }

    public int IdPerson { get; set; }

    public int IdTypeContract { get; set; }

    public virtual Person IdPersonNavigation { get; set; }

    public virtual TypeContract IdTypeContractNavigation { get; set; }
}
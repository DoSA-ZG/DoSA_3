﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Model;

public partial class Passport
{
    public int IdPassport { get; set; }

    public int IdVariant { get; set; }

    public string Origin { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime ExpiringDate { get; set; }

    public virtual Variant IdVariantNavigation { get; set; }
}
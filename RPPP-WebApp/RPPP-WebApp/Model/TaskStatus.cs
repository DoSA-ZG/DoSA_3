﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace RPPP_WebApp.Model;

public partial class TaskStatus
{
    public int IdTaskStatus { get; set; }

    public string Status { get; set; }

    public virtual ICollection<Tasks> Task { get; set; } = new List<Tasks>();
}
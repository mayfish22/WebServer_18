﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace WebServer.Models.WebServerDB
{
    public partial class Connection
    {
        public string ID { get; set; }
        public string UserID { get; set; }
        public string IP { get; set; }
        public string ConnectDT { get; set; }

        public virtual User User { get; set; }
    }
}
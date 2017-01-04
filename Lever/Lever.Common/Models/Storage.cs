﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lever.Common.Models
{
    public class Storage
    {
        public string StorageType { get; set; }
        public string Name { get; set; }
        public List<Environment> EnvironmentOverrides { get; set; }
    }
}

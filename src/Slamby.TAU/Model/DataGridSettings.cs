using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Slamby.TAU.Model
{
    public class DataGridSettings
    {
        public List<string> Columns { get; set; }

        public SortDescription? SortDescription { get; set; }
    }
}

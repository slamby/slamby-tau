using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;

namespace Slamby.TAU.Model
{
    public class CsvImportSettings
    {
        public string Delimiter { get; set; }

        public bool Force { get; set; }

        public CsvReader CsvReader { get; set; }
        
    }
}

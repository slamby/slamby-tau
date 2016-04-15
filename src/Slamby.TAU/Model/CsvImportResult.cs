using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Newtonsoft.Json.Linq;

namespace Slamby.TAU.Model
{
    public class CsvImportResult
    {
        public CsvReader CsvReader { get; set; }

        public List<JToken> Tokens { get; set; }=new List<JToken>();

        public List<int> InvalidRows { get; set; }=new List<int>();

        public bool CanContinue { get; set; } = true;
        

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Slamby.TAU.Model
{
    public class JContent
    {
        public JContent(object obj)
        {
            JString = JsonConvert.SerializeObject(obj, Formatting.Indented);
        }

        public string JString { get; set; }

        public JToken GetJToken()
        {
            return JToken.Parse(JString);
        }
    }
}

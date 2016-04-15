using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slamby.TAU.Helper
{
    public static class JsonProcesser
    {
        public static async Task<List<JToken>> GetTokens(string fileName)
        {
            //check utf
            using (var reader = File.OpenText(fileName))
            {
                var data = await reader.ReadToEndAsync();
                var exceptions = new List<Exception>();
                //the first array type node will be the source
                JToken firstArray = null;
                try
                {
                    var _jsonObject = JObject.Parse(data);
                    firstArray =
                        _jsonObject.Descendants()
                            .OfType<JProperty>()
                            .First(p => p.Value.Type == JTokenType.Array && p.Value.Type != JTokenType.Object);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
                if (firstArray == null)
                {
                    try
                    {
                        var _jsonArray = JArray.Parse(data);
                        firstArray = _jsonArray;
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }


                if (firstArray == null)
                {
                    if (exceptions.Any())
                    {
                        throw new Exception($"Bad Json format. Possible errors: {Environment.NewLine}{string.Join(Environment.NewLine, exceptions.Select(ex => ex.Message))}");
                    }
                    else
                    {
                        return new List<JToken>();
                    }
                }

                //rows
                if (firstArray is JArray)
                {
                    return firstArray.ToList();
                }
                else
                {
                    var r1 = firstArray.Children().ToList().First();
                    //row[i]
                    return r1.Children().ToList();
                }
            }
        }
    }
}

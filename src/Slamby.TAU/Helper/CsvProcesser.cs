using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Newtonsoft.Json.Linq;
using Slamby.TAU.Model;

namespace Slamby.TAU.Helper
{
    public static class CsvProcesser
    {
        public static async Task<CsvImportResult> GetTokens(CsvImportSettings importSettings, bool unlimied = false)
        {
            return await Task.Run(() =>
            {
                var result = new CsvImportResult();

                while (result.CanContinue && (unlimied || result.Tokens.Count + result.InvalidRows.Count < GlobalStore.SelectedEndpoint.BulkSize))
                {
                    try
                    {
                        result.CanContinue = importSettings.CsvReader.Read();
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    if (!result.CanContinue)
                        continue;

                    var tokenString = "";
                    try
                    {
                        tokenString = "{" + string.Join(",", importSettings.CsvReader.FieldHeaders.Select(h => $"\"{h}\":\"{(importSettings.CsvReader[h] == "NULL" ? "" : importSettings.CsvReader[h])}\"")) + "}";
                        result.Tokens.Add(JToken.Parse(tokenString));
                    }
                    catch (Exception)
                    {
                        result.InvalidRows.Add(((CsvParser)importSettings.CsvReader.Parser).RawRow);
                        if (!importSettings.Force)
                            result.CanContinue = false;
                    }
                }
                return result;
            });

        }

        public static CsvReader GetCsvReader(Stream fileStream, CsvImportSettings settings)
        {
            var streamReader = new StreamReader(fileStream);
            var csvReader = new CsvReader(streamReader);
            csvReader.Configuration.Encoding = Encoding.UTF8;
            csvReader.Configuration.HasHeaderRecord = true;
            csvReader.Configuration.DetectColumnCountChanges = true;
            csvReader.Configuration.Delimiter = settings.Delimiter;
            return csvReader;
        }
        

        public static async Task<List<int>> GetInvalidRowNumbers(string fileName, string delimiter)
        {
            return await Task.Run(() =>
            {
                var stream = new FileStream(fileName, FileMode.Open);
                stream.Position = 0;
                var streamReader = new StreamReader(stream);
                var csvReader = new CsvReader(streamReader);
                csvReader.Configuration.Encoding = Encoding.UTF8;
                csvReader.Configuration.HasHeaderRecord = true;
                csvReader.Configuration.DetectColumnCountChanges = true;
                csvReader.Configuration.Delimiter = delimiter;

                var invalidRows = new List<int>();
                var canContinue = true;
                while (canContinue)
                {
                    try
                    {
                        canContinue = csvReader.Read();
                        if (csvReader.CurrentRecord != null)
                        {
                            var tokenString = "{" +
                                              string.Join(",",
                                                  csvReader.FieldHeaders.Select(h => $"\"{h}\":\"{csvReader[h]}\"")) +
                                              "}";
                            JToken.Parse(tokenString);
                        }
                    }
                    catch (Exception)
                    {
                        invalidRows.Add(((CsvParser)csvReader.Parser).RawRow);
                    }

                }

                streamReader.Close();
                return invalidRows;
            });
        }
    }
}

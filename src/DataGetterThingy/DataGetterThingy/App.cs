using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
using DataGetterThingy.Models;
using Microsoft.Extensions.Logging;

namespace DataGetterThingy
{
    internal class App
    {
        private readonly ILogger<App> _logger;
        private readonly AppSettings _appSettings;
        private readonly HttpClient _httpClient;

        public App(ILogger<App> logger, AppSettings appSettings, IHttpClientFactory clientFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            if (clientFactory == null) throw new ArgumentNullException(nameof(clientFactory));

            _httpClient = clientFactory.CreateClient(nameof(App));
        }

        public async Task Run(string inputFilePath)
        {
            _logger.LogInformation("Running processing...");

            if (!File.Exists(inputFilePath))
            {
                throw new FileNotFoundException("File does not exist.", inputFilePath);
            }
            
            string outputFilePath = GetOutputFilePath(inputFilePath);

            _logger.LogInformation($"Output file: {outputFilePath}");

            FileStream outputFileStream = File.Exists(outputFilePath) 
                ? File.Open(outputFilePath, FileMode.Truncate) 
                : File.Create(outputFilePath);

            int skipped = 0;
            int processed = 0;

            using (var writer = new StreamWriter(outputFileStream))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                foreach (InputCsvRow row in ParseCsv(inputFilePath))
                {
                    if (string.IsNullOrEmpty(row.Uprn))
                    {
                        if (_appSettings.ShowUprnWarning) _logger.LogWarning($"ReferenceNumber {row.ReferenceNumber} does not have a UPRN.");
                        skipped++;
                        continue;
                    }

                    try
                    {
                        string data = await GetWebData(row);
                        csv.WriteRecord(new OutputCsvRow { ReferenceNumber = row.ReferenceNumber, Uprn = row.Uprn, Data = data });
                        await csv.NextRecordAsync();
                        processed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error while processing UPRN {row.Uprn}");
                    }

                    ShowProgression(row);
                }
            }

            _logger.LogInformation($"Processing finished, skipped: {skipped}, processed: {processed}");
        }



        private IEnumerable<InputCsvRow> ParseCsv(string inputFilePath)
        {
            var reader = new StreamReader(inputFilePath);
            var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            csv.Configuration.HasHeaderRecord = true;
            return csv.GetRecords<InputCsvRow>();
        }

        private async Task<string> GetWebData(InputCsvRow row)
        {
            string url = string.Format(_appSettings.PreviousMeasuresUrlFormat, row.Uprn);

            return await _httpClient.GetStringAsync(url);
        }

        private void ShowProgression(InputCsvRow row)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(row.ReferenceNumber);
        }

        private string GetOutputFilePath(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string directory = Path.GetDirectoryName(filePath);
            string newFileName = $"{fileName}-updated.csv";
            return Path.Combine(directory, newFileName);
        }
    }
}

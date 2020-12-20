using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using CsvHelper;
using DataGetterThingy.Data;
using DataGetterThingy.Inputs;
using Microsoft.Extensions.Logging;

namespace DataGetterThingy
{
    internal class App
    {
        private readonly ILogger<App> _logger;
        private readonly AppSettings _appSettings;
        private readonly IInputParser _parser;
        private readonly IDataGetter _dataGetter;

        public App(ILogger<App> logger, AppSettings appSettings, IInputParser parser, IDataGetter dataGetter)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _dataGetter = dataGetter ?? throw new ArgumentNullException(nameof(dataGetter));
        }

        public async Task Run(string inputFilePath)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            _logger.LogInformation("Running processing...");

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
                var csvData = _parser.Parse(inputFilePath);
                int i = 0;

                foreach (string[] row in csvData)
                {
                    string rowData = row[_appSettings.UrlColumn];
                    
                    if (string.IsNullOrEmpty(rowData))
                    {
                        if (_appSettings.ShowNoDataWarning) _logger.LogWarning($"Row {i} column {_appSettings.UrlColumn} has no data.");
                        skipped++;
                        continue;
                    }

                    try
                    {
                        string data = await _dataGetter.GetData(rowData);
                        
                        foreach (var o in row)
                        {
                            csv.WriteField(o);
                        }
                        csv.WriteField(data);

                        await csv.NextRecordAsync();
                        processed++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error while processing row {i}, data {row[_appSettings.UrlColumn]}");
                    }

                    if (i % 100 == 0)
                    {
                        _logger.LogInformation($"Processed {i} rows");
                    }

                    i++;
                }
            }

            sw.Stop();

            _logger.LogInformation($"Processing finished, skipped: {skipped}, processed: {processed}, took: {sw.Elapsed.TotalMinutes} mins or {sw.Elapsed.TotalSeconds} seconds");
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

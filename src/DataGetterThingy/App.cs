﻿using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CsvHelper;
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
            Stopwatch sw = new Stopwatch();
            sw.Start();

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
                var dataTable = ParseCsv(inputFilePath);

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    DataRow row = dataTable.Rows[i];
            
                    if (string.IsNullOrEmpty(row[_appSettings.UrlColumn].ToString()))
                    {
                        if (_appSettings.ShowNoDataWarning) _logger.LogWarning($"Row {i} column {_appSettings.UrlColumn} has no data.");
                        skipped++;
                        continue;
                    }

                    try
                    {
                        string data = await GetWebData(row[_appSettings.UrlColumn].ToString());
                        
                        foreach (var o in row.ItemArray)
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
                }
            }

            sw.Stop();

            _logger.LogInformation($"Processing finished, skipped: {skipped}, processed: {processed}, took: {sw.Elapsed.TotalMinutes} mins or {sw.Elapsed.TotalSeconds} seconds");
        }



        private DataTable ParseCsv(string inputFilePath)
        {
            var dataTable = new DataTable();

            using (var reader = new StreamReader(inputFilePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Configuration.HasHeaderRecord = _appSettings.HasHeaderRecord;

                using (var dr = new CsvDataReader(csv))
                {
                    dataTable.Load(dr);
                }
            }

            return dataTable;
        }

        private async Task<string> GetWebData(string urlData)
        {
            string url = string.Format(_appSettings.PreviousMeasuresUrlFormat, urlData);

            return await _httpClient.GetStringAsync(url);
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

using System;
using Microsoft.Extensions.Configuration;

namespace DataGetterThingy
{
    internal class AppSettings
    {
        private readonly IConfiguration _configuration;

        public AppSettings(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string PreviousMeasuresUrlFormat => _configuration["PreviousMeasuresUrlFormat"];
        public bool ShowNoDataWarning => _configuration.GetValue<bool>("ShowNoDataWarning");
        public bool HasHeaderRecord => _configuration.GetValue<bool>("HasHeaderRecord", false);
        public int UrlColumn => _configuration.GetValue<int>("UrlColumn", 0);
    }
}
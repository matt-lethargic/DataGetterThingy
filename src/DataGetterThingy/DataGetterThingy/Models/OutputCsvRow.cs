using CsvHelper.Configuration.Attributes;

namespace DataGetterThingy.Models
{
    public class OutputCsvRow
    {
        [Name("referencenumber")]
        public string ReferenceNumber { get; set; }
        [Name("uprn")]
        public string Uprn { get; set; }
        [Name("data")]
        public string Data { get; set; }
    }
}

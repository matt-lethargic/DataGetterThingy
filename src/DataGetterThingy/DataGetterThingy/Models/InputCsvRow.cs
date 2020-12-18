
using CsvHelper.Configuration.Attributes;

namespace DataGetterThingy.Models
{
    public class InputCsvRow
    {
        [Name("referencenumber")]
        public string ReferenceNumber { get; set; }
        [Name("uprn")]
        public string Uprn { get; set; }
    }
}

using System.Threading.Tasks;

namespace DataGetterThingy.Data
{
    public interface IDataGetter
    {
        Task<string> GetData(string urlData);
    }
}

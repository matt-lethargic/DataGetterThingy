using System.Collections.Generic;

namespace DataGetterThingy.Inputs
{
    public interface IInputParser
    {
        IEnumerable<string[]> Parse(string inputFilePath);
    }
}
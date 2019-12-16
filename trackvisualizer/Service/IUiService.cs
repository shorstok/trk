using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace trackvisualizer.Service
{
    public interface IUiService
    {
        Task NofityError(string text);
        Task<string> Ask(string what, string initialValue);
        Task<string> ChooseAsync(IEnumerable<Tuple<string, string>> choices, string title);
    }
}
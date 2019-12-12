using System.Threading.Tasks;

namespace trackvisualizer.Service
{
    public interface IUiService
    {
        Task NofityError(string text);
        Task<string> Ask(string what, string initialValue);
    }
}
using System.Threading.Tasks;
using trackvisualizer.Vm;

namespace trackvisualizer.Service.ReportExporters
{
    public interface ITrackReportExporter
    {
        string Id { get; }
        string Description { get; }
        Task<bool> Export(TrackReportVm source);
    }
}

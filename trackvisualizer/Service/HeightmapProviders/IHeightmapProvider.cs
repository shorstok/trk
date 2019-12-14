using System;
using System.Threading;
using System.Threading.Tasks;

namespace trackvisualizer.Service.HeightmapProviders
{
    public interface IHeightmapProvider
    {
        Guid InternalId { get; }

        bool IsAvailable { get; }

        int Priority { get; }

        string Description { get; }
        
        int MaxConcurrentInstances { get;}

        object Settings { get; }

        Task<bool> DownloadHeightmap(string missingSrtmName, Action<double, string> reportProgressAsync,
            CancellationToken token);
    }
}

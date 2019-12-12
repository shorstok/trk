using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace trackvisualizer.Service
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

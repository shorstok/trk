using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace trackvisualizer.Service
{
    public interface IUiLoggingService
    {
        void ResetLog();        
        void Log(string text, bool persist = false);
        void LogError(string errorText);
    }
}

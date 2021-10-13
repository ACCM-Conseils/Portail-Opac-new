using PortailsOpacBase.Common.Enumerations;
using PortailsOpacBase.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortailsOpacBase.Common.Interfaces
{
    public interface ILogHelper
    {
        void Log<T>(LoggerCategory category, string message, Exception innerException = null, params object[] logValues) where T : class;
        void LogInformation<T>(string message, params object[] logValues) where T : class;
        void LogWarning<T>(string message, Exception innerException = null, params object[] logValues) where T : class;
        void LogError<T>(string message, Exception innerException = null, params object[] logValues) where T : class;
        void LogFatal<T>(string message, Exception innerException = null, params object[] logValues) where T : class;
        void LogBaseException(BaseException exception, params object[] logValues);
        void LogException(Exception exception, params object[] logValues);
    }
}

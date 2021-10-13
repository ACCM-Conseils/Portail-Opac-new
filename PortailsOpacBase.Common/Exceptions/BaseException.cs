using PortailsOpacBase.Common.Enumerations;
using PortailsOpacBase.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortailsOpacBase.Common.Exceptions
{
    public abstract class BaseException : Exception
    {
        public static ILogHelper Logger;

        #region Ctors

        public BaseException(LoggerCategory category, string message, Exception innerException, params object[] errorValues) : base(message, innerException)
        {
            this.Category = category;

            if (null == BaseException.Logger)
                return;

            Logger.LogBaseException(this, errorValues);
        }

        public BaseException(string message, Exception innerException, params object[] errorValues) : this(LoggerCategory.Error, message, innerException, errorValues)
        {
        }

        public BaseException(string message, params object[] exceptionValues) : this(message, null, exceptionValues)
        {
        }

        #endregion // Ctors

        #region Properties

        public LoggerCategory Category { get; protected set; }

        #endregion // Properties
    }
}

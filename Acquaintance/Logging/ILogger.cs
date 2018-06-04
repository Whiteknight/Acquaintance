using System;

namespace Acquaintance.Logging
{
    public interface ILogger
    {
        /// <summary>
        /// A message which has importance for debugging, but which likely shouldn't appear in production logs
        /// </summary>
        /// <param name="fmt"></param>
        /// <param name="args"></param>
        void Debug(string fmt, params object[] args);

        /// <summary>
        /// Information about the operation of the system, without indicating a problem
        /// </summary>
        /// <param name="fmt"></param>
        /// <param name="args"></param>
        void Info(string fmt, params object[] args);

        /// <summary>
        /// A warning that things are not operating as expected, though the system does not treat it as an error
        /// </summary>
        /// <param name="fmt"></param>
        /// <param name="args"></param>
        void Warn(string fmt, params object[] args);

        /// <summary>
        /// An error which may cause an operation to fail or other functionality to not work as expected
        /// </summary>
        /// <param name="fmt"></param>
        /// <param name="args"></param>
        void Error(string fmt, params object[] args);

        /// <summary>
        /// An error which may cause an operation to fail or other functionality to not work as expected.
        /// Includes exception information.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="fmt"></param>
        /// <param name="args"></param>
        void Error(Exception e, string fmt, params object[] args);
    }
}

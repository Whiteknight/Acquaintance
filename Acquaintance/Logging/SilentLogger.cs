using System;

namespace Acquaintance.Logging
{
    public class SilentLogger : ILogger
    {
        public void Debug(string fmt, params object[] args)
        {
        }

        public void Info(string fmt, params object[] args)
        {
        }

        public void Warn(string fmt, params object[] args)
        {
        }

        public void Warn(Exception e, string fmt, params object[] args)
        {
        }

        public void Error(string fmt, params object[] args)
        {
        }

        public void Error(Exception e, string fmt, params object[] args)
        {
        }
    }
}
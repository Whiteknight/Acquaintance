using System;

namespace Acquaintance.Logging
{
    public class DelegateLogger : ILogger
    {
        private readonly Action<string> _logger;

        public DelegateLogger(Action<string> logger)
        {
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));
            _logger = logger;
        }

        public void Debug(string fmt, params object[] args)
        {
            _logger(Build("DEBUG", fmt, args));
        }

        public void Info(string fmt, params object[] args)
        {
            _logger(Build("INFO", fmt, args));
        }

        public void Warn(string fmt, params object[] args)
        {
            _logger(Build("WARN", fmt, args));
        }

        public void Error(string fmt, params object[] args)
        {
            _logger(Build("ERROR", fmt, args));
        }

        private string Build(string severity, string fmt, object[] args)
        {
            var msg = string.Format(fmt ?? string.Empty, args);
            return severity + "| " + msg;
        }
    }
}
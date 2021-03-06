﻿using System;
using System.Text;

namespace Acquaintance.Logging
{
    /// <summary>
    /// Format messages and log them to a simple callback delegate.
    /// This class IS NOT THREAD SAFE. It is expected that the callback delegate
    /// implements its own thread-safety mechanisms
    /// </summary>
    public class DelegateLogger : ILogger
    {
        private readonly Action<string> _logger;

        public DelegateLogger(Action<string> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        public void Warn(Exception e, string fmt, params object[] args)
        {
            var builder = new StringBuilder();
            builder.Append("WARN | ");
            var msg = string.Format(fmt ?? string.Empty, args);
            builder.AppendLine(msg);
            builder.AppendLine(e.Message);
            builder.AppendLine(e.StackTrace);
            _logger(builder.ToString());
        }

        public void Error(string fmt, params object[] args)
        {
            _logger(Build("ERROR", fmt, args));
        }

        public void Error(Exception e, string fmt, params object[] args)
        {   
            var builder = new StringBuilder();
            builder.Append("ERROR | ");
            var msg = string.Format(fmt ?? string.Empty, args);
            builder.AppendLine(msg);
            builder.AppendLine(e.Message);
            builder.AppendLine(e.StackTrace);
            _logger(builder.ToString());
        }

        private string Build(string severity, string fmt, object[] args)
        {
            var msg = string.Format(fmt ?? string.Empty, args);
            return severity + " | " + msg;
        }
    }
}
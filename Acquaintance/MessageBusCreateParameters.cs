﻿using System;
using Acquaintance.Logging;

namespace Acquaintance
{
    public class MessageBusCreateParameters
    {
        public MessageBusCreateParameters()
        {
            NumberOfWorkers = 2;
            MaximumQueuedMessages = 1000;
        }

        public bool AllowWildcards { get; set; }
        public int NumberOfWorkers { get; set; }
        public int MaximumQueuedMessages { get; set; }
        public ILogger Logger { get; set; }
        public Guid? Id { get; set; }

        public static MessageBusCreateParameters Default => new MessageBusCreateParameters();

        public MessageBusCreateParameters WithWildcards()
        {
            AllowWildcards = true;
            return this;
        }

        public ILogger GetLogger()
        {
            if (Logger != null)
                return Logger;
#if DEBUG
            return new DelegateLogger(s => System.Diagnostics.Debug.WriteLine(s));
#else
            return new SilentLogger();
#endif
        }
    }
}

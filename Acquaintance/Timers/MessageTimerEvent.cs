﻿namespace Acquaintance.Timers
{
    public class MessageTimerEvent
    {
        public const string EventName = "Tick";

        public MessageTimerEvent(string timerName, long id)
        {
            Id = id;
            TimerName = timerName;
        }

        public long Id { get; private set; }
        public string TimerName { get; private set; }
    }
}
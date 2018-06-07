namespace Acquaintance.Timers
{
    public class MessageTimerEvent
    {
        public MessageTimerEvent(string timerName, long id)
        {
            Id = id;
            TimerName = timerName;
        }

        public long Id { get; }
        public string TimerName { get; }
    }
}
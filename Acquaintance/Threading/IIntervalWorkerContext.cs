namespace Acquaintance.Threading
{
    public interface IIntervalWorkerContext
    {
        void Complete();
        bool IsComplete { get; }
        int IterationDelayMs { get; set; }
    }

    public class IntervalWorkerContext : IIntervalWorkerContext
    {
        public void Complete()
        {
            IsComplete = true;
        }

        public bool IsComplete { get; private set; }
        public int IterationDelayMs { get; set; }
    }
}
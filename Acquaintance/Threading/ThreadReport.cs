using System.Collections.Generic;
using System.Text;

namespace Acquaintance.Threading
{
    public class ThreadReport
    {
        public IReadOnlyList<int> FreeWorkers { get; set; }
        public IReadOnlyList<int> DedicatedWorkers { get; set; }
        public IReadOnlyList<RegisteredManagedThread> RegisteredThreads { get; set; }

        public ThreadReport(IReadOnlyList<int> freeWorkers, IReadOnlyList<int> dedicatedWorkers, IReadOnlyList<RegisteredManagedThread> registeredThreads)
        {
            FreeWorkers = freeWorkers;
            DedicatedWorkers = dedicatedWorkers;
            RegisteredThreads = registeredThreads;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            const string separator = "-----------------------------------------------------------";

            sb.AppendLine("Free Workers:");
            foreach (var thread in FreeWorkers)
                sb.AppendLine("\tThreadId:" + thread);

            sb.AppendLine(separator);

            sb.AppendLine("Dedicated Workers:");
            foreach (var thread in DedicatedWorkers)
                sb.AppendLine("\tThreadId:" + thread);

            sb.AppendLine(separator);

            sb.AppendLine("Registered threads (managed externally):");
            foreach (var thread in RegisteredThreads)
            {
                sb.AppendLine("\tThreadId:" + thread.ThreadId);
                sb.AppendLine("\t\tOwner:" + thread.Owner);
                sb.AppendLine("\t\tPurpose:" + thread.Purpose);
            }

            return sb.ToString();
        }
    }
}
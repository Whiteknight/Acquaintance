using System.Threading;

namespace Acquaintance.Sources
{
    public interface IEventSource
    {
        void CheckForEvents(IEventSourceContext context, CancellationToken cancellationToken);
    }
}

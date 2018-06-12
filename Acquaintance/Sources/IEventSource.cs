using System.Threading;

namespace Acquaintance.Sources
{
    public interface IEventSource
    {
        /// <summary>
        /// Called periodically, checks for any events which may be published using the provided context
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        void CheckForEvents(IEventSourceContext context, CancellationToken cancellationToken);
    }
}

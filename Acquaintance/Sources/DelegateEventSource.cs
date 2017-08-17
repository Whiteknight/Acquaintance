using System;
using System.Threading;
using Acquaintance.Utility;

namespace Acquaintance.Sources
{
    public class DelegateEventSource : IEventSource
    {
        private readonly Action<IEventSourceContext, CancellationToken> _func;

        public DelegateEventSource(Action<IEventSourceContext, CancellationToken> func)
        {
            Assert.ArgumentNotNull(func, nameof(func));
            _func = func;
        }

        public DelegateEventSource(Action<IEventSourceContext> func)
        {
            Assert.ArgumentNotNull(func, nameof(func));
            _func = (e, c) => func(e);
        }

        public void CheckForEvents(IEventSourceContext context, CancellationToken cancellationToken)
        {
            _func(context, cancellationToken);
        }
    }
}
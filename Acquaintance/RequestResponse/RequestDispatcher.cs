using System;
using System.Linq;
using Acquaintance.Logging;
using Acquaintance.Utility;

namespace Acquaintance.RequestResponse
{
    public class RequestDispatcher : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IListenerStore _store;

        public RequestDispatcher(ILogger logger, bool allowWildcards)
        {
            _logger = logger;
            _store = CreateStore(allowWildcards);
        }

        public IDisposable Listen<TRequest, TResponse>(string topic, IListener<TRequest, TResponse> listener)
        {
            Assert.ArgumentNotNull(listener, nameof(listener));
            var token = _store.Listen(topic ?? string.Empty, listener);
            _logger.Debug("Listener {0} added for RequestType={1} ResponseType={2} Topic={3}", listener.Id, typeof(TRequest).FullName, typeof(TResponse).FullName, topic);
            return token;
        }

        public void Request<TRequest, TResponse>(string topic, Envelope<TRequest> envelope, IResponseReceiver<TResponse> request)
        {
            Assert.ArgumentNotNull(envelope, nameof(envelope));
            Assert.ArgumentNotNull(request, nameof(request));

            var topicEnvelope = envelope.RedirectToTopic(topic);
            var listener = _store.GetListener<TRequest, TResponse>(topic);
            if (listener == null || !listener.CanHandle(envelope))
            {
                request.SetNoResponse();
                return;
            }

            _logger.Debug("Requesting RequestType={0} ResponseType={1} Topic={2} to listener Id={3}", typeof(TRequest).FullName, typeof(TResponse).FullName, envelope.Topics.FirstOrDefault(), listener.Id);
            try
            {
                listener.Request(topicEnvelope, request);
            }
            catch (Exception e)
            {
                _logger.Error($"Error on request Type={typeof(TRequest).FullName}, {typeof(TResponse).FullName} Topic={topic}, Listener Id={listener.Id}: {e.Message}\n{e.StackTrace}");
                request.SetError(e);
            }

            if (listener.ShouldStopListening)
                _store.RemoveListener(topic, listener);
        }

        public void Dispose()
        {
            (_store as IDisposable)?.Dispose();
        }

        private static IListenerStore CreateStore(bool allowWildcards)
        {
            if (allowWildcards)
                return new TrieListenerStore();
            return new SimpleListenerStore();
        }
    }
}

using System;
using System.Collections.Generic;

namespace Acquaintance.Sagas
{
    public class SagaBuilder<TState, TKey> : ISagaBuilder<TState, TKey>
    {
        private readonly List<IBuildStep> _steps;

        public SagaBuilder()
        {
            _steps = new List<IBuildStep>();
        }

        public void BuildTo(Saga<TState, TKey> saga)
        {
            if (_steps.Count == 0)
                throw new Exception("Saga is not configured");
            foreach (var step in _steps)
                step.BuildTo(saga);
        }

        public ISagaBuilder<TState, TKey> StartWith<TPayload>(string topic, Func<TPayload, TKey> getKey, Func<TPayload, TState> createState, Action<ISagaContext<TState, TKey>> onReceive)
        {
            _steps.Add(new StartWithStep<TPayload>(topic, getKey, createState, onReceive));
            return this;
        }

        public ISagaBuilder<TState, TKey> ContinueWith<TPayload>(string topic, Func<TPayload, TKey> getKey, Action<ISagaContext<TState, TKey>, TPayload> onReceive)
        {
            _steps.Add(new ContinueWithStep<TPayload>(topic, getKey, onReceive));
            return this;
        }

        public void WhenCompleted(Action<IPublishable, TState> onComplete)
        {
            _steps.Add(new WhenCompleteStep(onComplete));
        }

        private interface IBuildStep
        {
            void BuildTo(Saga<TState, TKey> saga);
        }

        private class ContinueWithStep<TPayload> : IBuildStep
        {
            private readonly string _topic;
            private readonly Func<TPayload, TKey> _getKey;
            private readonly Action<ISagaContext<TState, TKey>, TPayload> _onReceive;

            public ContinueWithStep(string topic, Func<TPayload, TKey> getKey, Action<ISagaContext<TState, TKey>, TPayload> onReceive)
            {
                _topic = topic;
                _getKey = getKey;
                _onReceive = onReceive;
            }

            public void BuildTo(Saga<TState, TKey> saga)
            {
                saga.ContinueWith(_topic, _getKey, _onReceive);
            }
        }

        private class StartWithStep<TPayload> : IBuildStep
        {
            private readonly string _topic;
            private readonly Func<TPayload, TKey> _getKey;
            private readonly Func<TPayload, TState> _createState;
            private readonly Action<ISagaContext<TState, TKey>> _onReceive;

            public StartWithStep(string topic, Func<TPayload, TKey> getKey, Func<TPayload, TState> createState, Action<ISagaContext<TState, TKey>> onReceive)
            {
                _topic = topic;
                _getKey = getKey;
                _createState = createState;
                _onReceive = onReceive;
            }

            public void BuildTo(Saga<TState, TKey> saga)
            {
                saga.StartWith(_topic, _getKey, _createState, _onReceive);
            }
        }

        private class WhenCompleteStep : IBuildStep
        {
            private readonly Action<IPublishable, TState> _onComplete;

            public WhenCompleteStep(Action<IPublishable, TState> onComplete)
            {
                _onComplete = onComplete;
            }

            public void BuildTo(Saga<TState, TKey> saga)
            {
                saga.WhenCompleted(_onComplete);
            }
        }
    }
}
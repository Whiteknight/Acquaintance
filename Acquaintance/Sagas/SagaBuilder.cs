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
            _steps.Add(new StartWithStep<TPayload>
            {
                Topic = topic,
                GetKey = getKey,
                CreateState = createState,
                OnReceive = onReceive
            });
            return this;
        }

        public ISagaBuilder<TState, TKey> ContinueWith<TPayload>(string topic, Func<TPayload, TKey> getKey, Action<ISagaContext<TState, TKey>, TPayload> onReceive)
        {
            _steps.Add(new ContinueWithStep<TPayload>
            {
                Topic = topic,
                GetKey = getKey,
                OnReceive = onReceive
            });
            return this;
        }

        public void WhenCompleted(Action<IPublishable, TState> onComplete)
        {
            _steps.Add(new WhenCompleteStep
            {
                OnComplete = onComplete
            });
        }

        private interface IBuildStep
        {
            void BuildTo(Saga<TState, TKey> saga);
        }

        private class ContinueWithStep<TPayload> : IBuildStep
        {
            public string Topic { get; set; }
            public Func<TPayload, TKey> GetKey { get; set; }
            public Action<ISagaContext<TState, TKey>, TPayload> OnReceive { get; set; }

            public void BuildTo(Saga<TState, TKey> saga)
            {
                saga.ContinueWith(Topic, GetKey, OnReceive);
            }
        }

        private class StartWithStep<TPayload> : IBuildStep
        {
            public string Topic { get; set; }
            public Func<TPayload, TKey> GetKey { get; set; }
            public Func<TPayload, TState> CreateState { get; set; }
            public Action<ISagaContext<TState, TKey>> OnReceive { get; set; }

            public void BuildTo(Saga<TState, TKey> saga)
            {
                saga.StartWith(Topic, GetKey, CreateState, OnReceive);
            }
        }

        private class WhenCompleteStep : IBuildStep
        {
            public Action<IPublishable, TState> OnComplete { get; set; }

            public void BuildTo(Saga<TState, TKey> saga)
            {
                saga.WhenCompleted(OnComplete);
            }
        }
    }
}
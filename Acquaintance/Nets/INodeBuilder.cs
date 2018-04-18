using System;
using System.Collections.Generic;
using Acquaintance.PubSub;

namespace Acquaintance.Nets
{
    public interface INodeBuilderReader<TInput>
    {
        INodeBuilderAction<TInput> ReadInput();
        INodeBuilderAction<TInput> ReadOutputFrom(string nodeName);
        INodeBuilderAction<TInput> ReadOutputFrom(NodeToken node);
    }

    public interface INodeBuilderAction<TInput>
    {
        INodeBuilderDetails<TInput> Transform<TOut>(Func<TInput, TOut> transform);
        INodeBuilderDetails<TInput> TransformMany<TOut>(Func<TInput, IEnumerable<TOut>> handler);
        INodeBuilderDetails<TInput> Handle(Action<TInput> action);
        INodeBuilderDetails<TInput> Handle(ISubscriptionHandler<TInput> handler);
    }

    public interface INodeBuilderDetails<TInput>
    {
        INodeBuilderDetails<TInput> OnCondition(Func<TInput, bool> predicate);
        INodeBuilderDetails<TInput> OnDedicatedThread();
        INodeBuilderDetails<TInput> OnDedicatedThreads(int numThreads);
    }
}

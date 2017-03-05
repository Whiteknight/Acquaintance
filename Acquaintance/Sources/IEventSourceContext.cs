namespace Acquaintance.Sources
{
    public interface IEventSourceContext : IPubSubBus
    {
        void Complete();
        bool IsComplete { get; }
    }
}

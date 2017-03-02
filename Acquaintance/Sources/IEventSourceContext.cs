namespace Acquaintance.Sources
{
    public interface IEventSourceContext : IPublishable
    {
        void Complete();
        bool IsComplete { get; }
    }
}

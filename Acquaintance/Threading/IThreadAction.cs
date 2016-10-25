namespace Acquaintance.Threading
{
    public interface IThreadAction
    {
        void Execute(MessageHandlerThreadContext threadContext);
    }
}
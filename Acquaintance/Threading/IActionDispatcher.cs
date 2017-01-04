namespace Acquaintance.Threading
{
    public interface IActionDispatcher
    {
        void DispatchAction(IThreadAction action);
    }
}
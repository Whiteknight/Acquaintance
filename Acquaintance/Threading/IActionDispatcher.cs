namespace Acquaintance.Threading
{
    public interface IActionDispatcher
    {
        /// <summary>
        /// Dispatches the action object to the appropriate worker threads
        /// </summary>
        /// <param name="action"></param>
        void DispatchAction(IThreadAction action);
    }
}
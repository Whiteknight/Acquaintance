using System;
using System.Threading.Tasks;

namespace Acquaintance.Threading
{
    public class ThreadPoolActionDispatcher : IActionDispatcher
    {
        public void DispatchAction(IThreadAction action)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    action.Execute();
                }
                catch (Exception e)
                {
                    // TODO: Log it or inform the user somehow?
                }
            });
        }
    }
}

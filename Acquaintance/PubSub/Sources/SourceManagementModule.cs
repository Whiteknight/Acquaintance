//using System;
//using System.Threading;

//namespace Acquaintance.PubSub.Sources
//{
//    public class SourceManagementModule : IMessageBusModule
//    {
//        private IMessageBus _messageBus;
//        public SourceManager SourceManager { get; private set; }
//        private Thread _workerThread;

//        public void Attach(IMessageBus messageBus)
//        {
//            _messageBus = messageBus;
//        }

//        public void Start()
//        {
//            throw new NotImplementedException();
//        }

//        public void Stop()
//        {
//            _shouldStop = true;
//            _workerThread.Join();
//        }

//        public void Unattach()
//        {
//            _messageBus = null;
//            SourceManager = null;
//        }

//        public void Dispose()
//        {
//            Stop();
//        }

//    }

//    public class SourceContext
//    {

//    }

//    public class SourceManager
//    {
//        private readonly IMessageBus _messageBus;

//        public SourceManager(IMessageBus messageBus)
//        {
//            if (messageBus == null)
//                throw new ArgumentNullException(nameof(messageBus));
//            _messageBus = messageBus;
//        }


//    }
//}

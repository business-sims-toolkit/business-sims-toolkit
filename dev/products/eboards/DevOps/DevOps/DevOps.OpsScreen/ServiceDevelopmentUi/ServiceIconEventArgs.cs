using System;

using Network;

namespace DevOps.OpsScreen.ServiceDevelopmentUi
{
    public class ServiceIconEventArgs : EventArgs
    {
        public Node ServiceNode { get; private set; }

        public ServiceIconEventArgs (Node serviceNode)
        {
            ServiceNode = serviceNode;
        }
    }
}

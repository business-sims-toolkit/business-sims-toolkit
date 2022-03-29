using System.Collections.Generic;

using Network;

namespace DevOps.OpsEngine
{
    public class DevelopingAppTerminator
    {
        public DevelopingAppTerminator (NodeTree model)
        {
            servicesCommandQueueNode = model.GetNamedNode("BeginServicesCommandQueue");
        }

        public void TerminateApp (Node app, string terminationCommand)
        {
            // ReSharper disable once ObjectCreationAsStatement
            new Node(servicesCommandQueueNode, terminationCommand, "",
                new List<AttributeValuePair>
                {
                    new AttributeValuePair("type", terminationCommand),
                    new AttributeValuePair("service_name", app.GetAttribute("name"))
                });

        }

        readonly Node servicesCommandQueueNode;

    }
}

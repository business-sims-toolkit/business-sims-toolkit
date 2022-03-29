using System.Collections.Generic;
using System.Xml;

using GameManagement;
using LibCore;
using Logging;
using Network;

namespace ReportBuilder
{
    public class DevOps_DevErrorReport
    {
        class ServiceErrorMessages
        {
            class GroupedMessages
            {
	            readonly string guid;
	            readonly List<string> messages;

                public GroupedMessages(string guid)
                {
                    this.guid = guid;
                    messages = new List<string>();
                }

                public void Add(string message)
                {
                    messages.Add(message);
                }

                public void OutputToXml(BasicXmlDocument xml, XmlElement parent)
                {
                    foreach (var message in messages)
                    {
                        var errorMessage = xml.AppendNewChild(parent, "Error_Message");
                        BasicXmlDocument.AppendAttribute(errorMessage, "message", message);
                        BasicXmlDocument.AppendAttribute(errorMessage, "guid", guid);
                    }
                    
                }
            }

            class ErrorType
            {
	            readonly string type;

	            readonly Dictionary<string, GroupedMessages> messages;

                public ErrorType(string type)
                {
                    this.type = type;
                    messages = new Dictionary<string, GroupedMessages>();
                }

                public void Add(string guid, string message)
                {
                    if (!messages.ContainsKey(guid))
                    {
                        messages[guid] = new GroupedMessages(guid);
                    }

                    messages[guid].Add(message);
                }

                public void OutputToXml(BasicXmlDocument xml, XmlElement parent)
                {
                    var error = xml.AppendNewChild(parent, "Error");
                    BasicXmlDocument.AppendAttribute(error, "error_type", type);

                    foreach(var guid in messages.Keys)
                    {
                        messages[guid].OutputToXml(xml, error);
                    }

                }
            }

	        readonly string serviceName;

	        readonly Dictionary<string, ErrorType> errors;

            public ServiceErrorMessages(string serviceName)
            {
                this.serviceName = serviceName;

                errors = new Dictionary<string, ErrorType>();
            }

            public void AddError(string errorType, string errorGuid, string errorMessage)
            {
                if (!errors.ContainsKey(errorType))
                {
                    errors[errorType] = new ErrorType(errorType);
                }

                errors[errorType].Add(errorGuid, errorMessage);
            }

            public void OutputToXml(BasicXmlDocument xml, XmlElement parent, NodeTree model)
            {
                var service = xml.AppendNewChild(parent, "Service");
                BasicXmlDocument.AppendAttribute(service, "service_name", serviceName);
                foreach (var type in errors.Keys)
                {
                    errors[type].OutputToXml(xml, service);
                }

	            BasicXmlDocument.AppendAttribute(service, "icon_name",
		            model.GetNamedNode("NS " + serviceName).GetAttribute("icon"));
            }
        }

	    readonly BasicXmlDocument xml;

	    readonly Dictionary<string, ServiceErrorMessages> servicesErrorMessages;

	    readonly NodeTree model;

        public DevOps_DevErrorReport(NodeTree model)
        {
	        this.model = model;
	        
            xml = BasicXmlDocument.Create();

            servicesErrorMessages = new Dictionary<string, ServiceErrorMessages>();
        }

        public string BuildReport(NetworkProgressionGameFile gameFile, int round)
        {
            // Pull the logfile to get data from.
            var logFile = gameFile.GetRoundFile(round, "NetworkIncidents.log", GameFile.GamePhase.OPERATIONS);

            var biLogReader = new BasicIncidentLogReader(logFile);
            
            // Watch for costed events
            biLogReader.WatchCreatedNodes("CostedEvents", biLogReader_CostedEventFound);
            biLogReader.Run();

            var root = xml.AppendNewChild("DevErrorReport");

            var services = xml.AppendNewChild(root, "Services");

            foreach (var serviceName in servicesErrorMessages.Keys)
            {
                servicesErrorMessages[serviceName].OutputToXml(xml, services, model);
            }
			
            var reportFile = gameFile.GetRoundFile(round, "DevErrorReport.xml", GameFile.GamePhase.OPERATIONS);
            xml.Save(reportFile);
            
            return reportFile;
        }

        protected void biLogReader_CostedEventFound(object sender, string key, string line, double time)
        {
            var type = BasicIncidentLogReader.ExtractValue(line, "type");
            
            if (type == "NS_error")
            {
                var serviceName = BasicIncidentLogReader.ExtractValue(line, "service_name");
                var errorType = BasicIncidentLogReader.ExtractValue(line, "error_type");

                if (errorType == "undo")
                {
                    servicesErrorMessages.Remove(serviceName);
                    return;
                }

                var errorMessage = BasicIncidentLogReader.ExtractValue(line, "error_message");
                var errorGuid = BasicIncidentLogReader.ExtractValue(line, "error_guid");

                if (!servicesErrorMessages.ContainsKey(serviceName))
                {
                    servicesErrorMessages[serviceName] = new ServiceErrorMessages(serviceName);
                }

                servicesErrorMessages[serviceName].AddError(errorType, errorGuid, errorMessage);
                
            }
        }
    }
}
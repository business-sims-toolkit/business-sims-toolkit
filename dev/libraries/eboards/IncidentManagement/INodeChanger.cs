using System.Collections.Generic;
using Network;

namespace IncidentManagement
{
	public interface INodeChanger
	{
		NodeTree Model { get; }

		void SetAttribute (Node node, string attribute, string value);
		void SetAttribute (Node node, string attribute, int value);
		void SetAttribute (Node node, string attribute, bool value);
		void SetAttribute (Node node, string attribute, float value);
		void SetAttributes (Node node, List<AttributeValuePair> attributes);

		void Delete (Node node);
		void Create (Node parent, string name, string type, List<AttributeValuePair> attributes);
	}
}

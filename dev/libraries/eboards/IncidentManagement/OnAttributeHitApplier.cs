using System.Collections;

using System.Xml;

using LibCore;
using Network;

namespace IncidentManagement
{
	public class AttributeHitPendingEvent : PendingEvent
	{
		string Attribute;
		string HitValue;

		Node watchedNode;

		public string Value
		{
			get
			{
				return HitValue;
			}
		}

		public AttributeHitPendingEvent(Node n, string attrib, string val)
		{
			Attribute = attrib;
			HitValue = val;
			watchedNode = n;

			watchedNode.AttributesChanged += watchedNode_AttributesChanged;
		}

		/// <summary>
		/// Displose ...
		/// </summary>
		public void Dispose()
		{
			watchedNode.AttributesChanged -= watchedNode_AttributesChanged;
		}

		void watchedNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			foreach(AttributeValuePair avp in attrs)
			{
				if(Attribute == avp.Attribute)
				{
					if(HitValue == avp.Value)
					{
						this.theEvent.ApplyActionNow(theNodeTree);
						OnAttributeHitApplier.TheInstance.RemoveEvent(this);
					}
				}
			}
		}
	}
	/// <summary>
	/// Summary description for OnAttributeHitApplier.
	/// </summary>
	public sealed class OnAttributeHitApplier
	{
		ArrayList eventsDue = new ArrayList();
		/// <summary>
		/// 
		/// </summary>
		public static readonly OnAttributeHitApplier TheInstance = new OnAttributeHitApplier();
		/// <summary>
		/// 
		/// </summary>
		public void Clear()
		{
			lock(this)
			{
				// TODO : If events can fire events on being discarded/fired then these should
				// be fired rather than letting the events just fade into oblivion.
				foreach(AttributeHitPendingEvent pe in eventsDue)
				{
					pe.Dispose();
				}
				eventsDue.Clear();
			}
		}

		OnAttributeHitApplier()
		{
		}

		public IncidentDefinition AddCreatedNodeEvent(Node parent, ArrayList attrs, string nodeName, string attribute, string hitValue, NodeTree nt)
		{
			string xmldata = "<createNodes i_to=\"" + parent.GetAttribute("name") + "\">";
			xmldata += "<i><node ";
			foreach(AttributeValuePair avp in attrs)
			{
				xmldata += avp.Attribute + "=\"" + avp.Value + "\" ";
			}
			xmldata += "/></createNodes></i>";
			//
			return AddEvent(xmldata, nodeName, attribute, hitValue, nt);
		}

		public IncidentDefinition AddEvent(string xmldata, string nodeName, string attribute, string hitValue, NodeTree nt)
		{
			BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
			return AddEvent(xdoc, nodeName, attribute, hitValue, nt);
		}

		public IncidentDefinition AddEvent(BasicXmlDocument xdoc, string nodeName, string attribute, string hitValue, NodeTree nt)
		{
			XmlNode rootNode = xdoc.DocumentElement;
			return AddEvent(rootNode, nodeName, attribute, hitValue, nt);
		}

		public IncidentDefinition AddEvent(XmlNode rootNode, string nodeName, string attribute, string hitValue, NodeTree nt)
		{
			IncidentDefinition idef = new IncidentDefinition(rootNode, nt);
			idef.doAfterSecs = 0;
			AddEvent((IEvent) idef, nodeName, attribute, hitValue, nt);
			return idef;
		}

		public void AddEvent(IEvent e, string nodeName, string attribute, string hitValue, NodeTree nt)
		{
			lock(this)
			{
				//
				// Assumming single thread for now for NeoSwiff apps.
				// Should add LibCore.LockAcquire / LockRelease funcs to
				// be really thread safe.
				//
				Node watchedNode = nt.GetNamedNode(nodeName);
				AttributeHitPendingEvent pe = new AttributeHitPendingEvent(watchedNode,attribute,hitValue);
				pe.theEvent = e;
				pe.theNodeTree = nt;
				//
				eventsDue.Add(pe);
			}
		}
		/// <summary>
		/// Removes an Event 
		/// </summary>
		/// <param name="e">The event to kill</param>
		public void RemoveEvent(AttributeHitPendingEvent e)
		{
			lock(this)
			{
				AttributeHitPendingEvent pe_tokill = e as AttributeHitPendingEvent;
				eventsDue.Remove(pe_tokill);
				pe_tokill.Dispose();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public Hashtable GetFutureIncidentsOfType(string type)
		{
			Hashtable eventToHitValue = new Hashtable();
			//
			foreach (AttributeHitPendingEvent pe in eventsDue)
			{
				IncidentDefinition idef = pe.theEvent as IncidentDefinition;
				if(null != idef)
				{
					if(idef.Type == type)
					{
						eventToHitValue.Add(idef, pe.Value);
					}
				}
			}
			//
			return eventToHitValue;
		}

		public void RemoveEvent(IEvent e)
		{
			lock(this)
			{
				AttributeHitPendingEvent pe_tokill = null;
				foreach (AttributeHitPendingEvent pe in eventsDue)
				{
					if (pe.theEvent == e)
					{
						pe_tokill = pe;
					}
				}
				//
				if(null != pe_tokill)
				{
					eventsDue.Remove(pe_tokill);
				}
			}
		}
	}
}


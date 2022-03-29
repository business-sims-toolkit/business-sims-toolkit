using System;
using LibCore;
using Network;
using CoreUtils;

namespace Logging
{

	/// <summary>
	/// Just a simple logger to report stuff to the console
	/// </summary>
	public class ConsoleLogger : ITimedClass
	{
		protected NodeTree _tree = null;
		protected DateTime started = DateTime.Now;

		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			CoreUtils.TimeManager.TheInstance.UnmanageClass(this);

			if(null != _tree)
			{
				_tree.TreeChanged -= _tree_TreeChanged;
				_tree.NodeAdded -= _tree_NodeAdded;
				_tree.NodeMoved -= _tree_NodeMoved;
			}
		}

		public void Reset()
		{
			lock(this)
			{
				started = DateTime.Now;
			}
		}

		public void FastForward(double timesRealTime)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="t"></param>
		public void LogTreeToFile(NodeTree t)
		{
			if(_tree != null)
			{
				_tree.TreeChanged -= _tree_TreeChanged;
				_tree.NodeAdded -= _tree_NodeAdded;
				_tree.NodeMoved -= _tree_NodeMoved;
			}
			//
			//
			_tree = t;
			//
			_tree.TreeChanged += _tree_TreeChanged;
			_tree.NodeAdded += _tree_NodeAdded;
			_tree.NodeMoved += _tree_NodeMoved;
			//
			started = DateTime.Now;
		}

		/// <summary>
		/// 
		/// </summary>
		public ConsoleLogger()
		{
			CoreUtils.TimeManager.TheInstance.ManageClass(this);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		void _tree_TreeChanged(object sender, NodeTreeEventArgs args)
		{
			DateTime now = DateTime.Now;
			TimeSpan ts = now-started;

			switch(args.TypeOfEvent)
			{
				case NodeTreeEventArgs.EventType.ArgsChanged:
				{
					string val = "<i id=\"AtStart\"><apply i_doAfterSecs=\"" + CONVERT.ToStr(ts.TotalSeconds) + "\" i_name=\"" + args.NodeAffected.GetAttribute("name") + "\"";
					foreach(AttributeValuePair avp in args.AttributesChanged)
					{
						val += " " + avp.Attribute + "=\"" + avp.Value + "\"";
					}
					val += "/></i>\r\n";
				}
					//tw.Flush();
					break;
					/* Not yet used.
									case NodeTreeEventArgs.EventType.Added:
										break;*/

				case NodeTreeEventArgs.EventType.Deleted:
				{
					// If the node being deleted has no name then we have to log its ID instead.
					string name = args.NodeAffected.GetAttribute("name");
					if("" != name)
					{
						// TODO:!
					}
					else
					{
						// TODO !
					}
				}
					break;
			}
		}

	/// <summary>
	/// TODO : This gets called even if a new node has just been moved in the tree! (Does it? Check!)
	/// This should be changed so that it only gets called if a new node is added to
	/// the tree. A new event "moved node" should be created and called.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="newNode"></param>
	void _tree_NodeAdded(NodeTree sender, Node newNode)
		{
			// TODO : Need a test that creates a new node in the tree!
			DateTime now = DateTime.Now;
			TimeSpan ts = now-started;
			string pname = newNode.Parent.GetAttribute("name");
			//
			string str = "<i id=\"AtStart\"><createNodes i_doAfterSecs=\"" + CONVERT.ToStr(ts.TotalSeconds) + "\" i_to=\"" + pname + "\">";
			str += "<" + newNode.Type;
			//
			// If the node doesn't have a name store its UUID in the file as this will help track it...
			//
			bool named = false;
			//
			foreach(string key in newNode.AttributesAsStringDictionary)
			{
				string val = newNode.GetAttribute(key);
				str += " " + key + "=\"" + val + "\"";
				if( (key == "name") && (val != "") ) named = true;
			}
			//
			if(!named)
			{
				str += " uuid=\"" + CONVERT.ToStr(newNode.ID) + "\"";
			}
			str += "/></createNodes></i>\r\n";
			//str += " name=\"" + newNode.GetAttribute("name") + "\"";
			//System.Diagnostics.Debug.WriteLine(str);
		}

		void _tree_NodeMoved(NodeTree sender, Node oldParent, Node movedNode)
		{
			Node parentNode = movedNode.Parent;
			if(null != parentNode)
			{
				DateTime now = DateTime.Now;
				TimeSpan ts = now-started;
			}
			else
			{
				// Should throw!
			}
		}
		#region ITimedClass Members

		/// <summary>
		/// 
		/// </summary>
		public void Start()
		{
		}

		/// <summary>
		/// 
		/// </summary>
		public void Stop()
		{
		}

		#endregion
	}
	
}

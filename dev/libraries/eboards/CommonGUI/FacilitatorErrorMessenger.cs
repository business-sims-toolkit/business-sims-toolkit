using System;
using System.Windows.Forms;

using Network;

namespace CommonGUI
{
	/// <summary>
	/// The FacilitatorErrorMessenger watches a model and pops up a dialog box if there has been an error.
	/// </summary>
	public class FacilitatorErrorMessenger
	{
		Control parent;
		protected Node errorNode;

		bool isInteractionDisabled;

		public bool IsInteractionDisabled
		{
			get => isInteractionDisabled;

			set
			{
				isInteractionDisabled = value;
			}
		}

		public FacilitatorErrorMessenger(Control parent, NodeTree tree)
		{
			this.parent = parent;
			errorNode = tree.GetNamedNode("FacilitatorNotifiedErrors");
			errorNode.ChildAdded += errorNode_ChildAdded;
		}

		public void Dispose()
		{
			errorNode.ChildAdded -= errorNode_ChildAdded;
		}

		void errorNode_ChildAdded(Node sender, Node child)
		{
			if (isInteractionDisabled)
			{
				Console.WriteLine(child.GetAttribute("text"));
			}
			else
			{
				MessageBox.Show(parent.TopLevelControl, child.GetAttribute("text"));
			}

			child.Parent.DeleteChildTree(child);
		}
	}
}

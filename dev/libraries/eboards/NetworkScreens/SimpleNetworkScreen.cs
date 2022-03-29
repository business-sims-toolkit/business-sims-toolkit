using System;
using System.IO;
using LibCore;
using Network;

namespace NetworkScreens
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class SimpleNetworkScreen : VisualPanel
	{
		protected NodeTree _model;
		private NodeTree _Visuals;
		protected TreeNetPanel netView;
		//
		public SimpleNetworkScreen(NodeTree model, string visuals_file)
		{
			_model = model;
			//
			StreamReader sr = File.OpenText(visuals_file);
			string data = sr.ReadToEnd();
			sr.Close();
			//
			_Visuals = new NodeTree(data);
			//
			netView = new TreeNetPanel(model, _Visuals);
			//
			this.Controls.Add(netView);
			//
			this.Resize += new EventHandler(SimpleNetworkScreen_Resize);
		}

		private void SimpleNetworkScreen_Resize(object sender, EventArgs e)
		{
			netView.Size = this.Size;
		}
	}
}

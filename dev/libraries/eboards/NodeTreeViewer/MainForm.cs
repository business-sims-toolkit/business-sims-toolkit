using System;
using System.Windows.Forms;
using Network;

namespace NodeTreeViewer
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class MainForm : System.Windows.Forms.Form
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.Container components = null;

		protected GraphView gv;

		public MainForm(NodeTree model)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			string model_uuid = model.uuid;

			gv = new GraphView(model.Root);
			gv.Size = this.ClientSize;
			this.Controls.Add(gv);
			gv.NodeClicked += gv_NodeClicked;

			this.Resize += MainForm_Resize;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// 
			// MainForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(712, 478);
			this.Name = "MainForm";
			this.Text = "Form1";

		}
		#endregion
#if BUILDAPP
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			System.IO.StreamReader file = new System.IO.StreamReader("network.xml");
			string xmldata = file.ReadToEnd();
			file.Close();
			file = null;

			Network.NodeTree model = new NodeTree(xmldata);

			Application.Run(new MainForm(model));
		}
#endif
		void MainForm_Resize(object sender, EventArgs e)
		{
			gv.Size = this.ClientSize;
		}

        protected Node newNodeToModel = null;

		void gv_NodeClicked(GraphView sender, Node n)
		{
            newNodeToModel = n;
            Timer t = new Timer();
            t.Tick += t_Tick;
            t.Interval = 10;
            t.Start();
		}

        void t_Tick(object sender, EventArgs e)
        {
            Timer t = (Timer)sender;
            t.Stop();
            //
            Control[] array = new Control[Controls.Count];
            Controls.CopyTo(array, 0);
			//
            foreach (Control c in array)
            {
                c.Dispose();
            }
            //
            gv = new GraphView(newNodeToModel);
            gv.Size = this.ClientSize;
            this.Controls.Add(gv);
            gv.NodeClicked += gv_NodeClicked;
        }
	}
}

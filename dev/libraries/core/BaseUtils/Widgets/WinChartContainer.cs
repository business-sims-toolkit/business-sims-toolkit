using System;
using System.Drawing;
using System.Windows.Forms;
//using PerformancePlusWidgets.Charting2;

namespace BaseUtils
{
	/// <summary>
	/// Summary description for ChartWinContainer.
	/// </summary>
	public class WinChartContainer : System.Windows.Forms.UserControl
	{
		WinChartAdapter adapter;

		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.Container components = null;
		
		public WinChartContainer()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// Create the chart adapter
			adapter = new WinChartAdapter();

			// Use double-buffering for drawing
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.Opaque, true);
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.Clear(Color.White);
			adapter.Draw(e.Graphics);
			base.OnPaint (e);
		}

		protected override void OnResize(EventArgs e)
		{
			adapter.Size = new Size(this.Width, this.Height);
			Invalidate();
			base.OnResize (e);
		}

		public WinChartAdapter ChartAdapter
		{
			get { return adapter; }
		}

		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
	}
}

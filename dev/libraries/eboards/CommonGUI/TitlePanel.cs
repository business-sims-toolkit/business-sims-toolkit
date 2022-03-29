using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LibCore;

namespace CommonGUI
{
	/// <summary>
	/// The TitlePanel draws a high quality test string over a panel 
	/// The panel is currently a coloured background but could be extended to have a bitmap
	/// 
	/// This is ensure that the draw title has the same look as the other test in 
	/// the application. 
	/// </summary>
	public class TitlePanel : BasePanel
	{
		
		//Presentation Variables : Fonts
		Font BoldFont11 = CoreUtils.SkinningDefs.TheInstance.GetFont(11, FontStyle.Bold);
		Font RegularFont11 = CoreUtils.SkinningDefs.TheInstance.GetFont(11, FontStyle.Regular);

		Brush TextForeColorBrush = Brushes.DimGray;

		int OffsetX=2;
		int OffsetY=2;
		string TitleText ="";
		bool UseNormalFontFlag = false;
		
		public TitlePanel(string new_titletext) 
		{
			//Setup the paint optmisations
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer,true);
			BackColor = Color.FromArgb(240,240,240);

			TitleText = new_titletext;
		}
		
		public void SetTitleOffset(int x, int y)
		{
			OffsetX=x;
			OffsetY=y;
		}

		public void SetTitleForeColorBlack()
		{
			TextForeColorBrush = Brushes.Black;
		}

		public void UseNormalFont()
		{
			UseNormalFontFlag = true;
		}


		#region Component Designer generated code
		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			// CT
			this.Name = "CT";
		}
		
		#endregion

		protected override void OnPaint(PaintEventArgs e)
		{
			//Setup the Quality and Rendering Options 
			e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
			e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;

			StringFormat format2 = new StringFormat();
			format2.Alignment = StringAlignment.Center;
			format2.LineAlignment = StringAlignment.Center;

			if (UseNormalFontFlag)
			{
				e.Graphics.DrawString(TitleText,RegularFont11,TextForeColorBrush,OffsetX,OffsetY); 
			}
			else
			{
				e.Graphics.DrawString(TitleText,BoldFont11,TextForeColorBrush,OffsetX,OffsetY); 
			}
		}

	}
	
}

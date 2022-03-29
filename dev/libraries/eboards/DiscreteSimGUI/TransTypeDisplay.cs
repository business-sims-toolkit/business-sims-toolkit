using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CoreUtils;
using LibCore;
using CommonGUI;

namespace DiscreteSimGUI
{
	/// <summary>
	/// This class handles the display of the Transaction Headers (usually "Online" and Instore")
	/// You can override the test fore colors by using SetTextColors 
	/// 
	/// The font should be connected to the application font defined in the skin file 
	/// </summary>
	public class TransTypeDisplay :FlickerFreePanel
	{
		Font dispfont_title = null;
		bool auto_translate = true;
		bool IsTrainingGame = false;
		string transaction_header_1 = "Instore";
		string transaction_header_2 = "Online";
		Brush NormalBrush = new SolidBrush(Color.White);
		Brush TrainingBrush = new SolidBrush(Color.Black);
		
		public TransTypeDisplay(bool IsTrainingGameFlag)
		{
			IsTrainingGame = IsTrainingGameFlag;
			//Font dispfont_title = ConstantSizeFont.NewFont("Verdana",12f, FontStyle.Bold);
			//Font dispfont_values = ConstantSizeFont.NewFont("Verdana",9.5f, FontStyle.Bold);
			string font_name = SkinningDefs.TheInstance.GetData("fontname", "Trebuchet MS");

			//Are we overriding the header names in the skin file
			transaction_header_1 = CoreUtils.SkinningDefs.TheInstance.GetData("transaction_header_1",transaction_header_1);
			transaction_header_2 = CoreUtils.SkinningDefs.TheInstance.GetData("transaction_header_2",transaction_header_2);

			//Now translate if the auto translate is ON
			if(auto_translate)
			{
				transaction_header_1 = TextTranslator.TheInstance.Translate(transaction_header_1);
				transaction_header_2 = TextTranslator.TheInstance.Translate(transaction_header_2);
			}

			if(auto_translate)
			{
				font_name = TextTranslator.TheInstance.GetTranslateFont(font_name);
				dispfont_title = ConstantSizeFont.NewFont(font_name,10f, FontStyle.Bold);
			}
			else
			{
				if (IsTrainingGame == false)
				{
					dispfont_title = ConstantSizeFont.NewFont(font_name,10f, FontStyle.Bold);
				}
				else
				{
					dispfont_title = ConstantSizeFont.NewFont(font_name,8f, FontStyle.Regular);
				}
			}
		}

		public new void Dispose()
		{
			if (dispfont_title != null)
			{
				dispfont_title.Dispose();
				dispfont_title =null;
			}
			if (NormalBrush != null)
			{
				NormalBrush.Dispose();
				NormalBrush = null;
			}
			if (TrainingBrush != null)
			{
				TrainingBrush.Dispose();
				TrainingBrush = null;
			}
		}

		public void setTextColors(Color normalColor, Color trainingColor)
		{
			NormalBrush.Dispose();
			NormalBrush = new SolidBrush(normalColor);
			TrainingBrush.Dispose();
			TrainingBrush = new SolidBrush(trainingColor);
		}

		/// <summary>
		/// Override on Paint
		/// </summary>
		/// <param name="e"></param>
		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			Brush BoxBrush = Brushes.White;

			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.CompositingQuality = CompositingQuality.HighQuality;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;					

			StringFormat sf = new StringFormat(StringFormatFlags.MeasureTrailingSpaces);
			SizeF instore_measure = g.MeasureString(transaction_header_1,dispfont_title,this.Width,sf);
			SizeF online_measure = g.MeasureString(transaction_header_2,dispfont_title,this.Width,sf);

			int offset_instore = (this.Width /4)-  (((int)instore_measure.Width)/2);
			int offset_online = ((this.Width*3) /4)-  (((int)online_measure.Width)/2);

			if (IsTrainingGame== false)
			{
				g.DrawString(transaction_header_1, dispfont_title, NormalBrush, offset_instore, 0);
				g.DrawString(transaction_header_2, dispfont_title, NormalBrush, offset_online, 0);
			}
			else
			{
				g.DrawString(transaction_header_1, dispfont_title, TrainingBrush, offset_instore, 0);
				g.DrawString(transaction_header_2, dispfont_title, TrainingBrush, offset_online, 0);
			}
		}

	}
}

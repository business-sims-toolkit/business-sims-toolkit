using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreUtils;

namespace GameDetails
{
	public class EmergencyActivationCodesPanel : Panel
	{
		TextBox instructions;
		Label responseCodeLabel;
		TextBox responseCodeBox;
		Button back;

		public EmergencyActivationCodesPanel (string instructionsText)
		{
			instructions = new TextBox { Font = new Font (FontFamily.GenericMonospace, 12), ReadOnly = true, Multiline = true };
			instructions.Text = instructionsText;
			Controls.Add(instructions);

			responseCodeLabel = SkinningDefs.TheInstance.CreateLabel("Activation Response Code", 20, FontStyle.Bold);
			Controls.Add(responseCodeLabel);

			responseCodeBox = SkinningDefs.TheInstance.CreateTextBox(20, FontStyle.Bold);
			responseCodeBox.TextChanged += responseCodeBox_ResponseCodeChanged;
			Controls.Add(responseCodeBox);

			back = SkinningDefs.TheInstance.CreateWindowsButton(FontStyle.Bold, 20);
			back.Text = "Back to details";
			back.Click += back_Click;
			Controls.Add(back);

			DoSize();
		}

		public string ResponseCode => responseCodeBox.Text;

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			int gap = 10;

			var backSize = new Size (300, back.PreferredSize.Height);
			back.Bounds = new Rectangle ((Width - backSize.Width) / 2, Height - gap - backSize.Height, backSize.Width, backSize.Height);

			responseCodeBox.Bounds = new Rectangle (gap, back.Top - gap - responseCodeBox.PreferredHeight, Width - (2 * gap), responseCodeBox.PreferredHeight);
			responseCodeLabel.Bounds = new Rectangle (gap, responseCodeBox.Top - gap - responseCodeLabel.PreferredHeight, Width - (2 * gap), responseCodeLabel.PreferredHeight);
			instructions.Bounds = new Rectangle (gap, gap, Width - (2 * gap), responseCodeLabel.Top - (3 * gap));
		}

		void responseCodeBox_ResponseCodeChanged (object sender, EventArgs args)
		{
		}

		public event EventHandler ResponseCodeCompleted;
		public event EventHandler Cancelled;

		void OnResponseCodeCompleted ()
		{
			ResponseCodeCompleted?.Invoke(this, EventArgs.Empty);
		}

		void back_Click (object sender, EventArgs args)
		{
			OnCancelled();
		}

		void OnCancelled ()
		{
			Cancelled?.Invoke(this, EventArgs.Empty);
		}
	}
}
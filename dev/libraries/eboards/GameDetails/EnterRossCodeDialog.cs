using System;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;

namespace GameDetails
{
	public partial class EnterRossCodeDialog : LibCore.CustomDialogBox
	{
        protected Label prompt;
        protected TextBox textBox;

		public string RossCode
		{
			get
			{
				return textBox.Text.Trim();
			}

			set
			{
				textBox.Text = value;
			}
		}

		public EnterRossCodeDialog ()
		{
			Text = "ROSS Code";

            blurb.Text = "Enter a ROSS Code to generate a game credit. This will be verified by our servers, so please ensure you are online before pressing Upload.";

            

            ok.Text = "Upload";


            
            prompt = new Label { Text = "ROSS Code: ", TextAlign = ContentAlignment.MiddleLeft };
            Controls.Add(prompt);

            textBox = new TextBox();
            textBox.Text = "ROSS";
            textBox.KeyPress += textBox_KeyPress;
            textBox.TextChanged += textBox_TextChanged;
            Controls.Add(textBox);
			
		}

        protected override void UpdateButtons()
        {
            if (ok != null && textBox != null)
            {
                ok.Enabled = !string.IsNullOrEmpty(textBox.Text.Trim());
            }
        }


        protected override void DoSize()
        {
            base.DoSize();


            if (prompt != null)
            {
                prompt.Location = new Point(margin, ok.Top - gap - ok.Height);
                prompt.Size = new Size(75, 20);
                blurbBackground.Size = new Size(ClientSize.Width - blurbBackground.Left, prompt.Top - gap - blurbBackground.Top);

            }

            if (textBox != null)
            {
                textBox.Location = new Point(prompt.Right + gap, prompt.Top);
                textBox.Size = new Size(ClientSize.Width - margin - textBox.Left, 24);
            }

        }
		

		protected override void textBox_KeyPress (object sender, KeyPressEventArgs e)
		{
			if ((e.KeyChar == 10) || (e.KeyChar == 13))
			{
				if (ok.Enabled)
				{
					ok_Click(sender, e);
				}
			}
			else if (e.KeyChar == 27)
			{
				cancel_Click(sender, e);
			}
		}



		protected override void ok_Click (object sender, EventArgs e)
		{
			UploadRossCodeEventArgs args = OnUploadRossCode();

			blurb.Text = args.Message;

			if (args.Success)
			{
				textBox.Enabled = false;
				ok.Hide();
				cancel.Hide();
				close.Show();
			}
		}

		public class UploadRossCodeEventArgs : EventArgs
		{
			public string RossCode { get; private set; }
			public bool Success { get; set; }
			public string Message { get; set; }

			public UploadRossCodeEventArgs (string rossCode)
			{
				RossCode = rossCode;
			}
		}

		public delegate void UploadRossCodeHandler (object sender, UploadRossCodeEventArgs args);
		public event UploadRossCodeHandler UploadRossCode;

		UploadRossCodeEventArgs OnUploadRossCode ()
		{
			using (WaitCursor cursor = new WaitCursor(TopLevelControl))
			{
				UploadRossCodeEventArgs args = new UploadRossCodeEventArgs (RossCode.Trim().Replace(" ", ""));
				UploadRossCode?.Invoke(this, args);

				return args;
			}
		}
	}
}
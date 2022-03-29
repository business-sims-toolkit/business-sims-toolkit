using System;
using System.Windows.Forms;
using System.Drawing;

namespace Charts
{
    public class TipWindow : Form
    {
        public Label label;

        public TipWindow()
        {
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.White;

            label = new Label();
            label.BorderStyle = BorderStyle.FixedSingle;
            label.TextAlign = ContentAlignment.MiddleCenter;
            this.SuspendLayout();
            this.Controls.Add(label);
            this.ResumeLayout(false);

            this.Resize += TipWindow_Resize;
        }

	    void TipWindow_Resize(object sender, EventArgs e)
        {
            label.Size = this.Size;
        }

        public void SetText(string text)
        {
            label.Text = text;
        }
    }

}

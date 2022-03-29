using System;
using System.Drawing;
using System.Windows.Forms;

namespace GameDetails
{
    public class DownloadCompleteDialog : LibCore.CustomDialogBox
    {

        
        RichTextBox miniReleaseNote;

        public DownloadCompleteDialog(string releaseNoteLocation)
            : base()
        {
            Text = "Download Complete";
            ok.Text = "Install";

            string releaseNoteTextAddition = releaseNoteLocation == null ? "." : ", and the release note is shown below.";
            blurb.Text = "The installer for the latest version of the software has been downloaded to your desktop" + releaseNoteTextAddition + "\n\nWould you like to close the Application now and run the installer?";

            if (releaseNoteLocation != null)
            {

                miniReleaseNote = new RichTextBox();
                miniReleaseNote.LoadFile(releaseNoteLocation);
                miniReleaseNote.Location = new Point(10, 75);
                Controls.Add(miniReleaseNote);
                miniReleaseNote.BringToFront();
                miniReleaseNote.Size = new Size(blurbBackground.Width - 20, blurbBackground.Height - 20 - miniReleaseNote.Top);
            }
            


        }

        protected override void DoSize()
        {
            base.DoSize();

            //blurbBackground.Height = 65;
            using (Graphics g = CreateGraphics())
            {
                SizeF size = g.MeasureString(blurb.Text, blurb.Font, ClientSize.Width - margin * 2);

               blurbBackground.Height = (int)size.Height + 15;
            }

            if (miniReleaseNote != null)
            {
                miniReleaseNote.Size = new Size(blurbBackground.Width - 20, ok.Top - 20 - blurbBackground.Height );
            }


        }

        protected override void ok_Click(object sender, EventArgs e)
        {
            base.ok_Click(sender, e);
        }
        

    }
}

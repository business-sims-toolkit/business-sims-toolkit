using System;
using System.Drawing;
using System.Windows.Forms;
using CoreUtils;

using LibCore;

namespace DevOps.OpsScreen
{
    public class RequestStatusDisplayRow : FlowLayoutPanel
    {
	    int heightPadding = 10;
	    int widthPadding = 5;

        public int HeightPadding
        {
            get { return heightPadding; }
        }

        public int WidthPadding
        {
            get { return widthPadding; }
        }

	    Label requestDisplay;
	    Label iconLabel;
        int iconWidth;

	    Color blank;
        
        Image developingImage;
        Image testImage;
        Image releaseImage;
        Image transitionImage;
        Image deployedImage;

        Image blankImage;

	    string status;

	    string newServiceName;

        public string NewServiceName
        {
            get { return newServiceName; }

        }

	    string demandId;
        public string DemandId
        {
            get { return demandId; }

        }

	    string fontname = SkinningDefs.TheInstance.GetData("fontname");

        public RequestStatusDisplayRow(string mbuSelected, string serviceName, string demandNumber, int height, int width)
        {
            Height = height;
            Width = width;
            
            newServiceName = serviceName;
            demandId = demandNumber;

            blank = Color.DarkBlue;
            
            string path = AppInfo.TheInstance.Location + "\\images\\panels\\10x20_";

            developingImage = Repository.TheInstance.GetImage(path + "dev.png");
            transitionImage = Repository.TheInstance.GetImage(path + "transition.png");
            deployedImage = Repository.TheInstance.GetImage(path + "live.png");

            testImage = Repository.TheInstance.GetImage(path + "test.png");
            releaseImage = Repository.TheInstance.GetImage(path + "release.png");
            blankImage = Repository.TheInstance.GetImage(path + "blank.png");

            iconLabel = new Label();
            iconLabel.BackColorChanged += IconColorChanged;

            iconWidth = 10;

            requestDisplay = new Label();
            requestDisplay.TextChanged += TextChange;

            if (demandId.Equals(string.Empty))
            {
                requestDisplay.Text = newServiceName;
            }
            else
            {
                requestDisplay.Text = "Demand " + demandId;
            }
            
            BackColor = Color.Transparent;
            ForeColor = Color.White;

            status = string.Empty;

            BasicLayout();

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                requestDisplay.Dispose();
                iconLabel.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (iconLabel.Image == null)
            {
                throw new Exception("Icon label null.");
            }
            base.OnPaint(e);
        }

        public bool IsStatusChanged(string newStatus)
        {
            if (!status.Equals(newStatus))
            {
                return true;
            }
            return false;
        }

	    void IconColorChanged(object sender, EventArgs eventArgs)
        {
            Invalidate();
        }

	    void TextChange(object sender, EventArgs eventArgs)
        {
            Invalidate();
        }

        public void BasicLayout()
        {
            this.Controls.Add(iconLabel);
            iconLabel.Size = new Size(iconWidth, Height);
            iconLabel.Location = new Point(10,10); 

            this.Controls.Add(requestDisplay);
            requestDisplay.Size = new Size(Width - 2*iconLabel.Width - widthPadding, Height);
            requestDisplay.Location = new Point(iconLabel.Right + widthPadding,heightPadding);
            requestDisplay.TextAlign = ContentAlignment.MiddleLeft;
            requestDisplay.Font = SkinningDefs.TheInstance.GetFont(9, FontStyle.Regular);
            
        }

        public void UpdateStatus(string newStatus)
        {
            status = newStatus;
            switch (newStatus)
            {
                case "dev":
                    iconLabel.Image = developingImage;
                    break;
                case "test":
                    iconLabel.Image = testImage;
                    break;
                case "release":
                    iconLabel.Image = releaseImage;
                    break;
                case "installing":
                    iconLabel.Image = transitionImage;
                    break;
                case "live":
                    iconLabel.Image = deployedImage;
                    break;
                default:
                    iconLabel.Image = blankImage;
                    break;
            }
        }


        public new void Dispose ()
        {
            if (iconLabel != null)
            {
                iconLabel.Dispose();
            }
            if (requestDisplay != null)
            {
                requestDisplay.Dispose();
            }

            base.Dispose();
        
        }
    }
}

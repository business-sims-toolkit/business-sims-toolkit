using System.Drawing;
using System.IO;
using LibCore;
using Network;
using System.Collections.Generic;

namespace TransitionScreens
{
    public class LogoPanelScrollingImage : LogoPanel_IBM
    {
	    List<Image> images;
	    int whichImageShowing = 0;

        public LogoPanelScrollingImage(NodeTree network) : base()
		{
            if (network != null)
            {
                Node timeNode = network.GetNamedNode("CurrentTime");
                timeNode.AttributesChanged += timeNode_AttributesChanged;
              
            }
            string foldername = CoreUtils.SkinningDefs.TheInstance.GetData("scrolling_image_folder_name");
            DirectoryInfo dir = new DirectoryInfo(AppInfo.TheInstance.Location + "\\images\\" + foldername);
            InitialiseImageList(dir);
            ChangeImage();

        }






        void timeNode_AttributesChanged(Node sender, System.Collections.ArrayList attrs)
        {
            foreach (AttributeValuePair avp in attrs)
            {
                if (avp.Attribute == "seconds" && CONVERT.ParseInt(avp.Value) % 60 == 0)
                {
                    ChangeImage();
                }
            }
        }

	    void InitialiseImageList(DirectoryInfo dir)
        {
            images = new List<Image>();


            FileInfo[] files = dir.GetFiles();

            foreach (var item in files)
            {
                FileStream stream = new FileStream(item.FullName, FileMode.Open, FileAccess.Read);
                Image Pic = Image.FromStream(stream);
                images.Add(Pic);

            }


        }

        public void ChangeImage()
        {
            if (whichImageShowing >= images.Count)
            {
                whichImageShowing = 0;
            }
            TeamLogoPicBox.Image = images[whichImageShowing];
            whichImageShowing++;
        }

        public override void BuildLogoContents()
        {
            //Need to fill the Team Logo 
            
            string FacilLogoPath = ImageDirectory + "\\global\\facil_logo.png";

           
            string DefFacLogoPath = AppInfo.TheInstance.Location + "\\images\\DefFacLogo.png";

        
            FacLogoPicBox.Image = GetIsolatedImage(FacilLogoPath, DefFacLogoPath);
        }


        

    }
}

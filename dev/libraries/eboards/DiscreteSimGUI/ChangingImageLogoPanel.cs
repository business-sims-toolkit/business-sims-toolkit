using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Network;
using LibCore;

namespace DiscreteSimGUI
{

    public class ChangingImageLogoPanel : LogoPanel
    {
	    List<Image> images;
	    int whichImageShowing = 0;
	    NodeTree _Network;


        public ChangingImageLogoPanel(Boolean IsTraining, string ImageDir, NodeTree nt, DirectoryInfo dir) :  base (IsTraining, ImageDir)
        {
            _Network = nt;
            Node timeNode = nt.GetNamedNode("CurrentTime");
            timeNode.AttributesChanged += timeNode_AttributesChanged;

            

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

        public void ChangeImage()
        {
            if (whichImageShowing >= images.Count)
            {
                whichImageShowing = 0;
            }
            this.TeamLogoPicBox.Image = images[whichImageShowing];
            whichImageShowing++;
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


    }
}

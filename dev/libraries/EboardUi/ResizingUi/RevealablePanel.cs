using System;
using System.Collections.Generic;
using System.Drawing;
using CommonGUI;
using Events;
using LibCore;

namespace ResizingUi
{
	public class RevealablePanel : SharedMouseEventControl
    {
	    readonly PicturePanel picture;
		string startFilename;
		string revealedFilename;
		bool revealed;

		public RevealablePanel ()
		{
			picture = new PicturePanel ();
			picture.Click += picture_Click;
			Controls.Add(picture);
            
			DoSize();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				picture.Dispose();
			}

			base.Dispose(disposing);
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize ()
		{
			picture.Bounds = new Rectangle (0, 0, Width, Height);
		}

		void picture_Click (object sender, EventArgs args)
		{
			Reveal(! revealed);
		}

		public void Reveal (bool reveal)
		{
			revealed = reveal;

			var imageFilename = (revealed ? revealedFilename : startFilename);
			if (imageFilename != null)
			{
				picture.ZoomWithLetterboxing(Repository.TheInstance.GetImage(imageFilename));
			}
		}

		public void LoadImages (string startFilename, string revealedFilename)
		{
			this.startFilename = startFilename;
			this.revealedFilename = revealedFilename;

			Reveal(false);
		}

		public bool Revealed => revealed;

	    //public override Dictionary<string, Rectangle> BoundIdsToRectangles =>
		   // new Dictionary<string, Rectangle> { { "revealable_all", ClientRectangle } };

	    public override List<KeyValuePair<string, Rectangle>> BoundIdsToRectangles =>
		    new List<KeyValuePair<string, Rectangle>>
		    {
			    new KeyValuePair<string, Rectangle>("revealable_all", RectangleToScreen(ClientRectangle))
		    };

	    public override void ReceiveMouseEvent (SharedMouseEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
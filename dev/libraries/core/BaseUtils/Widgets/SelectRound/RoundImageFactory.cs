using System;
using System.Drawing;
using LibCore;

namespace BaseUtils
{
	/// <summary>
	/// Factory for constructing Round images.
	/// </summary>
	public class RoundImageFactory
	{
		static RoundImageFactory fact;
		//private Bitmap[] bitmaps;
		//private Bitmap lockBitmap;
		string[] names;

		string[] DisplayNames;
		string RaceWord = "Race";


		/// <summary>
		/// Returns the one and only RoundImageFactory instance.
		/// </summary>
		/// <returns></returns>
		public static RoundImageFactory Instance()
		{
			if (fact == null)
			{
				fact = new RoundImageFactory();
			}
			return fact;
		}

		/// <summary>
		/// Creates an instance of the one and only RoundImageFactory.
		/// </summary>
		RoundImageFactory()
		{
			this.DisplayNames = new string[] {"Europe", "Asia", "Americas", "Australasia", "Africa"};
			try 
			{
				this.names = new string[] {"RoundSymbol1", "RoundSymbol2", "RoundSymbol3", "RoundSymbol4", "RoundSymbol5"};
			}
			catch(Exception evc)
			{
				string str1 = evc.Message;
			}
		}

		public void OverrideDisplayNames(string rw, string rnd1, string rnd2, string rnd3, string rnd4, string rnd5)
		{
			RaceWord = rw;
			if (DisplayNames != null)
			{
				if (DisplayNames.Length>4)
				{
					DisplayNames[0] = rnd1;
					DisplayNames[1] = rnd2;
					DisplayNames[2] = rnd3;
					DisplayNames[3] = rnd4;
					DisplayNames[4] = rnd5;
				}
			}
		}

		/// <summary>
		/// Constructs an image based on the specified round and locked flag.
		/// </summary>
		/// <param name="round">The round.</param>
		/// <param name="locked">The locked flag for the round.</param>
		/// <returns></returns>
		public Bitmap GetRoundImage(int round, bool locked)
		{
			string dir = AppInfo.TheInstance.Location;
			Image image = Repository.TheInstance.GetImage(dir + "\\images\\" + names[round-1] + ".png", Color.Pink);
			Bitmap bmp = new Bitmap(205, 102);
			Graphics g = Graphics.FromImage(bmp);

			g.Clear(Color.White);
			g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
			g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

			g.DrawImage(image, 5, 5, 133, 102);
			g.DrawString(DisplayNames[round-1], ConstantSizeFont.NewFont("Tahoma", 10f, FontStyle.Bold), Brushes.Black, 6, 6);
			g.DrawString(String.Format(RaceWord+" {0}", round), ConstantSizeFont.NewFont("Tahoma", 14f, FontStyle.Bold), Brushes.Black, 125-10, 82);

			if (locked)
			{
				Image image2 = Repository.TheInstance.GetImage(dir + "\\images\\" + "lock.png", Color.Green);
				g.DrawImage(image2, 125, 58, 24, 24);
			}

			return bmp;
		}
	}
}

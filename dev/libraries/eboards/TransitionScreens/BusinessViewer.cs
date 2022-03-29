using System;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using CommonGUI;
using CoreUtils;
using ResizingUi;

namespace TransitionScreens
{
	/// <summary>
	/// Summary description for FlashViewer
	/// </summary>
	public class BusinessViewer : FlickerFreePanel
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		System.ComponentModel.Container components = null;

		bool isTrainingGame= false;

		bool SelfDrawTranslatedTitle = false;
		string panelTitle = "Business";
		Font titleFont = null;
		bool auto_translate = true;
		
		VideoBoxFlashReplacement businessFlash;
		PicturePanel businessImage;

		/// <summary>
		/// 
		/// </summary>
		public BusinessViewer(bool isTrainingGameFlag)
		{
			isTrainingGame = isTrainingGameFlag;

			BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
				"\\images\\panels\\business.png");
			SetTrainingMode(isTrainingGame);

			string fontname =  SkinningDefs.TheInstance.GetData("fontname");
			titleFont = ConstantSizeFont.NewFont(fontname,12f);
			if (auto_translate)
			{
				titleFont.Dispose();
				titleFont = ConstantSizeFont.NewFont(TextTranslator.TheInstance.GetTranslateFont(fontname), 12f, FontStyle.Bold);
			}

			SuspendLayout();


			SuspendLayout();

			businessFlash = new VideoBoxFlashReplacement ();
			Controls.Add(businessFlash);
			
			businessImage = new PicturePanel ();
			Controls.Add(businessImage);
			businessImage.BringToFront();
			InitialiseStaticBusinessFlash();

			ResumeLayout(false);

			DoSize();
		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (businessFlash != null)
				{
					businessFlash.Dispose();
					businessFlash = null;
				}

				if (titleFont != null)
				{
					titleFont.Dispose();
				}

				if(components != null)
				{
					components.Dispose();
				}
			}

			base.Dispose( disposing );
		}

		public void InitialiseStaticBusinessFlash ()
		{
			if (businessFlash != null)
			{
				if (isTrainingGame)
				{
					businessImage.ZoomWithCropping(Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + "\\images\\panels\\traininggamepanel.png"));
					businessImage.Show();
				}
				else
				{
					businessImage.Hide();
				}

				businessFlash.Play();
			}
		}

		public void start()
		{
			if (businessFlash != null)
			{
				if (isTrainingGame)
				{
					businessImage.Show();
				}
				else
				{
					businessImage.Hide();
					businessFlash.Play();
				}
			}
		} 

		public void stop()
		{
			//businessFlash.Stop();
			InitialiseStaticBusinessFlash();
		} 

		public void EnableSelfDrawTitle(bool newState)
		{
			SelfDrawTranslatedTitle = newState;
		}

		public void SetTrainingMode(Boolean Tr)
		{
			isTrainingGame = Tr;
			if (Tr)
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
					"\\images\\panels\\t_business.png");
			}
			else
			{
				BackgroundImage = Repository.TheInstance.GetImage(AppInfo.TheInstance.Location + 
					"\\images\\panels\\business.png");
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics g = e.Graphics;
			if (SelfDrawTranslatedTitle)
			{
				string title_text = panelTitle;
				if (auto_translate)
				{
					title_text = TextTranslator.TheInstance.Translate(title_text);
					g.DrawString(title_text, titleFont, Brushes.DarkBlue,10,0);
				}
			}
		}

		public void DisableFlash ()
		{
			if (businessFlash != null)
			{
				Controls.Remove(businessFlash);
				businessFlash.Dispose();
				businessFlash = null;
			}

			if (businessImage != null)
			{
				businessImage.Hide();
			}
		}

		public void HideTitle ()
		{
			panelTitle = "";
			Invalidate();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			DoSize();
		}

		void DoSize ()
		{
			if (businessFlash != null)
			{
				businessFlash.Bounds = new Rectangle(0, 0, Width, Height);
			}

			if (businessImage != null)
			{
				businessImage.Bounds = new Rectangle(0, 0, Width, Height);
			}
		}
	}
}
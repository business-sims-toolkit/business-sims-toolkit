using System;
using System.Drawing;
using System.Windows.Forms;

using Network;

namespace GameBoardView
{
	public class GameBoardViewWithController : Panel
	{
		ZoomControlPanel controlPanel;
		GameBoardView boardView;

		public GameBoardViewWithController (NodeTree model)
			: this (model, LibCore.AppInfo.TheInstance.Location + @"\data\board_new.xml", LibCore.AppInfo.TheInstance.Location)
		{
		}

		public GameBoardViewWithController (NodeTree model, string boardXmlFilename, string imageParentPath)
		{
			boardView = new GameBoardView (model, boardXmlFilename, imageParentPath);
			boardView.IconMouseDown += boardView_IconMouseDown;
			Controls.Add(boardView);

			controlPanel = new ZoomControlPanel (boardView.Zones);
			controlPanel.ZoomSelected += controlPanel_ZoomSelected;
			Controls.Add(controlPanel);

			DoSize();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				boardView.Dispose();
				controlPanel.Dispose();
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
			controlPanel.Location = new Point (0, 0);
			controlPanel.Size = new Size (Width, 50);

			boardView.Location = new Point (0, controlPanel.Bottom);
			boardView.Size = new Size (Width, Height - boardView.Top);
		}

		void controlPanel_ZoomSelected (ZoomControlPanel sender, ZoomControlPanel.ZoomSelectedArgs args)
		{
			boardView.ZoomToZone(args.Zone);
		}

		public void ReadNetwork ()
		{
			boardView.ReadNetwork();
		}

		public void ReadNetwork (NodeTree model)
		{
			boardView.ReadNetwork(model);
		}

		public void ResetView ()
		{
			boardView.ResetView();
		}

		public event GameBoardView.IconMouseDownHandler IconMouseDown;

		void boardView_IconMouseDown (object sender, GameBoardView.IconMouseDownEventArgs args)
		{
			OnIconMouseDown(args);
		}

		void OnIconMouseDown (GameBoardView.IconMouseDownEventArgs args)
		{
			if (IconMouseDown != null)
			{
				IconMouseDown(this, args);
			}
		}

		public void SetLocationPosition (Node location, Point position, PointF grabOffset)
		{
			boardView.SetLocationPosition(location, position, grabOffset);
		}

		public void SaveBoardXml (string filename)
		{
			boardView.SaveBoardXml(filename);
		}

		public bool ShowLocationLabels
		{
			get
			{
				return boardView.ShowLocationLabels;
			}

			set
			{
				boardView.ShowLocationLabels = value;
			}
		}

		public Node HighlightLocation
		{
			get
			{
				return boardView.HighlightLocation;
			}

			set
			{
				boardView.HighlightLocation = value;
			}
		}

		public bool ShowHidden
		{
			get
			{
				return boardView.ShowHidden;
			}

			set
			{
				boardView.ShowHidden = value;
			}
		}
	}
}
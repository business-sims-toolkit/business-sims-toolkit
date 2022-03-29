using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

using CommonGUI;
using Network;
using LibCore;
using CoreUtils;
using Events;
using DevOps.OpsEngine;

namespace DevOps.OpsScreen
{
	public class NewServicesDisplay : FlickerFreePanel
	{
	    readonly NodeTree model;
	    readonly Node beginInstallNode;

		bool enabled;

	    readonly Dictionary<Node, NewServiceIcon> serviceToIcon;		

	    readonly RequestsManager requestsManager;
        

		List<Point> availableLocations = new List<Point>();
        
		int iconWidth = 50;
		int iconHeight = 50;
		int borderThickness = 5;

		int widthPadding = 44;
		float innerWidthPadding;

		int heightPadding = 56;
		float innerHeightPadding;

	    public event EventHandler NumberOfServicesChanged;
	    public event EventHandler BlankServiceIconClicked;
	    public event EventHandler<EventArgs<Node>> ServiceIconClicked;
	    public event EventHandler ServiceRemoved;

		IWatermarker watermarker;

		public IWatermarker Watermarker
		{
			get => watermarker;

			set
			{
				watermarker = value;
				Invalidate();
			}
		}

		public int NumberOfRemainingLocations => availableLocations.Count;

	    public int MaxNumNewServices { get; set; }

	    readonly int round;

		int numServicesHorizontally = 6;

		public int NumServicesHorizontally
		{
			get => numServicesHorizontally;

		    set
			{
				if (MaxNumNewServices % value != 0)
				{
					throw new Exception("Number of services across needs to divide into the total number of services.");
				}

				numServicesHorizontally = value;
				numServicesVertically = MaxNumNewServices / numServicesHorizontally;
			}
		}

		int numServicesVertically = 4;

		public int NumServicesVertically
		{
			get => numServicesVertically;
		    set
			{
				if (MaxNumNewServices % value != 0)
				{
					throw new Exception("Number of services down needs to divide into the total number of services.");
				}

				numServicesVertically = value;
				numServicesHorizontally = MaxNumNewServices / numServicesVertically;
			}
		}

	    readonly List<NewServiceIcon> blankIcons;

		public NewServicesDisplay (NodeTree model, RequestsManager requestsManager, int round)
		{
			enabled = true;

			this.model = model;
			this.requestsManager = requestsManager;
			this.round = round;

			MaxNumNewServices = SkinningDefs.TheInstance.GetIntData("max_new_services", 24);

			beginInstallNode = model.GetNamedNode("BeginNewServicesInstall");
			beginInstallNode.ChildAdded += beginInstallNode_ChildAdded;
			beginInstallNode.ChildRemoved += beginInstallNode_ChildRemoved;

			serviceToIcon = new Dictionary<Node, NewServiceIcon>();
			foreach (Node service in beginInstallNode.GetChildrenOfType("BeginNewServicesInstall"))
			{
				AddService(service);
			}

			blankIcons = new List<NewServiceIcon>();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			CreateLocations();
			Invalidate();
		}

		protected void CreateLocations ()
		{
			availableLocations.Clear();

			iconWidth = Math.Min(Width / 12, Height / 5);
			iconHeight = iconWidth;

			widthPadding = iconWidth;
			heightPadding = iconHeight * 3 / 4;

			var subWidth = (Width - 2 * widthPadding);
			var totalWidthAllIcons = (numServicesHorizontally * iconWidth);
			innerWidthPadding = (subWidth - totalWidthAllIcons) / (float) (numServicesHorizontally - 1);

			var subHeight = (Height - 2 * heightPadding);
			var totalHeightAllIcons = (numServicesVertically * iconHeight);
			innerHeightPadding = (subHeight - totalHeightAllIcons) / (float) (numServicesVertically - 1);

			for (var row = 0; row < numServicesVertically; row++)
			{
				for (var col = 0; col < numServicesHorizontally; col++)
				{
					var x = (int) ((iconWidth + innerWidthPadding) * col) + widthPadding;

					var y = (int) ((iconHeight + innerHeightPadding) * row) + heightPadding;

					availableLocations.Add(new Point(x, y));
				}
			}

			// Rather than trying to resize the list just remove them
			// all and readd them. So wasteful ... 

			foreach (var img in blankIcons)
			{
				Controls.Remove(img);
			}

			blankIcons.Clear();

			// Add a blank image icon at every location.
			foreach (var position in availableLocations)
			{
				var blank = new NewServiceIcon(null)
				{
					Size = new Size(iconWidth, iconHeight),
					Location = position,
					ImageLocation = AppInfo.TheInstance.Location + @"\images\icons\",
                    BackColor = Color.Transparent
				};

			    blank.Click += blank_Click;

                Controls.Add(blank);
				blankIcons.Add(blank);
			}


			foreach (var icon in serviceToIcon.Values)
			{
				icon.Location = availableLocations[0];
				icon.Size = new Size (iconWidth, iconHeight);
				availableLocations.RemoveAt(0);
			}
		}
        

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				beginInstallNode.ChildAdded -= beginInstallNode_ChildAdded;
				beginInstallNode.ChildRemoved -= beginInstallNode_ChildRemoved;
			}

			base.Dispose(disposing);
		}

		void AddService (Node service)
		{
			Debug.Assert(availableLocations.Count > 0, "No positions available.");

			var firstPosition = availableLocations[0];
			availableLocations.RemoveAt(0);

			OnNumberOfServicesChanged();

			var panel = new NewServiceIcon(service)
			{
				ImageLocation = AppInfo.TheInstance.Location + @"\images\icons\",
				BorderThickness = borderThickness,
				Tag = service,
				Name = service.GetAttribute("icon"),
				Size = new Size(iconWidth, iconHeight),
				Location = firstPosition,
			    BackColor = Color.Transparent
            };
			panel.Click += panel_Click;
			serviceToIcon.Add(service, panel);

			panel.Visible = ! service.GetBooleanAttribute("is_auto_installed", false);

			Controls.Add(panel);
			panel.BringToFront();

			Invalidate();
		}

		void RemoveService (Node service)
		{
			var icon = serviceToIcon[service];

			availableLocations.Add(icon.Location);
			// LINQ expression to order the Points by y (ascending) then by x (ascending)
			// The list is sorted after a position is returned to it so that new services,
			// when added, will be placed in a (relative) order.
			availableLocations = availableLocations.OrderBy(p => p.Y).ThenBy(p => p.X).ToList();

			OnNumberOfServicesChanged();

			Controls.Remove(icon);

			serviceToIcon.Remove(service);
            
            OnServiceRemoved();

			Invalidate();
		}
        
	    void blank_Click (object sender, EventArgs args)
	    {
	        OnBlankServiceIconClicked();
        }

        void panel_Click (object sender, EventArgs args)
		{
			if (enabled)
			{
			    var icon = (Control)sender;
                OnServiceIconClicked((Node)icon.Tag);
			}
		}

		void beginInstallNode_ChildAdded (Node sender, Node child)
		{
			AddService(child);
		}

		void beginInstallNode_ChildRemoved (Node sender, Node child)
		{
			RemoveService(child);
		}

	    void OnNumberOfServicesChanged()
	    {
	        NumberOfServicesChanged?.Invoke(this, EventArgs.Empty);
	    }

	    void OnBlankServiceIconClicked()
	    {
	        BlankServiceIconClicked?.Invoke(this, EventArgs.Empty);
	    }

	    void OnServiceIconClicked (Node serviceNode)
	    {
	        ServiceIconClicked?.Invoke(this, ServiceIconClicked.CreateArgs(serviceNode));
	    }

	    void OnServiceRemoved ()
	    {
            ServiceRemoved?.Invoke(this, EventArgs.Empty);
	    }

        public void DisableButtons ()
		{
			enabled = false;
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

			using (var brush = new SolidBrush(Color.FromArgb(239, 239, 239)))
			{
				e.Graphics.FillRectangle(brush, 0, 0, Width, Height);
			}

			watermarker?.Draw(this, e.Graphics);

			var positions = new List<Point> ();
			positions.AddRange(availableLocations);
			positions.AddRange(serviceToIcon.Values.Select(icon => icon.Location));

			foreach (var position in positions)
			{
				using (var path = new GraphicsPath ())
				{
					const float shadowAngle = (float)(60 * Math.PI / 180);
					const int shadowLength = 80;
					const int gradientLength = shadowLength + 2;

					var centre = new Point (position.X + (iconWidth / 2), position.Y + (iconHeight / 2));
					const double shadowPerpendicularAngle = shadowAngle + (Math.PI / 2);
					var leftShadowTip = new Point((int) (centre.X - (iconWidth * Math.Cos(shadowPerpendicularAngle) / 2)), (int) (centre.Y - (iconHeight * Math.Sin(shadowPerpendicularAngle)) / 2));
					var rightShadowTip = new Point((int) (centre.X + (iconWidth * Math.Cos(shadowPerpendicularAngle) / 2)), (int) (centre.Y + (iconHeight * Math.Sin(shadowPerpendicularAngle)) / 2));

					var gradientBoundPoints = new []
					{
						leftShadowTip,
						rightShadowTip,
						new Point ((int) (rightShadowTip.X + (shadowLength * Math.Cos(shadowAngle))), (int) (rightShadowTip.Y + (gradientLength * Math.Sin(shadowAngle)))),
						new Point ((int) (leftShadowTip.X + (shadowLength * Math.Cos(shadowAngle))), (int) (leftShadowTip.Y + (gradientLength * Math.Sin(shadowAngle)))),
					};

					var shadowBoundPoints = new[]
					{
						leftShadowTip,
						rightShadowTip,
						new Point ((int) (rightShadowTip.X + (shadowLength * Math.Cos(shadowAngle))), (int) (rightShadowTip.Y + (shadowLength * Math.Sin(shadowAngle)))),
						new Point ((int) (leftShadowTip.X + (shadowLength * Math.Cos(shadowAngle))), (int) (leftShadowTip.Y + (shadowLength * Math.Sin(shadowAngle)))),
					};

					path.AddLines(shadowBoundPoints);

					using (var brush = new LinearGradientBrush(gradientBoundPoints[0], gradientBoundPoints[3], Color.FromArgb(80, 0, 0, 0), Color.FromArgb(0, 0, 0, 0)))
					{
						e.Graphics.FillPath(brush, path);
					}
				}

				var service = serviceToIcon.Keys.FirstOrDefault(k => (serviceToIcon[k].Location == position));
				if ((service != null)
					&& ! service.GetBooleanAttribute("is_auto_installed", false))

				{
					var bounds = serviceToIcon[service].Bounds;

					using (var brush = new SolidBrush(Color.FromArgb(255, 92, 92, 92)))
					{
						using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(iconHeight / 5f, FontStyle.Bold))
						{
							e.Graphics.DrawString(service.GetAttribute("service_id"), font, brush,
								new RectangleF (bounds.Left, bounds.Bottom, bounds.Width, iconHeight / 4f),
								new StringFormat { LineAlignment = StringAlignment.Near, Alignment = StringAlignment.Center });
						}
					}
				}
			}
		}
	}
}
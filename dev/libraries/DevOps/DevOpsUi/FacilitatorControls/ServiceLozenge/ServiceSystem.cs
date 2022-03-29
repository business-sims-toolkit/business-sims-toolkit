using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DevOpsEngine;
using DevOpsEngine.RequestsManagers;
using LibCore;
using Network;
using ResizingUi;
using ResizingUi.Animation;
using ResizingUi.Enums;
using ResizingUi.Interfaces;

namespace DevOpsUi.FacilitatorControls.ServiceLozenge
{
	public class ServiceSystem : CascadedBackgroundPanel
	{
		readonly AgileRequestsManager requestsManager;
		readonly ContainerCustomBackground backgroundPanel;
		readonly bool backgroundPanelIsOurs;
		public ServiceSystem (NodeTree model, int maxModules,
		                      DevelopingAppTerminator appTerminator, Dictionary<int, string> columnToServiceName,
		                      IDialogOpener dialogOpener, AgileRequestsManager requestsManager, bool includeHeatMap, 
		                      bool includeCsatReadout, ContainerCustomBackground backgroundPanel)
							// TODO drawTop/BottomReticuleCorners, includeSummaryPanel
		{
			this.requestsManager = requestsManager;

			if (backgroundPanel == null)
			{
				backgroundPanel = new ContainerCustomBackground();
				Controls.Add(backgroundPanel);
				backgroundPanelIsOurs = true;
			}
			else
			{
				backgroundPanelIsOurs = false;
			}

			this.backgroundPanel = backgroundPanel;

			this.maxModules = maxModules;

			var orderedColumns = columnToServiceName.Keys.OrderBy(c => c).ToList();

			if (orderedColumns.Count > maxModules)
			{
				orderedColumns = orderedColumns.Take(maxModules).ToList();
			}

			serviceLozenges = new List<ServiceLozengePanel> ();
			var animatorProvider = new AnimatorProvider ();

			var minColumn = orderedColumns.Min();
			var maxColumn = orderedColumns.Max();

			foreach (var column in orderedColumns)
			{
				var borderedSides = new List<RectangleSides>();

				// The inner columns draw the left and right sides as borders
				// the first and last columns don't draw either sides as borders

				if (column < maxColumn)
				{
					borderedSides.Add(RectangleSides.Right);
				}

				var serviceLozenge = new ServiceLozengePanel(column, columnToServiceName[column], model, dialogOpener, appTerminator, requestsManager, 
					includeHeatMap, includeCsatReadout, animatorProvider, borderedSides)
				{
					BackColor = CONVERT.ParseHtmlColor("#ffffff")
				};

				serviceLozenge.UseCustomBackground(backgroundPanel);

				serviceLozenges.Add(serviceLozenge);
				Controls.Add(serviceLozenge);
			}
			backgroundPanel.SendToBack();
		}

		public List<ServiceLozengePanel> ServiceLozenges => serviceLozenges;

		int rowSeparation;

		public int RowSeparation
		{
			get => rowSeparation;
			set
			{
				rowSeparation = value;
				Invalidate();
			}
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			DoSize();
		}

		void DoSize ()
		{
			if (backgroundPanelIsOurs)
			{
				backgroundPanel.Bounds = new Rectangle(0, 0, Width, Height);
			}

			const int innerPadding = 0;
			var lozengeHeight = Height - rowSeparation;

			var y = (Height - lozengeHeight) / 2;

			var width = Width - (maxModules - 1) * innerPadding;
			var lozengeWidth = width / maxModules;

			foreach (var lozenge in serviceLozenges)
			{
				var lozengeX = lozenge.ColumnIndex * (lozengeWidth + innerPadding);
				lozenge.Bounds = new Rectangle(lozengeX, y, lozengeWidth, lozengeHeight);
			}
		}

		readonly int maxModules;
		readonly List<ServiceLozengePanel> serviceLozenges;

		// TODO include summary panel, and flag if is top and/or bottom row
		// to be reused with GAgile/Lloyds
		public void OpenFeaturePopup (Node feature)
		{
			var service = requestsManager.GetServiceByFeature(feature);
			var serviceIndex = service.GetIntAttribute("problem_statement_column", 0) - 1;
			serviceLozenges[serviceIndex].OpenFeaturePopup(feature);
		}
	}
}
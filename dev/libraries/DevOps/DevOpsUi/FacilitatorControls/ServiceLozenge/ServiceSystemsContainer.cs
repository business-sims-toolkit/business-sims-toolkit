using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using CommonGUI;
using CoreUtils;
using DevOpsEngine;
using DevOpsEngine.RequestsManagers;
using GameManagement;
using LibCore;
using Network;
using ResizingUi.Interfaces;

namespace DevOpsUi.FacilitatorControls.ServiceLozenge
{
	public class ServiceSystemsContainer : FlickerFreePanel
	{
		readonly ContainerCustomBackground backgroundPanel;
		readonly AgileRequestsManager requestsManager;

		public ServiceSystemsContainer (NetworkProgressionGameFile gameFile, NodeTree model, int round, DevelopingAppTerminator appTerminator,
		                                IDialogOpener dialogOpener, AgileRequestsManager requestsManager)
		{
			this.requestsManager = requestsManager;

			var useCustomBackground = SkinningDefs.TheInstance.GetBoolData("use_container_custom_background", false);
			if (useCustomBackground)
			{
				backgroundPanel = new ContainerCustomBackground(gameFile.GetOption("selected_background_image"));
				Controls.Add(backgroundPanel);
			}

			var systemsParentNodeName = SkinningDefs.TheInstance.GetData("system_parent_node_name", "ProblemStatements");
			var systemsTypeName = SkinningDefs.TheInstance.GetData("system_type_name", "ProblemStatement");
			var systemNumberAttribute = SkinningDefs.TheInstance.GetData("system_number_attribute", "number");

			var systemColumnIdentifier = SkinningDefs.TheInstance.GetData("system_column_identifier", "problem_statement_column");
			var systemIdentifier = SkinningDefs.TheInstance.GetData("system_identifier", "problem_statement");

			var systemNodes = model.GetNamedNode(systemsParentNodeName).GetChildrenWithAttributeValue("type", systemsTypeName).OrderBy(n => n.GetIntAttribute(systemNumberAttribute, 0)).ToList();
			
			serviceSystemRows = new List<ServiceSystem>();

			var bizServices = model.GetNamedNode("Business Services Group").GetChildrenAsList()
				.Where(bs => bs.GetIntAttribute("round", 0) == 0).ToList();

			var newServicesForThisRound = model.GetNamedNode("New Services").GetChildrenAsList()
				.Single(nsr => nsr.GetIntAttribute("round", 0) == round).GetChildrenAsList();

			var maxColumns = Math.Max(bizServices.Select(bs => bs.GetIntAttribute(systemColumnIdentifier, 0)).Max(),
				newServicesForThisRound.Select(ns => ns.GetIntAttribute(systemColumnIdentifier, 0)).Max());

			foreach (var systemNode in systemNodes)
			{
				var systemNumber = systemNode.GetIntAttribute(systemNumberAttribute, 0);
				var columnIndexToServiceName = bizServices
					.Where(bs => bs.GetIntAttribute(systemIdentifier, 0) == systemNumber).ToDictionary(
						bs => bs.GetIntAttribute(systemColumnIdentifier, 0),
						bs => bs.GetAttribute("biz_service_function"));

				foreach (var newService in newServicesForThisRound.Where(ns => ns.GetIntAttribute(systemIdentifier, 0) == systemNumber)
					.Select(ns => 
						new
						{
							Column = ns.GetIntAttribute(systemColumnIdentifier, 0),
							Name = ns.GetAttribute("biz_service_function")
						}
					))
				{
					if (! columnIndexToServiceName.ContainsKey(newService.Column))
					{
						columnIndexToServiceName[newService.Column] = newService.Name;
					}
				}

				var includeHeatMap = CONVERT.ParseBool(gameFile.GetOption("heat_map_available", "true")).Value;
				var includeCsatReadout = ! includeHeatMap;

				var systemRow = new ServiceSystem(model, maxColumns, appTerminator, columnIndexToServiceName, dialogOpener, requestsManager, includeHeatMap, includeCsatReadout, backgroundPanel)
				{
					BackColor = CONVERT.ParseHtmlColor("#989898"),
					RowSeparation = 0 // TODO
				};

				Controls.Add(systemRow);
				serviceSystemRows.Add(systemRow);
				systemRow.BringToFront();
			}
		}

		public List<ServiceSystem> ServiceSystems => serviceSystemRows;


		protected override void OnSizeChanged (EventArgs e)
		{
			DoSize();
		}
		void DoSize ()
		{
			if (backgroundPanel != null)
			{
				backgroundPanel.Bounds = new Rectangle(0, 0, Width, Height);
			}

			if (!serviceSystemRows.Any())
			{
				return;
			}

			var rowHeight = Height / (float) serviceSystemRows.Count;
			var y = 0f;

			foreach (var row in serviceSystemRows)
			{
				row.Bounds = new Rectangle(0, (int) y, Width, (int) rowHeight);
				y = row.Bottom;
			}

			// TODO if summaryPanel included
		}

		readonly List<ServiceSystem> serviceSystemRows;

		public void OpenFeaturePopup (Node feature)
		{
			var service = requestsManager.GetServiceByFeature(feature);
			var rowIndex = service.GetIntAttribute("problem_statement", 0) - 1;
			var row = serviceSystemRows[rowIndex];
			row.OpenFeaturePopup(feature);
		}
	}
}
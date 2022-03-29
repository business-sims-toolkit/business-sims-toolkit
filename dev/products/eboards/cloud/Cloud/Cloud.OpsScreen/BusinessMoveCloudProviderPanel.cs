using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using LibCore;
using CoreUtils;

using Network;

using Cloud.OpsEngine;

namespace Cloud.OpsScreen
{
	public class BusinessMoveCloudProviderPanel : FlickerFreePanel
	{
		NodeTree model;
		OrderPlanner orderPlanner;
		Font Font_SubTitle;
		Label lbl_IAAS_SubTitle;
		Label lbl_SAAS_SubTitle;
		int button_row_gap = 10;

		public BusinessMoveCloudProviderPanel (OrderPlanner orderPlanner, NodeTree model, Node business, PublicVendorManager cloudVendorManager)
		{
			BackColor = Color.Transparent;

			Node roundVariables = model.GetNamedNode("RoundVariables");
			int currentRound = roundVariables.GetIntAttribute("current_round", 0);

			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_SubTitle = FontRepository.GetFont(font, 12, FontStyle.Bold);

			this.orderPlanner = orderPlanner;
			this.model = model;

			int round = model.GetNamedNode("RoundVariables").GetIntAttribute("current_round", 0);

			List<Node> iaasServices = new List<Node> ();
			List<Node> saasServices = new List<Node> ();

			foreach (Node businessService in business.GetChildrenOfType("business_service"))
			{
				Node location = orderPlanner.GetCurrentCloudDeploymentLocationIfAny(businessService);

				if (location != null)
				{
					if (location.GetBooleanAttribute("iaas", false))
					{
						iaasServices.Add(businessService);
					}
					else if (location.GetBooleanAttribute("saas", false))
					{
						if (cloudVendorManager.DoesSaaSServiceChangePriceThisRound(businessService))
						{
							saasServices.Add(businessService);
						}
					}
				}
			}

			int pos_x = 10;
			int pos_y = 0;

			//Add the Sub Title 
			lbl_IAAS_SubTitle = new Label();
			lbl_IAAS_SubTitle.ForeColor = Color.FromArgb(64, 64, 64);
			lbl_IAAS_SubTitle.Location = new Point(10, pos_y);
			lbl_IAAS_SubTitle.Size = new Size(220, 20);
			lbl_IAAS_SubTitle.Text = "IaaS";
			lbl_IAAS_SubTitle.Font = Font_SubTitle;
			lbl_IAAS_SubTitle.Visible = true;
			Controls.Add(lbl_IAAS_SubTitle);
			pos_y += lbl_IAAS_SubTitle.Height;

			//Add the Sub Title 
			pos_y = AddRow("IaaS services", iaasServices, false, pos_x, pos_y);

			//Add the Sub Title 
			lbl_SAAS_SubTitle = new Label();
			lbl_SAAS_SubTitle.ForeColor = Color.FromArgb(64, 64, 64);
			lbl_SAAS_SubTitle.Location = new Point(10, pos_y);
			lbl_SAAS_SubTitle.Size = new Size(220, 25);
			lbl_SAAS_SubTitle.Text = "SaaS";
			lbl_SAAS_SubTitle.Font = Font_SubTitle;
			lbl_SAAS_SubTitle.Visible = true;
			Controls.Add(lbl_SAAS_SubTitle);
			pos_y += lbl_SAAS_SubTitle.Height;

			foreach (Node businessService in saasServices)
			{
				List<Node> serviceList = new List<Node> ();
				serviceList.Add(businessService);
				pos_y = AddRow(businessService.GetAttribute("desc"), serviceList, true, pos_x, pos_y);
			}
		}

		int AddRow (string header, List<Node> services, bool isSaaS, int x, int y)
		{
			Label label = new Label ();
			label.Text = header;
			label.Size = new Size (280, 25);
			label.TextAlign = ContentAlignment.MiddleLeft;
			label.Location = new Point(x, y);
			Controls.Add(label);

			CloudProviderSelectionPanel panel = new CloudProviderSelectionPanel (orderPlanner, model, services, isSaaS);
			panel.Location = new Point (label.Right + 15, label.Top);
			panel.Size = new Size (300, label.Height);
			Controls.Add(panel);

			return label.Bottom + button_row_gap;
		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

using CommonGUI;
using ChartScreens;
using ReportBuilder;
using LibCore;
using Network;
using Charts;
using Logging;
using GameManagement;
using CoreUtils;
using Polestar_PM.OpsGUI;
using Polestar_PM.DataLookup;

namespace Polestar_PM.ReportsScreen
{
	/// <summary>
	/// This is the container for show the Project Performance Dual Chart 
	/// It contains the chart itself and the buttons for selecting the different projects and chart type
	/// </summary>
	public class PrjPerfContainer :FlickerFreePanel
	{
		NodeTree MyNodeTree = null;
		Node projectsNode = null;
		PrjPerfControl ppd_PrjPerfControl = null;

		PMNetworkProgressionGameFile gameFile;

		int round = 1;
		int projectslot_id = 0;
		emProjectPerfChartType RequiredChartType = emProjectPerfChartType.BOTH;
		RadioButton allCostsRadioButton;

		Dictionary<int, System.Windows.Forms.RadioButton> rbPrjPerf_Projects;

		ComboBox cmbChartType;
		int max_slots = 6;

		public PrjPerfContainer(PMNetworkProgressionGameFile gameFile, NodeTree model, int round)
		{
			this.BackColor = Color.White;
			MyNodeTree = model;
			this.round = round;
			this.gameFile = gameFile;
			projectsNode = MyNodeTree.GetNamedNode("pm_projects_running");
			bool display_seven_projects = projectsNode.GetBooleanAttribute("display_seven_projects", false);

			max_slots = 6;
			if (display_seven_projects)
			{
				max_slots = 7;
			}

			BuildControls();
			Fill_ProjectData();
		}

		new public void Dispose()
		{
			MyNodeTree = null;
			projectsNode = null;
			DisposeControls();
		}


		public void BuildControls()
		{
			ppd_PrjPerfControl = new PrjPerfControl(gameFile, MyNodeTree, round, max_slots, -1, max_slots);
			ppd_PrjPerfControl.SetProjectNumber(0);
			ppd_PrjPerfControl.Size = new Size(Width, 556);
			ppd_PrjPerfControl.Location = new Point(0, 50);
			this.Controls.Add(ppd_PrjPerfControl);

			rbPrjPerf_Projects = new Dictionary<int,RadioButton> ();

			for (int step = 0; step < (1 + max_slots); step++)
			{
				System.Windows.Forms.RadioButton rb = new System.Windows.Forms.RadioButton();

				rb.Appearance = System.Windows.Forms.Appearance.Button;
				rb.FlatStyle = System.Windows.Forms.FlatStyle.System;
				rb.Location = new System.Drawing.Point(220+(step)*60, 10);
				
				rb.Name = "rbPrjPerf_Project"+step.ToString();
				rb.Size = new System.Drawing.Size(56, 19);
				rb.TabIndex = step+1;
				rb.Text = "";
				rb.Tag = step;
				rb.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
				rb.CheckedChanged += new System.EventHandler(this.rbPrjPerf_Project_CheckedChanged);
				this.Controls.Add(rb);
				rbPrjPerf_Projects.Add(step, rb);

				if (step == max_slots)
				{
					allCostsRadioButton = rb;
				}
			}

			//=====================================================
			//==The different type of Charts======================= 
			//=====================================================
			cmbChartType = new ComboBox();
			cmbChartType.Items.Add("Gantt Chart");
			cmbChartType.Items.Add("Cost Chart");
			cmbChartType.Items.Add("Both Charts");
			cmbChartType.Location = new Point(720, 10);
			cmbChartType.DropDownStyle = ComboBoxStyle.DropDownList;
			this.Controls.Add(cmbChartType);
			cmbChartType.SelectedIndexChanged += new EventHandler(cmbChartType_SelectedIndexChanged);
			cmbChartType.SelectedIndex = 2;
		}

		public void DisposeControls()
		{
			if (ppd_PrjPerfControl != null)
			{
				this.Controls.Remove(ppd_PrjPerfControl);
				ppd_PrjPerfControl.Dispose();
			}

			ArrayList kill_list = new ArrayList();

			foreach (System.Windows.Forms.RadioButton rb in rbPrjPerf_Projects.Values)
			{
				kill_list.Add(rb);
			}
			foreach (System.Windows.Forms.RadioButton rb in kill_list)
			{
				this.Controls.Remove(rb);
				rb.CheckedChanged -= new System.EventHandler(this.rbPrjPerf_Project_CheckedChanged);
				rb.Dispose();
			}
			rbPrjPerf_Projects.Clear();
		}


		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			ppd_PrjPerfControl.Size = new Size(Width, 556);

			cmbChartType.Location = new Point (Width - cmbChartType.Width - 12, 2);

			Control [] controls = new Control [rbPrjPerf_Projects.Values.Count];
			int step=0;
			foreach (Control ct in rbPrjPerf_Projects.Values)
			{
				controls[step]=ct;
				step++;
			}
			DistributeControls(controls, 15, 175, cmbChartType.Left, 2);
		}

		void DistributeControls (Control [] controls, int gap, int left, int right, int y)
		{
			int width = 0;
			foreach (Control control in controls)
			{
				width += control.Width;
			}

			int x = (left + right - (width + (gap * (controls.Length - 1)))) / 2;
			foreach (Control control in controls)
			{
				control.Location = new Point (x, y);
				x += control.Width + gap;
			}
		}

		public void Fill_ProjectData()
		{
			int slot_id = 0;
			int project_id = 0;
			System.Windows.Forms.RadioButton tmpRB;


			//New Code 
			foreach (RadioButton button in rbPrjPerf_Projects.Values)
			{
				if (button != null)
				{
					button.Enabled = false;
				}
			}

			foreach (Node prjnode in this.projectsNode.getChildren())
			{
				if (prjnode != null)
				{
					ProjectReader pr = new ProjectReader(prjnode);
					slot_id = pr.getProjectSlot();
					project_id = pr.getProjectID();

					if (rbPrjPerf_Projects.ContainsKey(slot_id))
					{
						tmpRB = rbPrjPerf_Projects[slot_id];
						if (tmpRB != null)
						{
							tmpRB.Text = CONVERT.ToStr(project_id);
							tmpRB.Enabled = true;
						}
					}
					pr.Dispose();
				}
			}

			rbPrjPerf_Projects[max_slots].Text = "Total";
			rbPrjPerf_Projects[max_slots].Enabled = true;

			UpdateTotalButton();
			SelectFirstValidProject();
		}

		void SelectFirstValidProject ()
		{
			foreach (RadioButton button in rbPrjPerf_Projects.Values)
			{
				if (button.Enabled)
				{
					button.Checked = true;
					break;
				}
			}
		}
	
		public void RefreshScreen()
		{
			this.ppd_PrjPerfControl.SetProjectNumber(projectslot_id);
			this.ppd_PrjPerfControl.setChartType(RequiredChartType);
			this.ppd_PrjPerfControl.Refresh();
		}

		private void rbPrjPerf_Project_CheckedChanged(object sender, System.EventArgs e)
		{
			System.Windows.Forms.RadioButton rb = null;
			rb = (System.Windows.Forms.RadioButton)sender;
			if (rb.Checked == true)
			{
				projectslot_id = ((int)rb.Tag);
				RefreshScreen();
			}
		}

		void cmbChartType_SelectedIndexChanged (object sender, EventArgs e)
		{
			switch (cmbChartType.SelectedIndex)
			{
				case 0:
					RequiredChartType = emProjectPerfChartType.GANTT;
					break;

				case 1:
					RequiredChartType = emProjectPerfChartType.COST;
					break;

				case 2:
					RequiredChartType = emProjectPerfChartType.BOTH;
					break;
			}

			UpdateTotalButton();

			RefreshScreen();
		}

		void UpdateTotalButton()
		{
			allCostsRadioButton.Visible = (RequiredChartType == emProjectPerfChartType.COST);

			if ((! allCostsRadioButton.Visible) && allCostsRadioButton.Checked)
			{
				SelectFirstValidProject();
			}
		}
	}
}
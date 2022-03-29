using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

using LibCore;
using Network;

using CoreUtils;

namespace CommonGUI
{
	/// <summary>
	/// The Temp Node Monitor displays the thermal status of a server node
	/// It will green unless either of the Following cases is true.
	///   The cooling node supporting this server is down 
	///   The server node has "thermal" attribute tag which is true
	/// </summary>
	public class TempNodeMonitor: BasePanel, ITimedClass
	{
		protected StopControlledTimer timer;
		Random random = new Random();

		protected Panel[] lights;
		protected Panel[,] mirroredLights;
		protected Color[] colors;
		protected int mirrorCount = 0;
		protected Color emptycolor = Color.DarkGray;
		protected Node monitoredEntity = null;
		protected Node supportingCoolingNode = null;

		protected int current_level;
		int cl;
		public double CurrentLevel
		{
			get
			{
				return cl / (double) max_levels;
			}
		}

		protected int min_level = -1;
		protected int max_levels = 5;
		protected bool thermal_failure_in_server = false;
		protected bool thermal_warning_in_server = false;
		protected bool thermal_failure_in_supportingCoolingNode = false;
		protected bool thermal_warning_in_supportingCoolingNode = false;

		public TempNodeMonitor(Node modelNode, Random r)
		{
			random = r;
			Setup(modelNode, 5);
		}

		public Node ExtractCoolingNode(Node monitorNode)
		{
			Node CoolingNode = null;
			if (monitorNode != null)
			{
				foreach (Node cn in monitorNode.getChildren())
				{
					if (cn is LinkNode)
					{
						string linknodetype = cn.GetAttribute("type");
						string linknode_to = cn.GetAttribute("to");

						Node node = monitorNode.Tree.GetNamedNode(linknode_to);

						if ((node != null) && (node.GetAttribute("type").ToLower() == "cooling"))
						{
							CoolingNode = node;
							break;
						}
					}
				}
			}
			return CoolingNode;
		}

		public TempNodeMonitor(Node modelNode, int num_levels, Random r)
		{
			random = r;
			Setup(modelNode, num_levels);
		}

		protected void Setup(Node modelNode, int num_levels)
		{
			monitoredEntity = modelNode;

			timer = new StopControlledTimer();
			timer.Interval = 1000;
			timer.Tick += timer_Tick;

			max_levels = num_levels;
			SuspendLayout();

			lights = new Panel[num_levels];
			colors = new Color[num_levels];
			current_level = 0;

			if (num_levels==5)
			{
				SetLevelColor(0, Color.Green);
				SetLevelColor(1, Color.Green);
				SetLevelColor(2, Color.Yellow);
				SetLevelColor(3, Color.Orange);
				SetLevelColor(4, Color.Red);
			}

			for(int i=0; i<num_levels; ++i)
			{
				Panel panel = new Panel();
				panel.BackColor = emptycolor;
				//panel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
				Controls.Add(panel);
				lights[i] = panel;
			}
			ResumeLayout(false);

			//Connect up the Monitored Item 
			if (monitoredEntity != null)
			{
				monitoredEntity.AttributesChanged += monitoredEntity_AttributesChanged;
				monitoredEntity.Deleting += monitoredEntity_Deleting;
				
				//Extract the current thermal status
				thermal_failure_in_server = monitoredEntity.GetBooleanAttribute("thermal",false);

				//Connect up the cooler that services this node 
				supportingCoolingNode = ExtractCoolingNode(monitoredEntity);
				if (supportingCoolingNode != null)
				{
					//connect up the handler for the attributes changes
					supportingCoolingNode.AttributesChanged += supportingCoolingNode_AttributesChanged;

					bool up = supportingCoolingNode.GetBooleanAttribute("up", true);

					//extract the current status 
					if (supportingCoolingNode.GetAttribute("thermal") == "true")
					{
						thermal_failure_in_supportingCoolingNode =  ! up;
						thermal_warning_in_supportingCoolingNode = up;
					}
				}

				TimeManager.TheInstance.ManageClass(this);
			}
			//Set the Current level for Normal Green Operation
			current_level = 1;
			SetCurrentLevelFromStatus();
			SetupDisplay();
			Resize += TempNodeMonitor_Resize;
		}

		new protected void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(null != monitoredEntity)
				{
					monitoredEntity.AttributesChanged -= monitoredEntity_AttributesChanged;
					monitoredEntity.Deleting -= monitoredEntity_Deleting;
				}

				if (null != supportingCoolingNode)
				{
					supportingCoolingNode.AttributesChanged -= supportingCoolingNode_AttributesChanged;				
				}

				TimeManager.TheInstance.UnmanageClass(this);
			}

			base.Dispose (disposing);
		}

		/// <summary>
		/// If either of the Fail Status are true, then we go to RED
		/// </summary>
		protected void SetCurrentLevelFromStatus()
		{
			current_level = 1;
			if ((thermal_failure_in_server)||(thermal_failure_in_supportingCoolingNode))
			{
				current_level = 5;
			}
			else if (thermal_warning_in_server || thermal_warning_in_supportingCoolingNode)
			{
				current_level = 3;
			}
		}

		protected void SetupDisplay ()
		{
			cl = current_level;
			
			if(cl < 3)
			{
				cl += (random.Next(3)-2);
				if(cl > 4) cl = 3;
				else if(cl < 0) cl = 0;
			}

			for (int i = 0; i < 5; i++)
			{
				Color color;
				if (i <= cl)
				{
					color = colors[i];
				}
				else
				{
					color = Color.DarkGray;
				}
				//
				lights[i].BackColor = color;
			}
		}

		public virtual void SetLevelColor(int level, Color color)
		{
			colors[level] = color;
		}

		public void SetOverrideFailure(Boolean Override)
		{
			//AWT_Failure_Override = Override;
			//Refresh();
		}

		public void SetEmptyColor(Color EmptyColor)
		{
			//AWT_Failure_Override = Override;
			//Refresh();
		}

		protected virtual void DoSize()
		{
			int mainCellStride = Height / lights.Length;
			Size cellSize = new Size (Width, mainCellStride);

			if (mirrorCount > 0)
			{
				cellSize.Height = cellSize.Height / mirrorCount;
				mainCellStride = cellSize.Height * mirrorCount;
			}
			cellSize.Height -= 1;

			for (int i = 0; i < lights.Length; i++)
			{
				lights[i].Size = cellSize;
				lights[i].Location = new Point (0, Height - (mainCellStride * (i + 1)));

				if (mirroredLights != null)
				{
					for (int j = 0; j < mirrorCount; j++)
					{
						mirroredLights[i, j].Size = cellSize;
						mirroredLights[i, j].Location = new Point (0, Height - (mainCellStride * (i + 1)) - (((j + 1) * (cellSize.Height + 1))));
					}
				}
			}
		}

		void TempNodeMonitor_Resize(object sender, EventArgs e)
		{
			DoSize();
		}

		void monitoredEntity_Deleting(Node sender)
		{
			if(null != monitoredEntity)
			{
				monitoredEntity.AttributesChanged -= monitoredEntity_AttributesChanged;
				monitoredEntity.Deleting -= monitoredEntity_Deleting;
			}
			monitoredEntity = null;
			min_level = -1;
			current_level = -1;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		void supportingCoolingNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			bool up = sender.GetBooleanAttribute("up", true);

			foreach(AttributeValuePair avp in attrs)
			{
				if(avp.Attribute == "thermal")
				{
					bool thermal = (avp.Value == "true");
					thermal_failure_in_supportingCoolingNode = (thermal && ! up);
					thermal_warning_in_supportingCoolingNode = (thermal && up);
					SetCurrentLevelFromStatus();
					SetupDisplay();
				}
			}
		}

		/// <summary>
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="attrs"></param>
		void monitoredEntity_AttributesChanged(Node sender, ArrayList attrs)
		{
			//foreach(AttributeValuePair avp in attrs)
			//{
			//}

			bool up_status = monitoredEntity.GetBooleanAttribute("up", true);
			bool thermal_status = monitoredEntity.GetBooleanAttribute("thermal",false);
			thermal_failure_in_server = (thermal_status && ! up_status);
			thermal_warning_in_server = (thermal_status && up_status);

			SetCurrentLevelFromStatus();
			SetupDisplay();
		}

		void timer_Tick(object sender, EventArgs e)
		{
			SetupDisplay();
			//this.Invalidate();
		}

		#region ITimedClass Members

		public void Start()
		{
			timer.Start();
		}

		public void FastForward(double timesRealTime)
		{
			// TODO:  Add TempNodeMonitor.FastForward implementation
		}

		public void Reset()
		{
			// TODO:  Add TempNodeMonitor.Reset implementation
		}

		public void Stop()
		{
			timer.Stop();
		}

		#endregion
	}
}

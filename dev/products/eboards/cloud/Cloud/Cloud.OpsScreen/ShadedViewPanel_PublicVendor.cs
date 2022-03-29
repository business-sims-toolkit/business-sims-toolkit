using System.Drawing;
using System.Windows.Forms;

using System.Collections;
using CoreUtils;
using LibCore;
using Network;

namespace Cloud.OpsScreen
{
	/// <summary>
	/// The Public Vendor display provide a display of services and prices that are available from a public vendor
	/// The cloud options in the model have visibility flags for R3 and R4 so that we can have different display per round
	/// 
	/// The display is divided to 2 areas IAAS and SAAS (each with a set of column headers)
	///   The IAAS section has 2 service display slots (we display "low" security in R3 and "med" in R4) governed by node tag flags
	///   The SAAS section has 4 service display slots 
	///   
	///   In SAAS, we display them in display order (but any items with alert status, is displayed first in display order)
	///   So a Changing item will display at the top of the list, this alerts the people to the new price as well as the red background.
	///   This also helps if a item was not displayed (due to the sheer number of the services) and changes its price.
	///   
	/// could be tidied up more   
	/// 
	/// </summary>

	public class ShadedViewPanel_PublicVendor : ShadedViewPanel_Base
	{
		protected Brush brsh_Offwhite;
		protected Brush brsh_Header;
		protected Font Font_TableTitle;
		protected Font Font_TableBody;
		protected Font Font_AnnounceText;
		protected Font Font_AnnounceTime;
		protected bool use_active_data;
		protected int display_char_limit = 19;

		protected Node vendorNode = null;
		protected ArrayList options_IAAS = new ArrayList();
		protected ArrayList options_SAAS = new ArrayList();
		protected Hashtable displayNameLookups_SAAS = new Hashtable();
		protected Hashtable displayNameLookups_IAAS = new Hashtable();

		protected Hashtable SAAS_Services_active = new Hashtable();
		protected Hashtable SAAS_Services_inactive = new Hashtable();

		protected int title_saas_col1_xpos = 2;
		protected int title_saas_col2_xpos = 130;
		protected int title_saas_col3_xpos = 165;
		protected int title_saas_col4_xpos = 200;
		protected int title_saas_col5_xpos = 230;

        protected int title_iaas_col1_xpos = 2;
        protected int title_iaas_col2_xpos = 130;
        protected int title_iaas_col3_xpos = 165;
        protected int title_iaas_col4_xpos = 200;
        protected int title_iaas_col5_xpos = 230;

		int saas_limit = 3;
		int iaas_limit = 2;
		int inter_group_gap_y = 5;

		public ShadedViewPanel_PublicVendor(NodeTree nt, bool isTraining)
			: base(nt, isTraining)
		{
			string font = SkinningDefs.TheInstance.GetData("fontname");
			Font_AnnounceText = FontRepository.GetFont(font, 20, FontStyle.Bold);
			Font_AnnounceTime = FontRepository.GetFont(font, 24, FontStyle.Bold);
			Font_TableTitle = FontRepository.GetFont(font, 10, FontStyle.Bold);
			Font_TableBody = FontRepository.GetFont(font, 10, FontStyle.Regular);
			brsh_Offwhite = new SolidBrush(Color.FromArgb(224, 224, 224));
			brsh_Header = new SolidBrush(Color.FromArgb(102, 153, 153));

			Build_Display_Lookup_System();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (options_IAAS != null)
				{
					foreach (Node option_node in options_IAAS)
					{
						option_node.AttributesChanged -= new Node.AttributesChangedEventHandler(option_node_AttributesChanged);
					}
				}
				if (vendorNode != null)
				{
					vendorNode.AttributesChanged -= new Node.AttributesChangedEventHandler(vendorNode_AttributesChanged);
				}

				if (options_SAAS != null)
				{
					foreach (Node option_node in options_SAAS)
					{
						option_node.AttributesChanged -= new Node.AttributesChangedEventHandler(option_node_AttributesChanged);
					}
				}
				if (brsh_Offwhite != null)
				{
					brsh_Offwhite.Dispose();
				}
				if (brsh_Header != null)
				{
					brsh_Header.Dispose();
				}
			}
		}

		public void Build_Display_Lookup_System()
		{
			//needs a full lookup of the correct short display name for the service
			//rather than this hard coded system  
			//We need to use the Cloud Option info to walk the tree to get the service node and the service short name
			//

			displayNameLookups_IAAS.Clear();
			displayNameLookups_IAAS.Add("on_demand", "On Demand");
			displayNameLookups_IAAS.Add("reserved", "Reserved");

			displayNameLookups_SAAS.Clear();
			displayNameLookups_SAAS.Add("Automated Search and Match", "Automated Search");
			displayNameLookups_SAAS.Add("Stock Control System", "Stock Control");
		}

		public void setVendorConnection(bool active_data, int vendor_number)
		{
			use_active_data = active_data;
			vendorNode = _model.GetNamedNode("Public Cloud Provider " + CONVERT.ToStr(vendor_number));

			vendorNode.AttributesChanged += new Node.AttributesChangedEventHandler(vendorNode_AttributesChanged);

			foreach (Node optionNode in options_IAAS)
			{
				optionNode.AttributesChanged -= new Node.AttributesChangedEventHandler(option_node_AttributesChanged);
			}
			foreach (Node optionNode in options_SAAS)
			{
				optionNode.AttributesChanged -= new Node.AttributesChangedEventHandler(option_node_AttributesChanged);
			}

			options_IAAS.Clear();
			options_SAAS.Clear();
			//
			foreach (Node option_node in vendorNode.getChildren())
			{
				if (option_node.GetBooleanAttribute(CONVERT.Format("round_{0}_visible", CurrentRound), false))
				{
					string type = option_node.GetAttribute("type");
					if ((type.ToLower() == "saas_cloud_option"))
					{
						options_SAAS.Add(option_node);
					}
					else
					{
						options_IAAS.Add(option_node);
					}
					option_node.AttributesChanged += new Node.AttributesChangedEventHandler(option_node_AttributesChanged);
				}
			}

			Invalidate();
		}

		void vendorNode_AttributesChanged(Node sender, ArrayList attrs)
		{
			Invalidate();
		}

		protected void option_node_AttributesChanged(Node sender, ArrayList attrs)
		{
			Invalidate();
		}

		protected void get_SAAS_DisplayString(Node tmpNode, out string display_name, out string security_level,
			out string data_cuw_str, out string data_cpt_str, out string SMI_str)
		{
			string data_display = string.Empty;

			string type = tmpNode.GetAttribute("type");
			string common_service_name = tmpNode.GetAttribute("common_service_name");
			security_level = tmpNode.GetAttribute("security");
			double cost_per_user = tmpNode.GetDoubleAttribute("cost_per_user_per_round", 0);
			double cost_per_trade = tmpNode.GetDoubleAttribute("cost_per_trade", 0);
			int smi_value = tmpNode.GetIntAttribute("smi", 0);

			data_cuw_str = CONVERT.ToPaddedStr(cost_per_user, 2);
			data_cpt_str = CONVERT.ToPaddedStr(cost_per_trade, 2);
			SMI_str = CONVERT.ToStr(smi_value);

			if (security_level.Length > 3)
			{
				security_level = security_level.Substring(0, 3);
			}
			display_name = common_service_name;

			if (string.IsNullOrEmpty(common_service_name) == false)
			{
				if (displayNameLookups_SAAS.ContainsKey(common_service_name))
				{
					common_service_name = (string)displayNameLookups_SAAS[common_service_name];
				}

				display_name = common_service_name;
				if (common_service_name.Length > display_char_limit)
				{
					display_name = common_service_name.Substring(0, display_char_limit);
				}
			}
		}

		protected void get_IAAS_DisplayString(Node tmpNode, out string display_name, out string security_level,
			out string data_cpt_str, out string data_cpr_str, out string SMI_str)
		{
			string data_display = string.Empty;

			string type = tmpNode.GetAttribute("type");
			string common_service_name = tmpNode.GetAttribute("common_service_name");

			data_display = tmpNode.GetAttribute("charge_model");
			security_level = tmpNode.GetAttribute("security");
			SMI_str = CONVERT.ToStr(tmpNode.GetIntAttribute("smi", 0));

			display_name = data_display;

			if (security_level.Length > 3)
			{
				security_level = security_level.Substring(0, 3);
			}

			//extract out the 
			string tmp_str = "";
			tmp_str = tmpNode.GetAttribute("cost_per_realtime_minute");
			if (string.IsNullOrEmpty(tmp_str))
			{
				data_cpt_str = "N/A";
			}
			else
			{
				double v = CONVERT.ParseDoubleSafe(tmp_str, 0);
				data_cpt_str = CONVERT.ToPaddedStr(v, 0);
			}

			tmp_str = tmpNode.GetAttribute("cost_per_round");
			if (string.IsNullOrEmpty(tmp_str))
			{
				data_cpr_str = "N/A";
			}
			else
			{
				double v = CONVERT.ParseDoubleSafe(tmp_str, 0);
				data_cpr_str = CONVERT.ToPaddedStr(v, 0);
			}


			if (string.IsNullOrEmpty(display_name) == false)
			{
				if (displayNameLookups_IAAS.ContainsKey(display_name))
				{
					display_name = (string)displayNameLookups_IAAS[display_name];
				}

				if (display_name.Length > display_char_limit)
				{
					display_name = display_name.Substring(0, display_char_limit);
				}
			}
		}

		protected string getPaddedString(int count)
		{
			if (count < 10)
			{
				return "0" + CONVERT.ToStr(count);
			}
			return CONVERT.ToStr(count);
		}

		protected Point[] getSlicedRectangle(int startx, int starty, int rect_width, int rect_height, int chamfer)
		{
			Point[] pts = new Point[8];
			pts[0] = new Point(startx + chamfer, starty);
			pts[1] = new Point(startx + rect_width - chamfer, starty);
			pts[2] = new Point(startx + rect_width, starty + chamfer);
			pts[3] = new Point(startx + rect_width, starty + rect_height - chamfer);
			pts[4] = new Point(startx + rect_width - chamfer, starty + rect_height);
			pts[5] = new Point(startx + chamfer, starty + rect_height);
			pts[6] = new Point(startx, starty + rect_height - chamfer);
			pts[7] = new Point(startx, starty + chamfer);
			return pts;
		}

		protected void Draw_NOT_Available_Display(PaintEventArgs e)
		{
			e.Graphics.DrawString("NOT ACTIVE", Font_TableTitle, brsh_Offwhite, title_saas_col1_xpos, 20);
		}

		protected void Draw_Opening_Display(PaintEventArgs e)
		{
			int count_down = vendorNode.GetIntAttribute("count_down",0);
			int count_down_min = count_down / 60;
			int count_down_sec = count_down % 60;

			string display_text = "Services Available in";

			SizeF sz = MeasureString(Font_AnnounceText, display_text);
			int startX = (Width - ((int)sz.Width)) / 2;

			Point startPt1 = new Point(100 - 15, 90);
			Point startPt2 = new Point(150 - 15, 90);

			Point[] Pts1 = getSlicedRectangle(startPt1.X, startPt1.Y, 40, 35, 3);
			Point[] Pts2 = getSlicedRectangle(startPt2.X, startPt2.Y, 40, 35, 3);

			e.Graphics.DrawString(display_text, Font_AnnounceText, br_hiGreen, startX, 50);
			e.Graphics.DrawPolygon(Pens.Silver, Pts1);
			e.Graphics.DrawString(getPaddedString(count_down_min), Font_AnnounceTime, br_hiGreen, startPt1.X + 0, startPt1.Y - 2);
			e.Graphics.DrawPolygon(Pens.Silver, Pts2);
			e.Graphics.DrawString(getPaddedString(count_down_sec), Font_AnnounceTime, br_hiGreen, startPt2.X + 0, startPt2.Y - 2);
			//e.Graphics.DrawString("NOT ACTIVE OPENING ", Font_TableTitle, brsh_Offwhite, title_saas_col1_xpos, 20);
		}

		protected void Draw_Closing_Display(PaintEventArgs e)
		{
			//e.Graphics.DrawString("NOT ACTIVE CLOSING ", Font_TableTitle, brsh_Offwhite, title_saas_col1_xpos, 20);
			int count_down = vendorNode.GetIntAttribute("count_down", 0);
			int count_down_min = count_down / 60;
			int count_down_sec = count_down % 60;

			string display_text = "Services Unavailable in";

			SizeF sz = MeasureString(Font_AnnounceText, display_text);
			int startX = (Width - ((int)sz.Width)) / 2;

			Point startPt1 = new Point(100 - 15, 90);
			Point startPt2 = new Point(150 - 15, 90);

			Point[] Pts1 = getSlicedRectangle(startPt1.X, startPt1.Y, 40, 35, 3);
			Point[] Pts2 = getSlicedRectangle(startPt2.X, startPt2.Y, 40, 35, 3);

			e.Graphics.DrawString(display_text, Font_AnnounceText, br_hiOrangeRed, startX, 50);
			e.Graphics.DrawPolygon(Pens.Silver, Pts1);
			e.Graphics.DrawString(getPaddedString(count_down_min), Font_AnnounceTime, br_hiOrangeRed, startPt1.X + 0, startPt1.Y - 2);
			e.Graphics.DrawPolygon(Pens.Silver, Pts2);
			e.Graphics.DrawString(getPaddedString(count_down_sec), Font_AnnounceTime, br_hiOrangeRed, startPt2.X + 0, startPt2.Y - 2);
			//e.Graphics.DrawString("NOT ACTIVE OPENING ", Font_TableTitle, brsh_Offwhite, title_saas_col1_xpos, 20);
		}

		protected void Draw_Available_Display(PaintEventArgs e)
		{
			//base.OnPaint(e);
			int x_start = 2;
			int y_start = 20;
			int y_rowStep = 15;

			int x_pos = x_start;
			int y_pos = y_start;

			if ((options_SAAS.Count > 0) & (options_IAAS.Count > 0))
			{
				saas_limit = 6;
				iaas_limit = 2;
			}
			else
			{
				if ((options_SAAS.Count == 0))
				{
					saas_limit = 0;
					iaas_limit = 6;
				}
				else
				{
					iaas_limit = 6;
					saas_limit = 0;
				}
			}

			if (iaas_limit > 0)
			{
				e.Graphics.DrawString("IaaS Services", Font_TableTitle, brsh_Header, title_iaas_col1_xpos, y_pos);
				e.Graphics.DrawString("SEC", Font_TableTitle, brsh_Header, title_iaas_col2_xpos, y_pos);
				e.Graphics.DrawString("$/TP", Font_TableTitle, brsh_Header, title_iaas_col3_xpos, y_pos);
				e.Graphics.DrawString("$/R", Font_TableTitle, brsh_Header, title_iaas_col4_xpos, y_pos);
				e.Graphics.DrawString("SMI", Font_TableTitle, brsh_Header, title_iaas_col5_xpos, y_pos);

				y_pos += y_rowStep;

				int used_counter = 0;
				foreach (Node tmpNode in options_IAAS)
				{
					bool isVisibleR3 = tmpNode.GetBooleanAttribute("round_3_visible", false);
					bool isVisibleR4 = tmpNode.GetBooleanAttribute("round_4_visible", false);

					if (((CurrentRound == 3) && (isVisibleR3)) | ((CurrentRound == 4) && (isVisibleR4)))
					{
						if (used_counter < iaas_limit)
						{
							string display_name = string.Empty;
							string security_level = string.Empty;
							string data_size_str = string.Empty;
							string data_cpt_str = string.Empty;
							string data_cpr_str = string.Empty;
							string SMI_str = string.Empty;

							get_IAAS_DisplayString(tmpNode, out display_name, out security_level, out data_cpt_str, out data_cpr_str, out SMI_str);

							bool alert = tmpNode.GetBooleanAttribute("alert", false);

							Brush brh_textFore = br_hiWhite;
							if (alert)
							{
								//brh_textFore = this.br_hiRed;
								e.Graphics.FillRectangle(br_hiRed, title_iaas_col1_xpos, y_pos, Width - 4, y_rowStep);
							}

							e.Graphics.DrawString(display_name, Font_TableBody, brh_textFore, title_iaas_col1_xpos, y_pos);
							e.Graphics.DrawString(security_level, Font_TableBody, brh_textFore, title_iaas_col2_xpos, y_pos);
							e.Graphics.DrawString(data_cpt_str, Font_TableBody, brh_textFore, title_iaas_col3_xpos, y_pos);
							e.Graphics.DrawString(data_cpr_str, Font_TableBody, brh_textFore, title_iaas_col4_xpos, y_pos);
							e.Graphics.DrawString(SMI_str, Font_TableBody, brh_textFore, title_iaas_col5_xpos, y_pos);

							y_pos += y_rowStep;
							used_counter++;
						}
					}
				}
				//y_pos += y_rowStep;
			}
			y_pos += inter_group_gap_y;

			if (saas_limit > 0)
			{
				e.Graphics.DrawString("SaaS Services", Font_TableTitle, brsh_Header, title_saas_col1_xpos, y_pos);
				e.Graphics.DrawString("SEC", Font_TableTitle, brsh_Header, title_saas_col2_xpos, y_pos);
				e.Graphics.DrawString("$/U", Font_TableTitle, brsh_Header, title_saas_col3_xpos, y_pos);
				e.Graphics.DrawString("$/T", Font_TableTitle, brsh_Header, title_saas_col4_xpos, y_pos);
				e.Graphics.DrawString("SMI", Font_TableTitle, brsh_Header, title_saas_col5_xpos, y_pos);
				y_pos += y_rowStep;

				SAAS_Services_active.Clear();
				SAAS_Services_inactive.Clear();

				int used_counter = 0;
				int discover_index = 0;

				//Extract the SAAS into 2 different Display Lists ("Active" and "inactive")
				foreach (Node tmpNode in options_SAAS)
				{
					bool isVisibleR3 = tmpNode.GetBooleanAttribute("round_3_visible", false);
					bool isVisibleR4 = tmpNode.GetBooleanAttribute("round_4_visible", false);
					bool alert = tmpNode.GetBooleanAttribute("alert", false);
					int display_order = tmpNode.GetIntAttribute("display_order", discover_index);

					if (((CurrentRound == 3) && (isVisibleR3)) | ((CurrentRound == 4) && (isVisibleR4)))
					{
						if (alert)
						{
							SAAS_Services_active.Add(display_order, tmpNode);
						}
						else
						{
							SAAS_Services_inactive.Add(display_order, tmpNode);
						}
					}
					discover_index++;
				}

				//===============================================================================
				//Now display the Active items (These have just Changed Attribute, usually Price)
				//===============================================================================
				foreach (int display_index in SAAS_Services_active.Keys)
				{
					Node tmpNode = (Node)SAAS_Services_active[display_index];
					bool alert = tmpNode.GetBooleanAttribute("alert", false);

					if (used_counter < saas_limit)
					{
						string display_name = string.Empty;
						string security_str = string.Empty;
						string data_cuw_str = string.Empty;
						string data_cpt_str = string.Empty;
						string SMI_str = string.Empty;

						get_SAAS_DisplayString(tmpNode, out display_name, out security_str,
								out data_cuw_str, out data_cpt_str, out SMI_str);
						Brush brh_textFore = br_hiWhite;
						if (alert)
						{
							//brh_textFore = this.br_hiRed;
							e.Graphics.FillRectangle(br_hiRed, title_iaas_col1_xpos, y_pos, Width - 4, y_rowStep);
						}

						e.Graphics.DrawString(display_name, Font_TableBody, brh_textFore, title_saas_col1_xpos, y_pos);
						e.Graphics.DrawString(security_str, Font_TableBody, brh_textFore, title_saas_col2_xpos, y_pos);
						e.Graphics.DrawString(data_cuw_str, Font_TableBody, brh_textFore, title_saas_col3_xpos, y_pos);
						e.Graphics.DrawString(data_cpt_str, Font_TableBody, brh_textFore, title_saas_col4_xpos, y_pos);
						e.Graphics.DrawString(SMI_str, Font_TableBody, brh_textFore, title_saas_col5_xpos, y_pos);
						y_pos += y_rowStep;
						used_counter++;
					}
				}

				//=============================================================
				//Now display the INActive items 
				//=============================================================
				foreach (int display_index in SAAS_Services_inactive.Keys)
				{
					Node tmpNode = (Node)SAAS_Services_inactive[display_index];
					bool alert = tmpNode.GetBooleanAttribute("alert", false);

					if (used_counter < saas_limit)
					{
						string display_name = string.Empty;
						string security_str = string.Empty;
						string data_cuw_str = string.Empty;
						string data_cpt_str = string.Empty;
						string SMI_str = string.Empty;

						get_SAAS_DisplayString(tmpNode, out display_name, out security_str,
								out data_cuw_str, out data_cpt_str, out SMI_str);
						Brush brh_textFore = br_hiWhite;
						if (alert)
						{
							//brh_textFore = this.br_hiRed;
							e.Graphics.FillRectangle(br_hiRed, title_iaas_col1_xpos, y_pos, Width - 4, y_rowStep);
						}

						e.Graphics.DrawString(display_name, Font_TableBody, brh_textFore, title_saas_col1_xpos, y_pos);
						e.Graphics.DrawString(security_str, Font_TableBody, brh_textFore, title_saas_col2_xpos, y_pos);
						e.Graphics.DrawString(data_cuw_str, Font_TableBody, brh_textFore, title_saas_col3_xpos, y_pos);
						e.Graphics.DrawString(data_cpt_str, Font_TableBody, brh_textFore, title_saas_col4_xpos, y_pos);
						e.Graphics.DrawString(SMI_str, Font_TableBody, brh_textFore, title_saas_col5_xpos, y_pos);
						y_pos += y_rowStep;
						used_counter++;
					}
				}
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.FillRectangle(Brushes.Black, 0, 0, Width, Height);
			if (ControlBackgroundImage != null)
			{
				e.Graphics.DrawImageUnscaled(BackgroundImage, 0, 0, ControlBackgroundImage.Width, ControlBackgroundImage.Height);
			}
			if (string.IsNullOrEmpty(TitleText) == false)
			{
				e.Graphics.DrawString(TitleText, Font_Title, brush_Title, 0, 0);

				if (TitleText.IndexOf("3") > -1)
				{
					Node v_node = vendorNode;
				}
			}

			//base.OnPaint(e);
			int x_start = 2;
			int y_start = 20;

			int x_pos = x_start;
			int y_pos = y_start;


			if ((use_active_data == false) | (vendorNode == null))
			{
				Draw_NOT_Available_Display(e);
			}
			else
			{
				string status = vendorNode.GetAttribute("status");

				switch (status.ToLower())
				{
					case "not_available":
						Draw_NOT_Available_Display(e);
						break;
					case "opening":
						Draw_Opening_Display(e);
						break;
					case "closing":
						Draw_Closing_Display(e);
						break;
					case "available":
						Draw_Available_Display(e);
						break;
				}
			}
		}

	}
}

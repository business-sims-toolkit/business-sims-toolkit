using System.Xml;
using System.Drawing;
using System.Windows.Forms;
using LibCore;

namespace Charts
{
	/// <summary>
	/// Summary description for ZonePowerChangeDisplay_HPDC.
	/// </summary>
	public class ZonePowerChangeDisplay_HPDC : Panel
	{
		bool[] ZonePowerDemandChanged = new bool[7];
		Label[] ZoneLabels = new Label[7];

		public ZonePowerChangeDisplay_HPDC()
		{
			for (int step=0; step < 7; step++)
			{
				ZonePowerDemandChanged[step] = false;
				ZoneLabels[step] = new Label();
				ZoneLabels[step].Location = new Point(2+step*70,2);
				ZoneLabels[step].Size = new Size(60,20);
				ZoneLabels[step].Text = "Zone "+(step+1).ToString();
				ZoneLabels[step].BackColor = Color.White;
				this.Controls.Add(ZoneLabels[step]);
			}
			this.BackColor = Color.White;
		}

		public void reset()
		{
			for (int step=0; step < 7; step++)
			{
				ZonePowerDemandChanged[step] = false;
			}
			refreshLabels();
		}

		public void LoadData(string xmldata)
		{
			BasicXmlDocument xdoc = LibCore.BasicXmlDocument.Create(xmldata);
			XmlNode rootNode = xdoc.DocumentElement;
			foreach(XmlNode child in rootNode.ChildNodes)
			{
				string ss = child.Name.ToLower();
				if (child.Name.ToLower() == "zones")
				{
					foreach(XmlAttribute att in child.Attributes)
					{
						string att_name = att.Name;
						string att_intxt = att.InnerText;
						string att_inxml = att.InnerXml;

						for (int step=0; step<7; step++)
						{
							if (att.Name.IndexOf((step+1).ToString())>-1)
							{
								if (att.InnerText.ToLower()== "true")
								{
									ZonePowerDemandChanged[step] = true;
								}
								else
								{
									ZonePowerDemandChanged[step] = false;
								}
							}
						}
						string st = att.InnerText;
						string sx = att.InnerXml;
					}
				}
			}
			refreshLabels();
		}

		void refreshLabels()
		{
			for (int step=0; step<7; step++)
			{
				if (ZoneLabels[step] != null)
				{
					if (ZonePowerDemandChanged[step])
					{
						ZoneLabels[step].BackColor = Color.Pink;
						ZoneLabels[step].Invalidate(); // Refresh();
					}
					else
					{
						ZoneLabels[step].BackColor = Color.White;
						ZoneLabels[step].Invalidate(); //Refresh();
					}
				}
				//this.Refresh();
				Invalidate();
			}
		}

	}
}

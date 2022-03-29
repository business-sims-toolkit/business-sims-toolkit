using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Network;

namespace Cloud.OpsScreen
{
	public class FeedbackPanel : Panel
	{
		List<string> sections;
		Dictionary<string, Color> sectionToColour;
		Dictionary<string, List<string>> sectionToItems;

		bool suppressUpdates;

		public FeedbackPanel ()
		{
			sections = new List<string> ();
			sectionToColour = new Dictionary<string, Color> ();
			sectionToItems = new Dictionary<string, List<string>> ();

			suppressUpdates = false;

			Rebuild();
		}

		public void Clear ()
		{
			sections.Clear();
			sectionToColour.Clear();
			sectionToItems.Clear();

			Rebuild();
		}

		public void AddSection (string section, Color colour)
		{
			if (! sections.Contains(section))
			{
				sections.Add(section);
				sectionToColour.Add(section, colour);
				sectionToItems.Add(section, new List<string>());

				Rebuild();
			}
		}

		public void AddFeedbackItem (string section, string text)
		{
			sectionToItems[section].Add(text);
			Rebuild();
		}

		void Rebuild ()
		{
			if (!suppressUpdates)
			{
				Controls.Clear();
				AutoScroll = false;

				int y = 0;
				int sectionGap = 10;

				foreach (string section in sections)
				{
					foreach (string item in sectionToItems[section])
					{
						Label itemLabel = CreateLabel(item, sectionToColour[section], y);
						y = itemLabel.Bottom;
					}

					y += sectionGap;
				}

				AutoScroll = true;
			}
		}

		Label CreateLabel (string text, Color colour, int y)
		{
			int margin = 4;
			Label label = new Label ();
			label.Text = text.Replace("&", "&&");
			label.ForeColor = colour;
			label.BackColor = Color.Transparent;

			Size preferredSize = label.GetPreferredSize(new Size (Width - margin, 0));
			label.Size = new Size (Width - margin, preferredSize.Height);
			label.Location = new Point (0, y);
			Controls.Add(label);

			return label;
		}

		public void ReflectPlannedOrders (Node plannedOrders)
		{
			Clear();

			suppressUpdates = true;
			foreach (Node message in plannedOrders.getChildren())
			{
				string text = message.GetAttribute("message");

				if (! string.IsNullOrEmpty(text))
				{
					string stage = message.GetAttribute("stage");

					Color colour;

					if (message.GetAttribute("type") == "error")
					{
						colour = Color.White;
					}
					else
					{
						if (message.GetAttribute("type") == "confirmation")
						{
							//colour = Color.Red;
							colour = Color.FromArgb(255, 51, 0); //matchs the orange red from the area finance displays
							stage = "hardware";
						}
						else
						{
							switch (stage)
							{
								case "dev":
									//colour = Color.FromArgb(0, 100, 255);
									colour = Color.FromArgb(102, 204, 0); //matches the green from the area finance displays
									break;

								case "production":
								default:
									//colour = Color.Green;
									colour = Color.FromArgb(102, 204, 0);//matches the green from the area finance displays
									break;
							}
						}
					}

					AddSection(stage, colour);
					AddFeedbackItem(stage, text);
				}
			}
			suppressUpdates = false;

			Rebuild();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);
			Rebuild();
		}
	}
}
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Network;

namespace ResizingUi
{
	public class AwtCollectionPanel : CascadedBackgroundPanel
	{
	    readonly List<AwtPanel> awtPanels;

	    readonly int columns = 4;
	    readonly int rows = 2;
		int rowGap = 20;
		int columnGap = 20;

		public AwtCollectionPanel (XmlElement root, NodeTree model)
		{
			UseCascadedBackground = false;

			awtPanels = new List<AwtPanel> ();
			var random = new Random ();

			foreach (XmlElement group in root.SelectNodes("group"))
			{
				var panel = new AwtPanel(model, group.SelectSingleNode("name").InnerText,
					model.GetNamedNode("AdvancedWarningTechnology"),
					group.SelectNodes("monitor").Cast<XmlElement>().Select(item => item.InnerText).ToList(),
					random)
				{
					BottomMargin = 20,
					SideMargin = 2,
					TopMargin = 20,
					ColumnGap = 1,
					RowGap = 1
				};
				awtPanels.Add(panel);
				Controls.Add(panel);
			}

			DoSize();
		}

		public ICollection<AwtPanel> AwtPanels => awtPanels.AsReadOnly();

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			DoSize();
		}

		void DoSize ()
		{
			rowGap = Height / 40;
			columnGap = Width / 80;
			int index = 0;
			var panelSize = new Size ((Width - ((columns - 1) * columnGap)) / columns, (Height - ((rows - 1) * rowGap)) / rows);

			for (int i = 0; i < rows; i++)
			{
				for (int j = 0; j < columns; j++)
				{
					if (index < awtPanels.Count)
					{
						var panel = awtPanels[index];
						index++;

						panel.Bounds = new Rectangle (j * (panelSize.Width + columnGap), i * (panelSize.Height + rowGap), panelSize.Width, panelSize.Height);
					}
				}
			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			using (var backBrush = new SolidBrush (Color.FromArgb(CoreUtils.SkinningDefs.TheInstance.GetIntData("cascaded_background_transparency", 255), BackColor)))
			{
				e.Graphics.FillRectangle(backBrush, new Rectangle(0, 0, Width, Height));
			}
		}
	}
}
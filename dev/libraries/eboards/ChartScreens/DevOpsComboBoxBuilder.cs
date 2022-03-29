using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using CommonGUI;
using CoreUtils;
using GameManagement;
using LibCore;
using Network;

namespace ChartScreens
{
    public class DevOpsComboBoxBuilder
    {
	    public static ComboBoxRow CreateComboBox (string title, IList<ComboBoxOption> items)
	    {
		    var buttonWidth = SkinningDefs.TheInstance.GetIntData("round_combo_box_button_width", 50);
		    var titleWidth = SkinningDefs.TheInstance.GetIntData("round_combo_box_title_width", 70);

		    var boxWidth = buttonWidth * items.Count + titleWidth;
		    var boxHeight = SkinningDefs.TheInstance.GetIntData("combo_box_height", 20);

		    return BuildComboBoxRow(title, items, buttonWidth, boxHeight, boxWidth, boxHeight);
	    }

        public static ComboBoxRow CreateRoundComboBox(NetworkProgressionGameFile gameFile, bool includeAllRounds = true, bool includeAllRoundsButton = false)
        {
            var roundCount = gameFile.GetTotalRounds();

            roundCount = (includeAllRounds) ? roundCount : roundCount - 1;

            var items = Enumerable.Range(1, roundCount).Select(i => new ComboBoxOption { Text = $"Round {i}", Tag = i }).ToList();

	        if (includeAllRoundsButton)
	        {
		        items.Add(new ComboBoxOption { Text = "All Rounds", Tag = null });
	        }

            var buttonWidth = SkinningDefs.TheInstance.GetIntData("round_combo_box_button_width", 50);
            var titleWidth = SkinningDefs.TheInstance.GetIntData("round_combo_box_title_width", 70);

            var boxWidth = buttonWidth * items.Count + titleWidth;
            var boxHeight = SkinningDefs.TheInstance.GetIntData("combo_box_height", 20);

            return BuildComboBoxRow(SkinningDefs.TheInstance.GetData("round_combo_box_title", "Round"), items, buttonWidth, boxHeight, boxWidth, boxHeight);
        }

        public static ComboBoxRow CreateBusinessComboBox(NetworkProgressionGameFile gameFile, bool includeAllBusinesses = false)
        {
            var items = new List<ComboBoxOption>();

            if (includeAllBusinesses)
            {
                items.Add(new ComboBoxOption {Text = SkinningDefs.TheInstance.GetData("allbiz"), Tag = null});
            }

            var bizName = SkinningDefs.TheInstance.GetData("biz");

            var businesses = new List<Node>((Node[])
                gameFile.NetworkModel.GetNodesWithAttributeValue("type", bizName).ToArray(typeof(Node)));

            items.AddRange(businesses.Select(b => new
            {
                Name = b.GetAttribute("name"),
                Order = b.GetAttribute("order")
            }).OrderBy(b => b.Order)
            .Select(b => new ComboBoxOption { Text = b.Name, Tag = b }));

            var buttonWidth = SkinningDefs.TheInstance.GetIntData("business_combo_box_button_width", 50);
            var titleWidth = SkinningDefs.TheInstance.GetIntData("business_combo_box_title_width", 70);

            var boxWidth = buttonWidth * items.Count + titleWidth;
            var boxHeight = SkinningDefs.TheInstance.GetIntData("combo_box_height", 20);

            return BuildComboBoxRow(SkinningDefs.TheInstance.GetData("business_combo_box_title", "Division"), items, buttonWidth, boxHeight, boxWidth, boxHeight);
        }

	    static ComboBoxRow BuildComboBoxRow (string title, IEnumerable<ComboBoxOption> items, int buttonWidth, int buttonHeight, int boxWidth, int boxHeight)
	    {
		    ComboBoxRow.ICheckableButtonCreator factory;
		    if (SkinningDefs.TheInstance.GetBoolData("use_styled_buttons", false))
		    {
			    factory = new ComboBoxRow.ComboBoxButtonCreator("combo_box", buttonWidth, buttonHeight, SkinningDefs.TheInstance.GetFontWithStyle("combo_box_button_font"));
		    }
		    else
		    {
			    factory = new ComboBoxRow.ImageTextButtonCreator(SkinningDefs.TheInstance.GetColorData("report_combo_box_text_colour", Color.FromArgb(69, 69, 83)),
				    SkinningDefs.TheInstance.GetColorData("report_combo_box_active_text_colour", Color.FromArgb(34, 40, 49)),
				    SkinningDefs.TheInstance.GetColorData("report_combo_box_hover_text_colour", Color.FromArgb(69, 69, 83)),
				    SkinningDefs.TheInstance.GetColorData("report_combo_box_disabled_text_colour", Color.FromArgb(35, 35, 42)),
				    $@"comboBox\{buttonWidth}x{buttonHeight}.png");
		    }

		    var comboBox = new ComboBoxRow(factory, title)
		    {
			    ItemGap = 0,
			    Size = new Size(boxWidth, boxHeight)
		    };

		    foreach (var item in items)
		    {
			    comboBox.Items.Add(item);
		    }

		    comboBox.BackColor = Color.Transparent;

		    return comboBox;
	    }
    }
}

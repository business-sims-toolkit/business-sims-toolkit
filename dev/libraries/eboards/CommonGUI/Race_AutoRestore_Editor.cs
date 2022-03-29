using System;
using System.Drawing;

using Network;
using CoreUtils;

namespace CommonGUI
{
	public class Race_AutoRestore_Editor : Race_SLA_Editor
	{
		public Race_AutoRestore_Editor (OpsControlPanel tcp, NodeTree tree,
		                                Boolean IsTrainingMode, Color OperationsBackColor, int round)
			: base (tcp, tree, IsTrainingMode, OperationsBackColor, round)
		{
			header.Text = TextTranslator.TheInstance.Translate("Set Auto Restore Time for Services");
			header.Width = 500;
		}

		protected override Race_SLA_Item CreateItem (Node node)
		{
			return new Race_AutoRestore_Item (_tree, node,
			                                  MyDefaultSkinFontBold8, MyDefaultSkinFontBold8,
			                                  MyDefaultSkinFontNormal10);
		}
	}
}
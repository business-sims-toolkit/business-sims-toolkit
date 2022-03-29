using System;
using System.Collections.Generic;
using System.Windows.Forms;

using Network;

using ContentAlignment = System.Drawing.ContentAlignment;

namespace DevOpsUi.FeatureDevelopment
{
	public class StageGroupProperties
	{
		public string Title { get; set; }
		public List<string> CommandTypes { get; set; }
		public Func<Node, string> GetCorrectOption { get; set; }
		public Func<Node, string> GetCurrentSelection { get; set; }
		public Func<List<ButtonTextTags>> GetOptions { get; set; }

		public FlowDirection ButtonFlowDirection { get; set; }
		public bool WrapContents { get; set; }

		public ContentAlignment TitleAlignment { get; set; } = ContentAlignment.MiddleLeft;
	}

	public struct ButtonTextTags
	{
		public string ButtonId { get; set; }
		public string ButtonText { get; set; }
		public string ButtonTag { get; set; }

		public bool IsEnabled { get; set; }
	}

	public struct StageButtonProperties
	{
		public string Text { get; set; }
		public string Tag { get; set; }
		public bool IsCorrectOption { get; set; }
		public bool IsAlreadySelected { get; set; }
		public bool IsEnabled { get; set; }
	}
}

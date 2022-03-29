using System.Threading;
using System.Windows.Forms;

namespace LibCore
{
	public static class MessageBoxHandling
	{
		struct MessageBoxParameters
		{
			public IWin32Window Parent;
			public string Text;
			public string Title;
			public MessageBoxButtons Buttons;
		}

		static DialogResult result;

		public static DialogResult ShowMessageBox (IWin32Window parent, string text, string title, MessageBoxButtons buttons)
		{
			Thread thread = new Thread (ShowMessageBoxThreadFunction);
			var parameters = new MessageBoxParameters { Parent = parent, Text = text, Title = title, Buttons = buttons };
			thread.Start(parameters);
			thread.Join();
			return result;
		}

		static void ShowMessageBoxThreadFunction (object parametersAsObject)
		{
			var parameters = (MessageBoxParameters) parametersAsObject;
			result = MessageBox.Show(null, parameters.Text, parameters.Title, parameters.Buttons);
		}
	}
}
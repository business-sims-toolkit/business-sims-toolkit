using System;
//using System.Collections.Generic;

namespace CoreScreens
{
	public class CrossThreadInvoker : System.Windows.Forms.Control
	{
		delegate void DoInvokeDelegate(object action);

		public delegate void CrossThreadActionHandler(CrossThreadInvoker sender, object action);
		public event CrossThreadActionHandler OnInvoke;

		public CrossThreadInvoker()
		{
		}

		public void DoInvoke(object action)
		{
			if (null == OnInvoke)
			{
				throw (new Exception("Cannot DoInvoke without an OnInvoke to call."));
			}

			if (this.InvokeRequired)
			{
				object[] args = new object[1];
				args[0] = action;

				this.Invoke(new DoInvokeDelegate(DoInvoke), args);
			}
			else
			{
				OnInvoke(this, action);
			}
		}
	}
}

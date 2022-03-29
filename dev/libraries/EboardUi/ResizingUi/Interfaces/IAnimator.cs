using System;

namespace ResizingUi.Interfaces
{
	public interface imator<T> : IDisposable
	{
		float Timer { get; }
		float Duration { get; }
		T GetValue ();

		event EventHandler Update;
	}
}
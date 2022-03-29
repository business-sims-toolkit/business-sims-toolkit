using System;

namespace LibCore
{
	public interface IGameFileUploader : IDisposable
	{
		void UploadFile (string filename);
		void UploadFiles (string gameFolder);
	}
}
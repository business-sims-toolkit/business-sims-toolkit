using System.Net.NetworkInformation;

namespace BaseUtils
{
	public class InternetDetector
	{
		public InternetDetector()
		{ 
		
		}

		public bool IsNetworkConnected()
		{
			bool connected = false;
			NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

			foreach (NetworkInterface ni in networkInterfaces)
			{
				if (ni.OperationalStatus == OperationalStatus.Up 
					&& ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
					&& ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
				{
					connected = true;
					//break;
				}
			}
			
			return connected;
		}

	}
}

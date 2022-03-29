using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameManagement;

namespace ReportBuilder
{
	public class RoundComparisonReport
	{
		NetworkProgressionGameFile gameFile;

		public RoundComparisonReport (NetworkProgressionGameFile gameFile)
		{
			this.gameFile = gameFile;
		}
	}
}
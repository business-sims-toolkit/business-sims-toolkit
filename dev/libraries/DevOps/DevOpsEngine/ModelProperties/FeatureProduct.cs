using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Network;

namespace DevOpsEngine.ModelProperties
{
	public class FeatureProduct
	{
		public Node ProductNode { get; set; }
		public string ProductId { get; set; }
		public string Platform { get; set; }
		public bool IsOptimal { get; set; }
		public bool IsAvailable { get; set; }
		public string DisplayName => $"{ProductId} {Platform}";
	}
}

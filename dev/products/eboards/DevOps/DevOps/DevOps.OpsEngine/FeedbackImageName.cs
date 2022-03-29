using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevOps.OpsEngine
{
    public class FeedbackImageName
    {
        public const string Cash = "cash";
        public const string Clock = "clock";
        public const string Cross = "cross";
        public const string Tick = "tick";

        public static List<string> All => new List<string> { Cash, Clock, Cross, Tick };
    }
}

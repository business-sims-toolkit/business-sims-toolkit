
using System.Collections.Generic;

namespace DevOps.OpsEngine
{
    public class ServiceStatus
    {   
        // TODO need to find all the possible statuses used to complete this 
        // then replace the hardcoded strings with these.
        public const string Dev = "dev";
        public const string Test = "test";
        public const string TestDelay = "testDelay";
        public const string Release = "release";
        public const string Deploy = "deploy";
        public const string Installing = "installing";
        public const string Live = "live";
        public const string Cancelled = "cancelled";
        public const string Undo = "undo";

        public static List<string> All => new List<string>
        {
            Dev, Test, TestDelay, Release, Installing, Live, Cancelled, Undo
        };
    }
}

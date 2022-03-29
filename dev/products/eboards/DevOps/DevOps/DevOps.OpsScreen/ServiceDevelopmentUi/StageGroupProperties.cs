using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Network;

namespace DevOps.OpsScreen.ServiceDevelopmentUi
{
    internal class StageGroupProperties
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
}

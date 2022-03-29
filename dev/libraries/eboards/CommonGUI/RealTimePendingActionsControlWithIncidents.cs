using System;
using System.Drawing;
using LibCore;
using Network;
using TransitionObjects;
namespace CommonGUI
{
    public class RealTimePendingActionsControlWithIncidents : PendingActionsControlWithIncidents
    {
        NodeTree nt;
        public RealTimePendingActionsControlWithIncidents(OpsControlPanelBase mainPanel, NodeTree model,
                                                   ProjectManager prjmanager, Boolean IsTrainingMode,
                                                   Color OperationsBackColor, Color GroupPanelBackColor,
                                                   GameManagement.NetworkProgressionGameFile gameFile)
            : base(mainPanel, model, prjmanager, IsTrainingMode, OperationsBackColor, GroupPanelBackColor, gameFile)
        {
            nt = model;
        }



      


        protected string FormatAutoTime(int offset)
        {
            if (nt != null)
            {
                DateTime time = nt.GetNamedNode("CurrentTime").GetDateTimeAttribute("world_time_at_zero_seconds", DateTime.Now).AddSeconds(offset);

                return CONVERT.Format("{0}:{1:00}:{2:00}", time.Hour, time.Minute, time.Second);
            }
            else
            {
                nt = GetNodeTree();

                DateTime time = nt.GetNamedNode("CurrentTime").GetDateTimeAttribute("world_time_at_zero_seconds", DateTime.Now).AddSeconds(offset);

                return CONVERT.Format("{0}:{1:00}:{2:00}", time.Hour, time.Minute, time.Second);
            }
        }

        protected override string BuildTimeString(int timevalue)
        {
            return FormatAutoTime(timevalue);
        }


    }
}

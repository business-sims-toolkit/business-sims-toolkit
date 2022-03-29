using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using CommonGUI;
using LibCore;
using ResizingUi;

namespace DevOps.OpsScreen.ServiceDevelopmentUi
{
    internal class StageFailurePanel : FlickerFreePanel
    {
        public StageFailurePanel ()
        {
            Visible = false;

            stageToFailureMessage = new Dictionary<ILinkedStage, string>();

            failureMessageLabel = new Label
            {
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                ForeColor = CONVERT.ParseHtmlColor("#ff2d4e") // TODO skin file
            };
            Controls.Add(failureMessageLabel);
        }

        public void AddStageFailureMessage (ILinkedStage stage, string failureMessage)
        {
            stage.StageStatusChanged += stage_StageStatusChanged;

            stageToFailureMessage[stage] = failureMessage;
        }

        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                foreach (var stage in stageToFailureMessage.Keys)
                {
                    stage.StageStatusChanged -= stage_StageStatusChanged;
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

        void DoSize ()
        {
            failureMessageLabel.Bounds = new Rectangle(0, 0, Width, Height);
        }

        void stage_StageStatusChanged (object sender, EventArgs e)
        {
            var stage = (ILinkedStage) sender;

            if (! stage.HasFailedStage || ! stageToFailureMessage.ContainsKey(stage)) return;
            
            failureMessageLabel.Text = stageToFailureMessage[stage];
                
            failureMessageLabel.Font = failureMessageLabel.GetFontToFit(FontStyle.Bold, failureMessageLabel.Text,
                new SizeF(failureMessageLabel.Width, failureMessageLabel.Height));
            Show();
            BringToFront();
        }

        readonly Label failureMessageLabel;
        readonly Dictionary<ILinkedStage, string> stageToFailureMessage;
        
    }
}

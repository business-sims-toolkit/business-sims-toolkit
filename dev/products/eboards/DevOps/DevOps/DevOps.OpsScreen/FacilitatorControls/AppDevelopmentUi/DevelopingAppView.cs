using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

using Algorithms;
using CommonGUI;
using CoreUtils;
using Events;
using DevOps.OpsEngine;
using LibCore;
using Network;
using ResizingUi;
using ResizingUi.Component;
using ResizingUi.Interfaces;

// ReSharper disable ParameterHidesMember

namespace DevOps.OpsScreen.FacilitatorControls.AppDevelopmentUi
{
    internal class DevelopingAppView : FlickerFreePanel, IDynamicSharedFontSize
    {
        public Node ServiceNode { get; }

        public DevelopingAppView (Node serviceNode, DevelopingAppTerminator appTerminator)
        {
            ServiceNode = serviceNode;
            ServiceNode.AttributesChanged += ServiceNode_AttributesChanged;

            this.appTerminator = appTerminator;

            columnsToBounds = new Dictionary<AppDevelopmentColumn, RectangleF>();
            
            columnsToTexts = new Dictionary<AppDevelopmentColumn, Func<Node, string>>
            {
                { AppDevelopmentColumn.ServiceId, service => service.GetAttribute("service_id") },
                { AppDevelopmentColumn.ProductChoice, service => $"{service.GetAttribute("product_id")} {service.GetAttribute("platform")}" },

                { AppDevelopmentColumn.DevOneChoice, service => service.GetAttribute("dev_one_selection", "-") },
                { AppDevelopmentColumn.DevTwoChoice, service => service.GetAttribute("dev_two_selection", "-") },
                { AppDevelopmentColumn.TestChoice, service => service.GetAttribute("test_environment_selection", "-")},
                { AppDevelopmentColumn.TestTimeRemaining, service =>
                    {
                        if (service.GetAttribute("status") == ServiceStatus.TestDelay)
                        {
                            var testTimeRemaining = ServiceNode.GetIntAttribute("delayRemaining", 0);

                            return testTimeRemaining > 0
                                ? CONVERT.FormatTimeFourDigits(testTimeRemaining)
                                : "--:--";
                        }
                        else
                        {
                            return " ";
                        }
                    }
                },
                { AppDevelopmentColumn.ReleaseChoice, service => service.GetAttribute("release_selection", "-") },
                { AppDevelopmentColumn.EnclosureChoice, service => service.GetAttribute("enclosure_selection", "-") },
                {
                    AppDevelopmentColumn.Status, service =>
                    {
                        var currentStatus = service.GetAttribute("status");

                        switch (currentStatus)
                        {
                            case ServiceStatus.Dev:
                                return "In Dev";
                            case ServiceStatus.Test:
                                return "In Test";
                            case ServiceStatus.TestDelay:
                                return "Testing";
                            case ServiceStatus.Release:
                                return "Release";
                            case ServiceStatus.Installing:
                            case ServiceStatus.Live:
                                return "Deployed";
                            default:
                                return " - ";
                        }
                    }
                }
            };

            columnsToImages = new Dictionary<AppDevelopmentColumn, Image>
            {
                { AppDevelopmentColumn.ServiceIcon, Repository.TheInstance.GetImage(AppInfo.TheInstance.Location +
                                                                                  $@"\images\icons\{ServiceNode.GetAttribute("icon")}_default.png") }

            };

            animationComponent = new ControlAnimationComponent();
            animationComponent.AnimationTick += animationComponent_AnimationTick;

            DoSize();
        }

        public void AnimateTo (PointF newLocation, Color rowColour)
        {
            if (newLocation == Location && this.rowColour.EqualsByComponents(rowColour))
            {
                return;
            }

            animationComponent.AnimateTo(
                new AnimationProperties
                {
                    Location = Location,
                    Colour = this.rowColour
                }, 
                new AnimationProperties
                {
                    Location = newLocation,
                    Colour = rowColour
                }, 0.1f);

        }

        public float FontSize
        {
            get => fontSize;
            set
            {
                fontSize = value;
                Invalidate();
            }
        }
        public float FontSizeToFit
        {
            get => fontSizeToFit;
            private set
            {
                if (Math.Abs(fontSizeToFit - value) > float.Epsilon)
                {
                    fontSizeToFit = value;
                    OnFontSizeToFitChanged();
                }
            }
        }
        public event EventHandler FontSizeToFitChanged;

        protected override void OnSizeChanged (EventArgs e)
        {
            DoSize();
        }

        void DoSize ()
        {
            columnsToBounds.Clear();

            var columnWidth = Width / (float) AppDevelopmentColumnInfo.ColumnOrder.Count;
            var x = 0f;
            foreach (var column in AppDevelopmentColumnInfo.ColumnOrder)
            {
                columnsToBounds.Add(column, new RectangleF(x, 0, columnWidth, Height));
                x += columnWidth;
            }

            UpdateFontSize();

            Invalidate();
        }

        protected override void OnPaint (PaintEventArgs e)
        {
            UpdateFontSize();

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            using (var backBrush = new SolidBrush(rowColour))
            {
                e.Graphics.FillRectangle(backBrush, new Rectangle(0, 0, Width, Height));
            }

            var columnTexts = columnsToTexts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Invoke(ServiceNode));
            
            using (var font = new Font(SkinningDefs.TheInstance.GetFontName(), fontSize, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                foreach (var column in AppDevelopmentColumnInfo.ColumnOrder)
                {
                    if (columnsToTexts.ContainsKey(column))
                    {
                        RenderText(e.Graphics, columnTexts[column], columnsToBounds[column], font);
                    }

                    if (columnsToImages.ContainsKey(column))
                    {
                        RenderImage(e.Graphics, columnsToImages[column], columnsToBounds[column]);
                    }
                }
                
            }

        }

        protected override void OnMouseClick (MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // TODO show menu
                menu?.Close();

                menu = new DevOpsLozengePopupMenu
                {
                    BackColor = CONVERT.ParseHtmlColor("#CDCDCD")
                };
				
                menu.AddHeading("Terminate App");
                menu.AddDivider();

	            var isAppLive = ServiceNode.GetAttribute("deployment_stage_status") == ServiceStageStatus.Completed;

				DevOpsLozengePopupMenu.AddMenuItem(menu, "Undo", menu_Undo, !isAppLive);
	            DevOpsLozengePopupMenu.AddMenuItem(menu, "Abort", menu_Abort, !isAppLive);
	            DevOpsLozengePopupMenu.AddMenuItem(menu, "Close", menu_Closed, true, @"lozenges\cancel.png");
				
                menu.FormClosed += menu_Closed;

                menu.Show(TopLevelControl, this, PointToScreen(new Point(e.X, e.Y)));
            }
            
        }

        void menu_Undo (object sender, EventArgs e)
        {
            const string message = "Are you sure you want to undo this service?\r\nThis should only be used if the service was started accidentally.";
            const string caption = "Undo Service";

            ShowConfirmationPopup(caption, message, AppDevelopmentCommandType.UndoService);
        }

        void menu_Abort (object sender, EventArgs e)
        {
            const string message = "Are you sure you want to cancel the development of this service?";
            const string caption = "Cancel Service";

            ShowConfirmationPopup(caption, message, AppDevelopmentCommandType.CancelService);
        }

        void ShowConfirmationPopup (string caption, string message, string command)
        {
            const MessageBoxButtons buttons = MessageBoxButtons.YesNo;

            var result = MessageBox.Show(this, message, caption, buttons);

            if (result == DialogResult.Yes)
            {
                appTerminator.TerminateApp(ServiceNode, command);
            }
        }

        void menu_Closed (object sender, EventArgs e)
        {
            if (menu != null)
            {
                menu.Dispose();
                menu = null;
            }
        }
        
        void UpdateFontSize ()
        {
            var columnTexts = columnsToTexts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Invoke(ServiceNode));

            var columnSizes = (from boundsKvp in columnsToBounds
                               join textsKvp in columnTexts
                                   on boundsKvp.Key equals textsKvp.Key
                               select boundsKvp.Value.Size).ToList();

	        const string templateText = "XXXXXXXXX";
	        var minColumnSize = columnSizes.Aggregate((s1, s2) => s1.Width < s2.Width ? s1 : s2);

            FontSizeToFit = this.GetFontSizeInPixelsToFit(FontStyle.Regular, templateText, minColumnSize);
        }

        static void RenderText (Graphics graphics, string text, RectangleF bounds, Font font)
        {
            var textAlignment = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            
            using (var textBrush = new SolidBrush(Color.White))
            {
                graphics.DrawString(text, font, textBrush, bounds, textAlignment);
            }
        }

        static void RenderImage (Graphics graphics, Image image, RectangleF bounds)
        {
            var imageWidth = Math.Min(bounds.Width, bounds.Height - 4);

            graphics.DrawImage(image, bounds.AlignRectangle(imageWidth, imageWidth));
        }

        void OnFontSizeToFitChanged()
        {
            FontSizeToFitChanged?.Invoke(this, EventArgs.Empty);
        }

        void ServiceNode_AttributesChanged(Node sender, ArrayList attrs)
        {
            Invalidate();
        }

        void animationComponent_AnimationTick(object sender, EventArgs<AnimationProperties> e)
        {
            if (e.Parameter.Location.HasValue)
            {
                Location = new Point((int)e.Parameter.Location?.X, (int)e.Parameter.Location?.Y);
            }

            if (e.Parameter.Colour.HasValue)
            {
                rowColour = e.Parameter.Colour.Value;
            }

            Invalidate();
        }

        Color rowColour;

        readonly ControlAnimationComponent animationComponent;
        
        readonly Dictionary<AppDevelopmentColumn, RectangleF> columnsToBounds;
        readonly Dictionary<AppDevelopmentColumn, Func<Node, string>> columnsToTexts;
        readonly Dictionary<AppDevelopmentColumn, Image> columnsToImages;

        readonly DevelopingAppTerminator appTerminator;


        float fontSize = 12;
        float fontSizeToFit;

        DevOpsLozengePopupMenu menu;
    }
}

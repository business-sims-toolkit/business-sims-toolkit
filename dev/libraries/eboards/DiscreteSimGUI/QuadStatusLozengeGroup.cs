using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using LibCore;
using Network;

using CoreUtils;

namespace DiscreteSimGUI
{
    /// <summary>
    /// This panel contains all the business services for the application 
    /// only Business services which are not RETIRED will be shown 
    /// </summary>
    public class QuadStatusLozengeGroup : CommonGUI.FlickerFreePanel
    {
        protected NodeTree _Network;
        protected bool _UseMaskPath = false;

        protected Size ItemSize;
        protected ArrayList items = new ArrayList();

        protected Hashtable familyToNode = new Hashtable();

        // We need to keep the lozenges in the order that we first see that family name.
        // This means that if we upgrade a service then we stil place the new lozenge in
        // the same order. To do this we keep an ArrayList of family names.
        protected ArrayList familyNames = new ArrayList();

        protected Hashtable NodesNonRetired = new Hashtable();
        protected Hashtable NodesRetired = new Hashtable();
        protected ArrayList MonitorPotentials = new ArrayList();
        protected ArrayList DisplayLocations = new ArrayList();
        protected Hashtable DisplaySpots = new Hashtable();
        protected VideoBoxFlashReplacement trackFlash = null;
        protected Image MyCentralPic = null;
        protected Brush backBrush = null;

        protected Hashtable PositionalPts = new Hashtable();
        protected ArrayList PositionalFamilys = new ArrayList();
        protected int PositionalMaxValue = 1;

        protected Node BusinessServicesGroup;

        //private int titleHeight=0;
        protected Label ViewTitle;

        protected bool ShowTitle = false;
        protected bool ShowFlash = true;

        protected ArrayList dispOrderNames = new ArrayList();

        protected bool inTrainingMode;
        protected string FlashFileName_Normal = "\\flash\\gamebackdrop.swf";
        protected string FlashFileName_Training = "\\flash\\gamebackdrop.swf";

        Point [] lozengeMaskPoints;
        protected bool isESM = SkinningDefs.TheInstance.GetBoolData("esm_sim", false);

        public virtual void MoveLozenge (int index, Point location)
        {
            DisplaySpots[index] = location;
            Invalidate();
            LayoutItems();
        }

        public virtual void BuildLozengesLocations ()
        {
            DisplaySpots.Add(1, new Point(329, 74));
            DisplaySpots.Add(2, new Point(476, 28));
            DisplaySpots.Add(3, new Point(625, 74));
            DisplaySpots.Add(4, new Point(425, 129));
            DisplaySpots.Add(5, new Point(529, 129));
            DisplaySpots.Add(6, new Point(336, 191));
            DisplaySpots.Add(7, new Point(618, 191));
            DisplaySpots.Add(8, new Point(230, 209));
            DisplaySpots.Add(9, new Point(724, 209));
            DisplaySpots.Add(10, new Point(307, 289));
            DisplaySpots.Add(11, new Point(647, 289));
            DisplaySpots.Add(12, new Point(230, 369));
            DisplaySpots.Add(13, new Point(336, 387));
            DisplaySpots.Add(14, new Point(618, 387));
            DisplaySpots.Add(15, new Point(724, 369));
            DisplaySpots.Add(16, new Point(425, 449));
            DisplaySpots.Add(17, new Point(529, 449));
            DisplaySpots.Add(18, new Point(329, 504));
            DisplaySpots.Add(19, new Point(477, 549));
            DisplaySpots.Add(20, new Point(625, 504));
        }

	    public void RearrangeRoundOutside ()
	    {
		    RearrangeRoundOutside(10, 10, 10, 10);
	    }

		public void RearrangeRoundOutside (int leftMargin, int rightMargin, int topMargin, int bottomMargin)
        {
			int lozengeHeight = 42;
            int lozengeWidth = 105;

            var logicallyOrderedSpots = new List<Point>();

            int rows = 9;
            var yGap = ((Height - topMargin - bottomMargin) - (rows * lozengeHeight)) / (float) (rows - 1);
            for (float y = topMargin; (y + lozengeHeight) <= (Height - bottomMargin); y += (lozengeHeight + yGap))
            {
                logicallyOrderedSpots.Add(new Point (leftMargin, (int) y));
                logicallyOrderedSpots.Add(new Point (Width - rightMargin - lozengeWidth, (int) y));
            }

            int columns = 7;
            var xGap = (Width - (leftMargin + rightMargin) - (2 * lozengeWidth) - (columns * lozengeWidth)) / (float) (columns + 1);
            for (float x = (leftMargin + lozengeWidth + xGap); (x + lozengeWidth) <= (Width - rightMargin - lozengeWidth); x += (lozengeWidth + xGap))
            {
                logicallyOrderedSpots.Add(new Point ((int) x, topMargin));
                logicallyOrderedSpots.Add(new Point ((int) x, Height - bottomMargin - lozengeHeight));
            }

            var spotOrdering = new List<int> { 1, 25, 0, 16, 29, 3, 2, 19, 31, 5, 4, 21, 17, 30, 6, 15, 18, 28 };
            for (int i = 0; i < logicallyOrderedSpots.Count; i++)
            {
                if (! spotOrdering.Contains(i))
                {
                    spotOrdering.Add(i);
                }
            }

	        DisplaySpots.Clear();
            for (int i = 0; i < spotOrdering.Count; i++)
            {
	            if (i < logicallyOrderedSpots.Count)
	            {
		            DisplaySpots.Add(i + 1, logicallyOrderedSpots[spotOrdering[i]]);
	            }
            }

            LayoutItems();
        }

        public virtual void Build_DisplayLocations ()
        {
            int x = 1012;
            int y = 420;
            int quarterX = x / 4;
            int quarterY = y / 4;
            int lozengeHeight = 42;
            int lozengeWidth = 105;
            int paddingX = 10; //horizontal padding from edge of screen in pixels;
            int innerPaddingX = 3; //horizontal padding between lozenges in pixels;
            int innerPaddingY = 3; //vertical padding between lozenges in pixels;

            //using a hashtable to store the insertion order.
            //Populating top left hand quadrant, adjacent to quarteryY
            DisplaySpots.Clear();

            DisplaySpots.Add(12, new Point(paddingX, quarterY * 1 - (lozengeHeight + innerPaddingY) * 1));
            DisplaySpots.Add(1, new Point(paddingX, quarterY * 1 - (lozengeHeight + innerPaddingY) * 2));

            DisplaySpots.Add(7, new Point(paddingX, quarterY * 1 + (lozengeHeight + innerPaddingY) * 0));
            DisplaySpots.Add(17, new Point(paddingX, quarterY * 1 + (lozengeHeight + innerPaddingY) * 1));

            //Populating bottom left hand quadrant, adjacent to quarteryY
            DisplaySpots.Add(13, new Point(paddingX, quarterY * 3 - (lozengeHeight + innerPaddingY) * 1));
            DisplaySpots.Add(2, new Point(paddingX, quarterY * 3 - (lozengeHeight + innerPaddingY) * 2));

            DisplaySpots.Add(8, new Point(paddingX, quarterY * 3 + (lozengeHeight + innerPaddingY) * 0));
            DisplaySpots.Add(18, new Point(paddingX, quarterY * 3 + (lozengeHeight + innerPaddingY) * 1));

            //Populating top right hand quadrant, adjacent to quarteryY
            DisplaySpots.Add(14,
                new Point(x - (lozengeWidth + paddingX), quarterY * 1 - (lozengeHeight + innerPaddingY) * 1));
            DisplaySpots.Add(3,
                new Point(x - (lozengeWidth + paddingX), quarterY * 1 - (lozengeHeight + innerPaddingY) * 2));

            DisplaySpots.Add(9,
                new Point(x - (lozengeWidth + paddingX), quarterY * 1 + (lozengeHeight + innerPaddingY) * 0));
            DisplaySpots.Add(19,
                new Point(x - (lozengeWidth + paddingX), quarterY * 1 + (lozengeHeight + innerPaddingY) * 1));


            //Populating bottom right hand quadrant, adjacent to quarteryY
            DisplaySpots.Add(15,
                new Point(x - (lozengeWidth + paddingX), quarterY * 3 - (lozengeHeight + innerPaddingY) * 1));
            DisplaySpots.Add(4,
                new Point(x - (lozengeWidth + paddingX), quarterY * 3 - (lozengeHeight + innerPaddingY) * 2));

            DisplaySpots.Add(10,
                new Point(x - (lozengeWidth + paddingX), quarterY * 3 + (lozengeHeight + innerPaddingY) * 0));
            DisplaySpots.Add(20,
                new Point(x - (lozengeWidth + paddingX), quarterY * 3 + (lozengeHeight + innerPaddingY) * 1));

            //Populating bottom line from half outwards i.e adjacent to halfX
            DisplaySpots.Add(21,
                new Point(quarterX * 2 - ((lozengeWidth + innerPaddingX) * 3),
                    quarterY * 3 + (lozengeHeight + innerPaddingY) * 1));
            DisplaySpots.Add(11,
                new Point(quarterX * 2 - ((lozengeWidth + innerPaddingX) * 2),
                    quarterY * 3 + (lozengeHeight + innerPaddingY) * 1));
            DisplaySpots.Add(5,
                new Point(quarterX * 2 - ((lozengeWidth + innerPaddingX) * 1),
                    quarterY * 3 + (lozengeHeight + innerPaddingY) * 1));

            DisplaySpots.Add(6,
                new Point(quarterX * 2 + ((lozengeWidth + innerPaddingX) * 0),
                    quarterY * 3 + (lozengeHeight + innerPaddingY) * 1));
            DisplaySpots.Add(16,
                new Point(quarterX * 2 + ((lozengeWidth + innerPaddingX) * 1),
                    quarterY * 3 + (lozengeHeight + innerPaddingY) * 1));
            DisplaySpots.Add(22,
                new Point(quarterX * 2 + ((lozengeWidth + innerPaddingX) * 2),
                    quarterY * 3 + (lozengeHeight + innerPaddingY) * 1));

            //Populating top line from half outwards i.e adjacent to halfX
            DisplaySpots.Add(27,
                new Point(quarterX * 2 - ((lozengeWidth + innerPaddingX) * 3),
                    quarterY * 1 - (lozengeHeight + innerPaddingY) * 2));
            DisplaySpots.Add(25,
                new Point(quarterX * 2 - ((lozengeWidth + innerPaddingX) * 2),
                    quarterY * 1 - (lozengeHeight + innerPaddingY) * 2));
            DisplaySpots.Add(23,
                new Point(quarterX * 2 - ((lozengeWidth + innerPaddingX) * 1),
                    quarterY * 1 - (lozengeHeight + innerPaddingY) * 2));

            DisplaySpots.Add(24,
                new Point(quarterX * 2 + ((lozengeWidth + innerPaddingX) * 0),
                    quarterY * 1 - (lozengeHeight + innerPaddingY) * 2));
            DisplaySpots.Add(26,
                new Point(quarterX * 2 + ((lozengeWidth + innerPaddingX) * 1),
                    quarterY * 1 - (lozengeHeight + innerPaddingY) * 2));
            DisplaySpots.Add(28,
                new Point(quarterX * 2 + ((lozengeWidth + innerPaddingX) * 2),
                    quarterY * 1 - (lozengeHeight + innerPaddingY) * 2));
        }

        public virtual void SetLozengeMaskPoints (Point [] points)
        {
            lozengeMaskPoints = points;

            if (isESM)
            {
                foreach (QuadStatusLozenge_ESM lozenge in items)
                {
                    lozenge.SetMaskBoundingPolygon(lozengeMaskPoints);
                }

            }
            else
            {
                foreach (QuadStatusLozenge lozenge in items)
                {
                    lozenge.SetMaskBoundingPolygon(lozengeMaskPoints);
                }
            }
        }

        protected virtual void SetupShape ()
        {
            int h = SkinningDefs.TheInstance.GetIntData("esm_lozenges_height", 40);
            int w = SkinningDefs.TheInstance.GetIntData("esm_lozenges_width", 105);
            this.ItemSize = new Size(w, h);
            if (isESM)
            {
                this.BackColor = Color.Transparent;
            }
            else
            {
                this.BackColor = Color.White;
            }
            backBrush = new SolidBrush(this.BackColor);
        }

        public QuadStatusLozengeGroup (NodeTree nt, bool IsTrainingGame, bool UseMaskPath)
            : this(nt, IsTrainingGame, UseMaskPath, true)
        {
        }

        public QuadStatusLozengeGroup (NodeTree nt, bool IsTrainingGame, bool UseMaskPath, bool useFlash)
            : this(nt, IsTrainingGame, UseMaskPath, useFlash, null, null)
        {
        }

        public QuadStatusLozengeGroup (NodeTree nt, bool IsTrainingGame, bool UseMaskPath, bool useFlash, string normal,
                                       string training)
        {
            inTrainingMode = IsTrainingGame;

            _Network = nt;
            _UseMaskPath = UseMaskPath;

            if (useFlash)
            {
                if ((normal != null) || (training != null))
                {
                    SetFlashFileNames(normal, training);
                }
                else
                {
                    SetFlashFileNames();
                }
            }

            SetTrainingFlashBehaviour(IsTrainingGame);
            if (SkinningDefs.TheInstance.GetBoolData("esm_sim", false))
            {
                BuildLozengesLocations();
            }
            else
            {
                Build_DisplayLocations();
            }
            LoadBackImage();
            SetupShape();

            ViewTitle = new Label();
            ViewTitle.BackColor = this.BackColor;
            ViewTitle.ForeColor = Color.Black;
            ViewTitle.TextAlign = ContentAlignment.MiddleLeft;
            ViewTitle.Font = CoreUtils.SkinningDefs.TheInstance.GetFont(10f, FontStyle.Bold);
            ViewTitle.Text = "Business Services";
            ViewTitle.Size = new Size(120, 18);
            ViewTitle.Location = new Point(0, 0);
            ViewTitle.Visible = ShowTitle;
            this.Controls.Add(ViewTitle);

            BusinessServicesGroup = _Network.GetNamedNode("Business Services Group");
            BusinessServicesGroup.ChildAdded +=
                BusinessServicesGroup_ChildAdded;
            BusinessServicesGroup.ChildRemoved +=
                BusinessServicesGroup_ChildRemoved;

            BuildMonitoring();

            if (useFlash)
            {
                BuildFlashSystem();
            }
            else
            {
                BackgroundImage = null;
                BackColor = Color.Transparent;
            }
            LayoutItems();
        }

        protected virtual void SetTrainingFlashBehaviour (bool IsTrainingGame)
        {
            if (IsTrainingGame)
            {
                ShowFlash = false;
            }
        }

        protected virtual void SetFlashFileNames ()
        {
            FlashFileName_Normal = "\\flash\\gamebackdrop.swf";
            FlashFileName_Training = "\\flash\\gamebackdrop.swf";
        }

        public void SetFlashFileNames (string normal, string training)
        {
            FlashFileName_Normal = normal;
            FlashFileName_Training = training;
        }

        public virtual void LoadBackImage ()
        {
            string centre = LibCore.AppInfo.TheInstance.Location + "\\images\\panels\\TrainingGamePanel.png";
            MyCentralPic = Repository.TheInstance.GetImage(centre);
        }

        public virtual void BuildFlashSystem ()
        {
            trackFlash = new VideoBoxFlashReplacement();

            string ffile = FlashFileName_Normal;
            if (inTrainingMode)
            {
                ffile = FlashFileName_Training;
            }
            trackFlash.LoadFile(LibCore.AppInfo.TheInstance.Location + ffile);
            trackFlash.Pause();
            this.Controls.Add(trackFlash);
            trackFlash.SendToBack();

            // : Fix for 3738 (black line under title bar in CA).  Made the vertical offset
            // (originally 1 pixel) skinnable, with CA specifying 0.
            trackFlash.Location = new Point(1, SkinningDefs.TheInstance.GetIntData("gameboard_y_offset", 1));

            trackFlash.Size = new Size(1000, 415);

            if (! ShowFlash)
            {
                trackFlash.Visible = false;
                this.BackgroundImage = MyCentralPic;
            }
        }

        public virtual void ShowRunningFlash ()
        {
            LoadFlash();
            if (trackFlash != null)
            {
                if (NonPersistentGlobalOptions.AnimateMainGameFlash)
                {
                    trackFlash.Play();
                }
            }
        }

        protected virtual void LoadFlash ()
        {
            if (trackFlash != null)
            {
                if (trackFlash.State == Media.MediaState.Unloaded)
                {
                    string ffile;
                    if (inTrainingMode)
                    {
                        ffile = FlashFileName_Training;
                    }
                    else
                    {
                        ffile = FlashFileName_Normal;
                    }

                    trackFlash.LoadFile(LibCore.AppInfo.TheInstance.Location + ffile);
                }

                trackFlash.Loop = true;
            }
        }

        public virtual void ShowPausedFlash ()
        {
            LoadFlash();
            if (trackFlash != null)
            {
                trackFlash.Pause();
            }
        }

        /// <summary>
        /// Dispose ...
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose (bool disposing)
        {
            if (disposing)
            {
                if (trackFlash != null)
                {
                    trackFlash.Dispose();

                    this.SuspendLayout();
                    this.Controls.Remove(trackFlash);
                    this.ResumeLayout(false);

                    trackFlash = null;
                }

                DisposeMonitoring();
                if (BusinessServicesGroup != null)
                {
                    BusinessServicesGroup.ChildAdded -=
                        BusinessServicesGroup_ChildAdded;
                    BusinessServicesGroup.ChildRemoved -=
                        BusinessServicesGroup_ChildRemoved;
                }
            }
            base.Dispose(disposing);
        }

        public virtual void Play ()
        {
            if (trackFlash != null)
            {
                trackFlash.Play();
            }
        }

        public virtual void Stop ()
        {
            if (trackFlash != null)
            {
                trackFlash.Pause();
            }
        }

        public virtual void Rewind ()
        {
            if (trackFlash != null)
            {
                trackFlash.Rewind();
            }
        }

        public virtual void FastForward (double timesRealTime)
        {
        }

        public virtual void Reset ()
        {
        }

        protected void BuildMonitoring ()
        {
            DisposeMonitoring();
            this.dispOrderNames.Clear();

            PositionalPts.Clear();
            PositionalFamilys.Clear();
            PositionalMaxValue = 1;

            //
            Node biz_group = _Network.GetNamedNode("Business Services Group");
            foreach (Node biz in biz_group)
            {
                if (biz.GetAttribute("type") == "biz_service")
                {
                    AddNodeToMonitoring(biz, true);
                }
            }
            //
            LayoutItems();
        }

        protected virtual QuadStatusLozenge CreateLozenge (Node BizServiceNode, Random r, bool _UseMaskPath)
        {
            QuadStatusLozenge lozenge = new QuadStatusLozenge(BizServiceNode, r, _UseMaskPath);

            if (_UseMaskPath && (lozengeMaskPoints != null))
            {
                lozenge.SetMaskBoundingPolygon(lozengeMaskPoints);
            }

            return lozenge;
        }

        protected virtual QuadStatusLozenge_ESM CreateLozenge_ESM (Node BizServiceNode, Random r, bool _UseMaskPath)
        {
            QuadStatusLozenge_ESM lozenge = new QuadStatusLozenge_ESM(BizServiceNode, r, _UseMaskPath);

            if (_UseMaskPath && (lozengeMaskPoints != null))
            {
                lozenge.SetMaskBoundingPolygon(lozengeMaskPoints);
            }

            return lozenge;
        }

        protected int DetermineConnectionChildCount (Node ServiceNode)
        {
            int numberOfConnectionChildren = 0;
            if (ServiceNode != null)
            {
                foreach (Node n in ServiceNode.getChildren())
                {
                    string kidtype = n.GetAttribute("type");
                    if (kidtype.ToLower() == "connection")
                    {
                        numberOfConnectionChildren++;
                    }
                }
            }
            return numberOfConnectionChildren;
        }

        public virtual void setLozengeBackColorAfterCreation (QuadStatusLozenge sl)
        {
            sl.SetBackgroundColor(this.BackColor);
        }

        public virtual void setLozengeBackColorAfterCreation (QuadStatusLozenge_ESM sl)
        {
            sl.SetBackgroundColor(this.BackColor);
        }

        protected void AddNodeToMonitoring (Node BizServiceNode, Boolean OnlyAcceptParents)
        {
            string bizname = BizServiceNode.GetAttribute("name");

            string functionname = BizServiceNode.GetAttribute("biz_service_function");
            int kids_count = BizServiceNode.getChildren().Count;
            string retired_status = BizServiceNode.GetAttribute("retired");
            string desc = BizServiceNode.GetAttribute("desc");
            string shortdesc = BizServiceNode.GetAttribute("shortdesc");
            string iconname = BizServiceNode.GetAttribute("icon");

            int kid_connection_count = DetermineConnectionChildCount(BizServiceNode);

            Node reservations = _Network.GetNamedNode("LozengeServiceReservations");
            if (reservations != null)
            {
                foreach (Node reservation in reservations.getChildren())
                {
                    if (reservation.GetIntAttribute("index", 0) == (1 + dispOrderNames.Count))
                    {
                        dispOrderNames.Add(reservation.GetAttribute("business_service"));
                    }
                }
            }

            if (this.dispOrderNames.Contains(functionname) == false)
            {
                this.dispOrderNames.Add(functionname);
            }

            Random r = new Random(1);

            Boolean AddFlag = false;
            if (OnlyAcceptParents)
            {
                if (kid_connection_count > 0)
                {
                    AddFlag = true;
                }
            }
            else
            {
                AddFlag = true;
            }

            if (AddFlag)
            {
                if (isESM)
                {
                    QuadStatusLozenge_ESM sl = CreateLozenge_ESM(BizServiceNode, r, _UseMaskPath);
                    sl.SetDefaultWidthAndHeight(ItemSize.Width, ItemSize.Height);

                    sl.Name = "A Biz Status Lozenge";
                    this.Controls.Add(sl);
                    items.Add(sl);
                    familyToNode.Add(functionname, sl);
                    if (! familyNames.Contains(functionname))
                    {
                        familyNames.Add(functionname);
                        //System.Diagnostics.Debug.WriteLine(" Adding Family Name (A):"+functionname);
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine(" Adding Family Name (A1):"+functionname);
                    }
                    //LayoutItems();
                    sl.BringToFront();
                    setLozengeBackColorAfterCreation(sl);
                    if (this.PositionalFamilys.Contains(functionname) == false)
                    {
                        PositionalFamilys.Add(functionname);
                        PositionalPts.Add(PositionalMaxValue, functionname);
                        PositionalMaxValue++;
                    }

                    sl.SetState();
                }
                else
                {
                    QuadStatusLozenge sl = CreateLozenge(BizServiceNode, r, _UseMaskPath);
                    sl.SetDefaultWidthAndHeight(ItemSize.Width, ItemSize.Height);

                    sl.Name = "A Biz Status Lozenge";
                    this.Controls.Add(sl);
                    items.Add(sl);
                    familyToNode.Add(functionname, sl);
                    if (! familyNames.Contains(functionname))
                    {
                        familyNames.Add(functionname);
                        //System.Diagnostics.Debug.WriteLine(" Adding Family Name (A):"+functionname);
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine(" Adding Family Name (A1):"+functionname);
                    }
                    //LayoutItems();
                    sl.BringToFront();
                    setLozengeBackColorAfterCreation(sl);
                    if (this.PositionalFamilys.Contains(functionname) == false)
                    {
                        PositionalFamilys.Add(functionname);
                        PositionalPts.Add(PositionalMaxValue, functionname);
                        PositionalMaxValue++;
                    }

                    sl.SetState();
                }
            }
            else
                //if (processed== false)
            {
                if (familyToNode.ContainsKey(functionname) == false)
                {
                    if (! familyNames.Contains(functionname))
                    {
                        familyNames.Add(functionname);
                        //System.Diagnostics.Debug.WriteLine(" Adding Family Name (B):"+functionname);
                    }
                    MonitorPotentials.Add(BizServiceNode);
                    BizServiceNode.ChildAdded += BizServiceNode_ChildAdded;
                }
            }
        }

        protected void RemoveNodeFromMonitoring (Node BizServiceNode)
        {
        }

        protected void DisposeMonitoring ()
        {
            if (isESM)
            {
                //Removing the Items 
                foreach (QuadStatusLozenge_ESM smi in items)
                {
                    smi.Dispose();
                }
                items.Clear();
            }
            else
            {
                //Removing the Items 
                foreach (QuadStatusLozenge smi in items)
                {
                    smi.Dispose();
                }
                items.Clear();
            }

            foreach (Node n in MonitorPotentials)
            {
                n.ChildAdded += BizServiceNode_ChildAdded;
            }
            MonitorPotentials.Clear();
        }

        protected void BusinessServicesGroup_ChildAdded (Node sender, Node child)
        {
            int kid_connection_count = DetermineConnectionChildCount(child);

            if (kid_connection_count > 0)
            {
                AddNodeToMonitoring(child, false);
                this.LayoutItems();
            }
        }

        protected void BusinessServicesGroup_ChildRemoved (Node sender, Node child)
        {
        }

        /// <summary>
        /// Layout the individual application monitors on the screen.
        /// </summary>
        public virtual void LayoutItems ()
        {
            ViewTitle.Width = this.Width - 10;
            ViewTitle.Left = 5;

            if (items != null && items.Count > 0)
            {
                int y = ViewTitle.Height;
                int step = 1;

                //Walk through the list of functional names 
                foreach (string functionname in this.dispOrderNames)
                {
                    if (isESM)
                    {
                        QuadStatusLozenge_ESM smi = (QuadStatusLozenge_ESM) this.familyToNode[functionname];
                        if (smi != null)
                        {
                            int index = _Network.GetNamedNode(functionname)
                                .GetIntAttribute("lozenge_location_index", step);

                            if (DisplaySpots.ContainsKey(index))
                            {
                                smi.Location = (Point) DisplaySpots[index];
                            }
                            else
                            {
                                smi.Location = new Point(0, 0);
                            }
                        }
                        step++;
                    }
                    else
                    {
                        //System.Diagnostics.Debug.WriteLine("step: "+step.ToString() + "  " + functionname);
                        //extract the object fro this name
                        QuadStatusLozenge smi = (QuadStatusLozenge) this.familyToNode[functionname];
                        //If we have a lozenge for this name then position it 
                        //if not then step over this location as it needs to be reserved
                        //the service is present but has no children and not currently displayed 
                        if (smi != null)
                        {
                            //smi.Size = ItemSize;
                            if (DisplaySpots.ContainsKey(step))
                            {
                                smi.Location = (Point) DisplaySpots[step];
                                //System.Diagnostics.Debug.WriteLine("Family Name:"+functionname+" DC:" + display_count.ToString()+ " X:"+smi.Location.X.ToString() + "  Y:"+smi.Location.Y.ToString());
                            }
                            else
                            {
                                smi.Location = new Point(0, 0);
                                //System.Diagnostics.Debug.WriteLine("Family Name:"+functionname+" DC:" + display_count.ToString()+ " X:0 Y:0");
                            }
                        }
                        step++;
                    }
                }
            }
        }

        protected void BizServiceNode_ChildAdded (Node sender, Node child)
        {
            //this is a empty Business Service with a child link being added
            string familyname = child.Parent.GetAttribute("biz_service_function");
            string bizname = child.Parent.GetAttribute("name");

            //remove from Potential 
            MonitorPotentials.Remove(bizname);
            child.Parent.ChildAdded -= BizServiceNode_ChildAdded;

            //do we already have a monitored node for that family 
            if (familyToNode.ContainsKey(familyname))
            {
                //yes, need to reconnected to the parent 
                if (isESM)
                {
                    QuadStatusLozenge_ESM sl = (QuadStatusLozenge_ESM) familyToNode[familyname];
                    sl.setMonitoredNode(child.Parent);
                }
                else
                {
                    QuadStatusLozenge sl = (QuadStatusLozenge) familyToNode[familyname];
                    sl.setMonitoredNode(child.Parent);
                }
            }
            else
            {
                //remove from 
                AddNodeToMonitoring(child.Parent, true);
                this.LayoutItems();
            }


            //empty adding child links 
            //
            //check if we already have a monitored node of that family 
            //then change the monitored node for that item 
            //else 
            //  add a new monitored item 
            //remove from Potential node list

        }

        protected override void OnSizeChanged (EventArgs eventArgs)
        {
	        if (trackFlash != null)
	        {
		        trackFlash.Size = Size;
		        trackFlash.ZoomWithCropping(new Point (Width / 2, 0), new Point (trackFlash.VideoSize.Width / 2, 0));
	        }
		}

        /// <summary>
        /// used to redefine the upper left corner of the flash
        /// </summary>
        /// <param name="newPoint"></param>
        public virtual void SetFlashPosition (Point newPoint)
        {
            if (null != trackFlash) trackFlash.Location = newPoint;
        }

        public void RearrangeInLine (int [] ids)
        {
            //Redundant method that does nothing - it's a relic from the past! Exists for backward compatibility with old branch code (post March 2015)
        }

        public void DisableVideo ()
        {
            trackFlash.UnloadMedia();
            Controls.Remove(trackFlash);
        }
    }
}
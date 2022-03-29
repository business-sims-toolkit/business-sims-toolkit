using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Algorithms;
using CommonGUI;
using CoreUtils;
using LibCore;
using Network;

namespace ResizingUi
{
	public class AwtPanel : CascadedBackgroundPanel, ITimedClass
	{
		struct ColumnStateChange
		{
			public int Time;
			public int BlocksLit;
		}

		enum ChangeType
		{
			Worse,
			Same,
			Better
		}

	    readonly NodeTree model;
	    readonly string title;
	    readonly Node awtNode;
	    readonly Node timeNode;
	    readonly List<string> itemNames;
	    readonly Dictionary<string, Node> nameToItem;
		readonly Dictionary<string, int> nameToRandomFluctuation;
	    readonly Dictionary<Node, ColumnStateChange> itemToLastStateChange;
		IWatermarker watermarker;

		int sideMargin;
		int topMargin;
		int bottomMargin;
		int columnGap;
		int rowGap;

	    readonly Random random;

		Timer timer;

		public IWatermarker Watermarker
		{
			get => watermarker;

			set
			{
				watermarker = value;
				Invalidate();
			}
		}

		public AwtPanel (NodeTree model, string title, Node awtNode, IList<string> itemNames, Random random)
		{
			this.model = model;
			this.awtNode = awtNode;
			this.itemNames = new List<string> ();
			this.title = title;
			nameToItem = new Dictionary<string, Node> ();
			itemToLastStateChange = new Dictionary<Node, ColumnStateChange> ();
			nameToRandomFluctuation = new Dictionary<string, int> ();

			foreach (var itemName in itemNames)
			{
				this.itemNames.Add(itemName);
				var item = model.GetNamedNode(itemName);

				nameToItem.Add(itemName, item);
				if (item != null)
				{
					item.AttributesChanged += item_AttributesChanged;
				}
			}

			awtNode.AttributesChanged += awtNode_AttributesChanged;

			timeNode = model.GetNamedNode("CurrentTime");
			timeNode.AttributesChanged += timeNode_AttributesChanged;

			sideMargin = 20;
			topMargin = 20;
			bottomMargin = 30;
			columnGap = 4;
			rowGap = 4;

			this.random = random;

			timer = new Timer { Interval = 250 };
			timer.Tick += timer_Tick;

			TimeManager.TheInstance.ManageClass(this);
		}

		void timer_Tick (object sender, EventArgs args)
		{
			foreach (var name in nameToItem.Keys)
			{
				nameToRandomFluctuation[name] = (random.NextDouble() > 0.9) ? random.Next(-1, 2) : 0;
			}

			Invalidate();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				foreach (var item in nameToItem.Values)
				{
					if (item != null)
					{
						item.AttributesChanged -= item_AttributesChanged;
					}
				}

				awtNode.AttributesChanged -= awtNode_AttributesChanged;
				timeNode.AttributesChanged -= timeNode_AttributesChanged;

				timer.Dispose();

				TimeManager.TheInstance.UnmanageClass(this);
			}

			base.Dispose(disposing);
		}

		void awtNode_AttributesChanged (Node sender, ArrayList attributes)
		{
			Invalidate();
		}

		void timeNode_AttributesChanged (Node sender, ArrayList attributes)
		{
			Invalidate();
		}

		void item_AttributesChanged (Node sender, ArrayList attributes)
		{
			Invalidate();
		}

		protected override void OnSizeChanged (EventArgs e)
		{
			base.OnSizeChanged(e);

			Invalidate();
		}

		bool AreNodeAndAllParentsUp (Node node)
		{
			if (! node.GetBooleanAttribute("up", true))
			{
				return false;
			}

			if (node.GetBooleanAttribute("notinnetwork", false))
			{
				return true;
			}

			if (node.Parent != null)
			{
				return AreNodeAndAllParentsUp(node.Parent);
			}

			return true;
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint(e);

			using (var backBrush = new SolidBrush(Color.FromArgb(CoreUtils.SkinningDefs.TheInstance.GetIntData("cascaded_background_transparency", 255), BackColor)))
			{
				e.Graphics.FillRectangle(backBrush, new Rectangle(0, 0, Width, Height));
			}

			watermarker?.Draw(this, e.Graphics);

			int currentTime = timeNode.GetIntAttribute("seconds", 0);

			Color [] fullColours =
			{
				SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_ok", Color.Green),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_ok", Color.Green),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_warning", Color.Yellow),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_danger", Color.Orange),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_fault", Color.Red)
			};

			Color [] emptyColours =
			{
				SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_background_colour_ok", SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_empty", Color.Gray)),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_background_colour_ok", SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_empty", Color.Gray)),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_background_colour_warning", SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_empty", Color.Gray)),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_background_colour_danger", SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_empty", Color.Gray)),
				SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_background_colour_fault", SkinningDefs.TheInstance.GetColorDataGivenDefault("awt_colour_empty", Color.Gray))
			};

			int columnWidth = (Width - (2 * sideMargin) - ((itemNames.Count - 1) * columnGap)) / itemNames.Count;
			int rowHeight = (Height - topMargin - bottomMargin - ((fullColours.Length - 1) * rowGap)) / fullColours.Length;

			int x = sideMargin;
			string biggestColumnText = "9";
			using (var textBrush = new SolidBrush(ForeColor))
			using (var font = this.GetFontToFit(FontStyle.Bold, biggestColumnText, new SizeF(columnWidth, bottomMargin)))
			{
				for (int j = 0; j < itemNames.Count; j++)
				{
					int blocksLit = 0;
					int blocksLitWithoutRandomFluctuation = blocksLit;
					ChangeType change = ChangeType.Same;
					bool recordStateChange = false;

					var itemName = itemNames[j];
					var item = nameToItem[itemName];
					if (item == null)
					{
						item = model.GetNamedNode(itemName);
						if (item != null)
						{
							nameToItem[itemName] = item;
						}
					}

					if ((item != null) && awtNode.GetBooleanAttribute("up", true))
					{
						var dangerLevel = item.GetIntAttribute("danger_level", 0);
						int proportion;

						if (dangerLevel > 40)
						{
							proportion = 100;
						}
						else if (dangerLevel > 30)
						{
							proportion = 80;
						}
						else
						{
							proportion = 40;
						}

						blocksLit = proportion * fullColours.Length / 100;
						blocksLitWithoutRandomFluctuation = blocksLit;

						if (blocksLit == 2)
						{
							if (nameToRandomFluctuation.ContainsKey(itemName))
							{
								blocksLit += nameToRandomFluctuation[itemName];
							}
						}

						if (item.GetIntAttribute("goingDownInSecs").HasValue
							|| item.GetBooleanAttribute("goingDown", false)
							|| (item.GetIntAttribute("workingAround", 0) > 0))
						{
							blocksLit = emptyColours.Length - 1;
							blocksLitWithoutRandomFluctuation = blocksLit;
						}
						else if (!AreNodeAndAllParentsUp(item))
						{
							blocksLit = emptyColours.Length;
							blocksLitWithoutRandomFluctuation = blocksLit;
						}

						ColumnStateChange? previousStateChange = null;
						if (itemToLastStateChange.ContainsKey(item))
						{
							previousStateChange = itemToLastStateChange[item];
						}
						else
						{
							recordStateChange = true;
						}

						if (previousStateChange != null)
						{
							int flashDuration = 0;

							if (blocksLitWithoutRandomFluctuation < previousStateChange.Value.BlocksLit)
							{
								change = ChangeType.Better;

								flashDuration = 5;
							}
							else if (blocksLitWithoutRandomFluctuation > previousStateChange.Value.BlocksLit)
							{
								change = ChangeType.Worse;

								flashDuration = 10;
							}

							if (currentTime >= (previousStateChange.Value.Time + flashDuration))
							{
								recordStateChange = true;
							}
						}
					}

					for (int i = 0; i < fullColours.Length; i++)
					{
						Color colour = emptyColours[i];

						if (i < blocksLit)
						{
							colour = fullColours[i];

							if (change != ChangeType.Same)
							{
								var fadeColour = (change == ChangeType.Worse) ? Color.White : Color.Black;
								colour = (((currentTime % 2) == 1) ? colour : Maths.Lerp(0.25, colour, fadeColour));
							}
						}

						using (Brush brush = new SolidBrush(colour))
						{
							e.Graphics.FillRectangle(brush,
								new Rectangle(x, Height - bottomMargin - (i * (rowHeight + rowGap)) - rowHeight, columnWidth, rowHeight));
						}
					}

					if (recordStateChange)
					{
						itemToLastStateChange[item] = new ColumnStateChange { BlocksLit = blocksLitWithoutRandomFluctuation, Time = currentTime };
					}

					int pad = 20;

					e.Graphics.DrawString(CONVERT.Format("{0}", 1 + j),
						font, textBrush,
						new RectangleFromBounds { Left = x - pad, Width = columnWidth + pad + pad, Top = Height - bottomMargin, Bottom = Height }.ToRectangle(),
						new StringFormat(StringFormatFlags.FitBlackBox | StringFormatFlags.NoClip | StringFormatFlags.NoWrap) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near });

					x += columnWidth + columnGap;
				}
			}

			var style = (SkinningDefs.TheInstance.GetBoolData("awt_titles_in_bold", false) ? FontStyle.Bold : FontStyle.Regular);
			using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(topMargin, style))
			using (var brush = new SolidBrush (ForeColor))
			{
				e.Graphics.DrawString(title, font, brush, new Rectangle (0, 0, Width, topMargin), new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center });
			}
		}

		public int SideMargin
		{
			get => sideMargin;

			set
			{
				sideMargin = value;
				Invalidate();
			}
		}

		public int TopMargin
		{
			get => topMargin;

			set
			{
				topMargin = value;
				Invalidate();
			}
		}

		public int BottomMargin
		{
			get => bottomMargin;

			set
			{
				bottomMargin = value;
				Invalidate();
			}
		}

		public int ColumnGap
		{
			get => columnGap;

			set
			{
				columnGap = value;
				Invalidate();
			}
		}

		public int RowGap
		{
			get => rowGap;

			set
			{
				rowGap = value;
				Invalidate();
			}
		}

		public void Start ()
		{
			timer.Start();
		}

		public void Stop ()
		{
			timer.Stop();
		}

		public void Reset ()
		{
		}

		public void FastForward (double timesRealTime)
		{
		}
	}
}
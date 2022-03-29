using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using CommonGUI;
using CoreUtils;
using LibCore;
using Network;

namespace ResizingUi.TransactionsView
{
    public class TransactionRowView : FlickerFreePanel
    {
        readonly Node transaction;
	    Font romanFont;
	    Font boldFont;
	    float [] tabStops;
        readonly Dictionary<TransactionStatus, Color> statusToForeColour;
        readonly Dictionary<TransactionStatus, Color> statusToBackColour;
        readonly string [] longestColumnContents;

	    StringAlignment timeAlignment;
	    StringAlignment codeAlignment;
	    StringAlignment buAlignment;
	    StringAlignment statusAlignment;

	    IWatermarker watermarker;

        readonly bool useCascadedBackground;

        CascadedBackgroundProperties cascadedBackgroundProperties;
        public CascadedBackgroundProperties CascadedBackgroundProperties
        {
            set
            {
                if (cascadedBackgroundProperties != null)
                {
                    cascadedBackgroundProperties.PropertiesChanged -= cascadedBackgroundProperties_PropertiesChanged;
                }

                cascadedBackgroundProperties = value;

                if (cascadedBackgroundProperties != null)
                {
                    cascadedBackgroundProperties.PropertiesChanged += cascadedBackgroundProperties_PropertiesChanged;
                }

                Invalidate(new Rectangle(0, 0, Width, Height), true);
            }
        }

	    public IWatermarker Watermarker
	    {
		    get => watermarker;

		    set
		    {
			    watermarker = value;
		        Invalidate(new Rectangle(0, 0, Width, Height), true);
            }
	    }

        public TransactionRowView (Node transaction, bool useCascadedBackground = false)
	    {
		    this.transaction = transaction;
	        this.useCascadedBackground = useCascadedBackground;

            if (transaction != null)
		    {
			    transaction.AttributesChanged += transaction_AttributesChanged;
		    }

		    statusToForeColour = new Dictionary<TransactionStatus, Color>
			{
				{ TransactionStatus.Queued, SkinningDefs.TheInstance.GetColorData("transaction_queued_fore_colour", Color.Black) },
				{ TransactionStatus.AtRisk, SkinningDefs.TheInstance.GetColorData("transaction_at_risk_fore_colour", Color.Transparent) },
				{ TransactionStatus.Handled, SkinningDefs.TheInstance.GetColorData("transaction_handled_fore_colour", Color.Transparent)},
				{ TransactionStatus.Cancelled, SkinningDefs.TheInstance.GetColorData("transaction_cancelled_fore_colour", Color.Transparent)}
			};

			statusToBackColour = new Dictionary<TransactionStatus, Color>
		    {
			    { TransactionStatus.Queued, SkinningDefs.TheInstance.GetColorData("transaction_queued_back_colour", Color.Transparent) },
			    { TransactionStatus.AtRisk, SkinningDefs.TheInstance.GetColorData("transaction_at_risk_back_colour", Color.Orange) },
			    { TransactionStatus.Handled, SkinningDefs.TheInstance.GetColorData("transaction_handled_back_colour", Color.Green)},
			    { TransactionStatus.Cancelled, SkinningDefs.TheInstance.GetColorData("transaction_cancelled_back_colour", Color.Red)}
		    };

			longestColumnContents = new [] { "99:99:99", "WW999", "9", "Canceled" };

		    tabStops = new [] { 0, 0.25f, 0.5f, 0.75f };

			DoSize();
	    }

	    protected override void Dispose (bool disposing)
	    {
		    if (disposing)
		    {
			    if (transaction != null)
			    {
				    transaction.AttributesChanged -= transaction_AttributesChanged;
			    }

			    romanFont?.Dispose();
			    boldFont?.Dispose();
		    }

			base.Dispose(disposing);
	    }

	    void transaction_AttributesChanged (Node sender, ArrayList attributes)
	    {
			Invalidate();
	    }

	    protected override void OnSizeChanged (EventArgs e)
	    {
		    base.OnSizeChanged(e);
		    DoSize();
	    }

	    struct LayoutResults
	    {
		    public float FontSize { get; set; }
		    public IList<SizeF> ColumnSizes { get; set; }
			public IList<SizeF> ColumnTextSizes { get; set; }
		}

	    void DoSize ()
	    {
		    var results = CalculateLayout(Size);

			romanFont?.Dispose();
		    romanFont = SkinningDefs.TheInstance.GetPixelSizedFont(results.FontSize);

		    boldFont?.Dispose();
		    boldFont = SkinningDefs.TheInstance.GetPixelSizedFont(results.FontSize, FontStyle.Bold);

		    Invalidate();
	    }

		LayoutResults CalculateLayout (Size size)
		{
			var columnSizes = new List<SizeF> ();
		    for (int column = 0; column < longestColumnContents.Length; column++)
		    {
				columnSizes.Add(new SizeF (size.Width * (((column >= (tabStops.Length - 1)) ? 1 : tabStops[column + 1]) - tabStops[column]), size.Height));
		    }

			var results = new LayoutResults
			{
				FontSize = this.GetFontSizeInPixelsToFit(SkinningDefs.TheInstance.GetFontName(), FontStyle.Bold, longestColumnContents, columnSizes),
				ColumnSizes = columnSizes,
				ColumnTextSizes = new List<SizeF> ()
			};

			using (var graphics = CreateGraphics())
			using (var font = SkinningDefs.TheInstance.GetPixelSizedFont(results.FontSize, FontStyle.Bold))
			{
				foreach (var columnContents in longestColumnContents)
				{
					results.ColumnTextSizes.Add(graphics.MeasureString(columnContents, font));
				}
			}

			return results;
		}

		public float [] TabStops
	    {
		    get => tabStops;

		    set
		    {
			    tabStops = value;
				DoSize();
		    }
	    }

	    Color GetColour (TransactionStatus status, bool isForeground)
	    {
		    var table = (isForeground ? statusToForeColour : statusToBackColour);
		    var colour = table[status];

		    if (colour == Color.Transparent)
		    {
			    colour = BackColor;
		    }

	        if (! isForeground && useCascadedBackground)
	        {
	            colour = Color.FromArgb(SkinningDefs.TheInstance.GetIntData("cascaded_background_transparency", 255), colour);
	        }

			return colour;
	    }

        void RenderRow (Graphics graphics, TransactionStatus status)
        {
            var rectangle = new Rectangle(0, 0, Width, Height);
            using (var brush = new SolidBrush(GetColour(status, false)))
            {
                if (SkinningDefs.TheInstance.GetBoolData("transaction_use_rounded_corners", false))
                {
                    var cornerRadius = SkinningDefs.TheInstance.GetFloatData("transaction_rounded_corner_radius", 0.5f) * Height;

                    RoundedRectangle.FillRoundedRectangle(graphics, brush, rectangle, cornerRadius);
                }
                else
                {
                    graphics.FillRectangle(brush, rectangle);
                }
            }
        }

	    void RenderColumn (Graphics graphics, int column, TransactionStatus status, StringAlignment alignment, bool bold, string text, bool renderColumnRectangle)
	    {
			var rectangle = new RectangleF (Width * tabStops[column], 0, Width * (((column >= (tabStops.Length - 1)) ? 1 : tabStops[column + 1]) - tabStops[column]), Height);
            if (renderColumnRectangle)
            {
	            using (var brush = new SolidBrush(GetColour(status, false)))
	            {
		            if (SkinningDefs.TheInstance.GetBoolData("transaction_use_rounded_corners", false))
		            {
			            var cornerRadius = SkinningDefs.TheInstance.GetFloatData("transaction_rounded_corner_radius", 0.5f) * Height;

			            RoundedRectangle.FillRoundedRectangle(graphics, brush, rectangle, cornerRadius);
		            }
		            else
		            {
			            graphics.FillRectangle(brush, rectangle);
		            }
	            }
            }

		    var format = new StringFormat { Alignment = alignment, LineAlignment = StringAlignment.Center };
		    using (var brush = new SolidBrush (GetColour(status, true)))
		    {
			    graphics.DrawString(text, bold ? boldFont : romanFont, brush, rectangle, format);
		    }
		}

	    TransactionStatus TransactionStatus
	    {
		    get
		    {
			    switch (transaction.GetAttribute("status").ToLower())
			    {
				    case "queued":
					    return TransactionStatus.Queued;

				    case "at risk":
					case "delayed":
					    return TransactionStatus.AtRisk;

				    case "handled":
					    return TransactionStatus.Handled;

				    case "cancelled":
				    case "canceled":
					    return TransactionStatus.Cancelled;

					default:
						throw new Exception ("Unhandled transaction status");
				}
			}
	    }

	    string TransactionDisplayStatus
	    {
		    get
		    {
			    switch (TransactionStatus)
			    {
				    case TransactionStatus.Queued:
					    return SkinningDefs.TheInstance.GetData("status_name_queued", "Queued");

					case TransactionStatus.AtRisk:
					    return SkinningDefs.TheInstance.GetData("status_name_delayed", "At Risk");

				    case TransactionStatus.Handled:
					    return SkinningDefs.TheInstance.GetData("status_name_handled", "Handled");

				    case TransactionStatus.Cancelled:
					    return SkinningDefs.TheInstance.GetData("status_name_cancelled", "Canceled");

				    default:
					    throw new Exception ("Unhandled transaction status");
				}
			}
	    }
		
		protected override void OnPaint (PaintEventArgs e)
	    {
	        if (useCascadedBackground)
	        {
	            BackgroundPainter.Paint(this, e.Graphics, cascadedBackgroundProperties);
            }

			watermarker?.Draw(this, e.Graphics);

	        var renderRow = SkinningDefs.TheInstance.GetBoolData("transaction_render_row", false);

            if (renderRow)
	        {
                RenderRow(e.Graphics, TransactionStatus);
	        }

		    RenderColumn(e.Graphics, 0, TransactionStatus, timeAlignment, SkinningDefs.TheInstance.GetBoolData("transaction_time_bold", false), CONVERT.FormatTimeHms(transaction.Tree.GetNamedNode("CurrentTime").GetHmsAttribute("round_start_clock_time", 0) + transaction.GetIntAttribute("time", 0)), !renderRow);
		    RenderColumn(e.Graphics, 1, TransactionStatus, codeAlignment, SkinningDefs.TheInstance.GetBoolData("transaction_code_bold", false), transaction.GetAttribute("displayname"), !renderRow);

		    var businessUnit = transaction.Tree.GetNamedNode(transaction.GetAttribute("store"));
		    RenderColumn(e.Graphics, 2, TransactionStatus, buAlignment, SkinningDefs.TheInstance.GetBoolData("transaction_store_bold", false), businessUnit.GetAttribute("shortdesc"), !renderRow);

		    RenderColumn(e.Graphics, 3, TransactionStatus, statusAlignment, SkinningDefs.TheInstance.GetBoolData("transaction_status_bold", false), TransactionDisplayStatus, !renderRow);
	    }

	    public StringAlignment TimeAlignment
	    {
		    get => timeAlignment;

		    set
		    {
			    timeAlignment = value;
			    DoSize();
		    }
	    }

	    public StringAlignment CodeAlignment
	    {
		    get => codeAlignment;

		    set
		    {
			    codeAlignment = value;
			    DoSize();
		    }
	    }

	    public StringAlignment BuAlignment
	    {
		    get => buAlignment;

		    set
		    {
			    buAlignment = value;
			    DoSize();
		    }
	    }

	    public StringAlignment StatusAlignment
	    {
		    get => statusAlignment;

		    set
		    {
			    statusAlignment = value;
			    DoSize();
		    }
	    }

	    public override Size GetPreferredSize (Size proposedSize)
	    {
		    var results = CalculateLayout(proposedSize);

		    return new Size ((int) results.ColumnTextSizes.Sum(size => size.Width), proposedSize.Height);
	    }

        void cascadedBackgroundProperties_PropertiesChanged (object sender, EventArgs e)
        {
            Invalidate(new Rectangle(0, 0, Width, Height), true);
        }
	}
}
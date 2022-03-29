using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Network;

namespace ResizingUi
{
	public class BusinessServiceLozengeGroup : IDisposable
	{
		public delegate Control BusinessServiceLozengeCreator (Node businessService);
		public delegate void BusinessServiceLozengeArranger (IList<Control> businessServiceLozenges, List<List<Point>> leftColumns, List<List<Point>> rightColumns, List<Point> allLocations, Size lozengeSize);

	    readonly Node businessServicesNode;
	    readonly BusinessServiceLozengeCreator lozengeCreator;
		readonly BusinessServiceLozengeArranger lozengeArranger;

	    readonly List<Node> orderedBusinessServices;
	    readonly Dictionary<Node, Control> businessServiceToLozenge;

		Control control;
		Rectangle bounds;

		public BusinessServiceLozengeGroup (Node businessServicesNode, BusinessServiceLozengeCreator lozengeCreator, BusinessServiceLozengeArranger lozengeArranger = null)
		{
			this.businessServicesNode = businessServicesNode;
			this.lozengeCreator = lozengeCreator;
			this.lozengeArranger = lozengeArranger ?? DefaultBusinessServiceArranger;

			orderedBusinessServices = new List<Node> ();
			businessServiceToLozenge = new Dictionary<Node, Control> ();

			businessServicesNode.ChildAdded += businessServicesNode_ChildAdded;
			businessServicesNode.ChildRemoved += businessServicesNode_ChildRemoved;

			foreach (Node businessService in businessServicesNode.GetChildrenOfType("biz_service"))
			{
				AddBusinessService(businessService);
			}
		}

		public void Dispose ()
		{
			foreach (var businessService in new List<Node> (orderedBusinessServices))
			{
				RemoveBusinessService(businessService);
			}

			businessServicesNode.ChildAdded -= businessServicesNode_ChildAdded;
			businessServicesNode.ChildRemoved -= businessServicesNode_ChildRemoved;
		}

		void businessServicesNode_ChildAdded (Node parent, Node child)
		{
			AddBusinessService(child);
		}

		void businessServicesNode_ChildRemoved (Node parent, Node child)
		{
			RemoveBusinessService(child);
		}

		Control AddBusinessService (Node businessService)
		{
			var lozenge = lozengeCreator(businessService);

			orderedBusinessServices.Add(businessService);
			businessServiceToLozenge.Add(businessService, lozenge);

			DoLayout();

			return lozenge;
		}

		void RemoveBusinessService (Node businessService)
		{
			businessServiceToLozenge[businessService].Dispose();
			businessServiceToLozenge.Remove(businessService);
			orderedBusinessServices.Remove(businessService);
		}

	    Rectangle DoLayout ()
	    {
	        var lozengeHeights = new List<int> { 60, 50, 40 };

	        const int rows = 7;

	        int? lozengeHeight = null;
	        float yGap = 0;

	        foreach (var height in lozengeHeights)
	        {
	            yGap = (bounds.Height - rows * height) / (float) (rows - 1);

                // Ensure at least a 1px gap
	            if (yGap >= 1)
	            {
	                lozengeHeight = height;
	                break;
	            }
	        }

	        if (lozengeHeight == null)
	        {
                return Rectangle.Empty;
	        }

	        var lozengeWidth = (int) (lozengeHeight.Value * 2.625);

	        const int columns = 4;
	        const int xGap = 15;

	        if (bounds.Width < columns * lozengeWidth)
	        {
	            return Rectangle.Empty;
	        }

	        var lozengeLocations = new List<Point> ();
	        const int padding = 0;

		    var leftColumnsToPositions = new List<List<Point>> ();
		    var rightColumnsToPositions = new List<List<Point>> ();

			for (var column = 0; column < (columns / 2); column++)
	        {
				leftColumnsToPositions.Add(new List<Point> ());
				rightColumnsToPositions.Add(new List<Point> ());

	            for (var row = 0; row < rows; row++)
	            {
		            var y = bounds.Top + ((lozengeHeight.Value + yGap) * row);

		            var leftX = bounds.Left + padding + (column * (lozengeWidth + xGap));
		            var leftLocation = new Point (leftX, (int) y);
					lozengeLocations.Add(leftLocation);
					leftColumnsToPositions[column].Add(leftLocation);

	                var rightX = bounds.Right - padding - ((column + 1) * lozengeWidth) - (column * xGap);
		            var rightLocation = new Point (rightX, (int) y);
					lozengeLocations.Add(rightLocation);
					rightColumnsToPositions[column].Add(rightLocation);
	            }
	        }

			Debug.Assert(orderedBusinessServices.Count <= lozengeLocations.Count, "More business services than there are locations");

		    var lozenges = orderedBusinessServices.Select(s => businessServiceToLozenge[s]).ToList();
		    foreach (var lozenge in lozenges)
		    {
			    if (lozenge.Parent != control)
			    {
				    lozenge.Parent?.Controls.Remove(lozenge);
				    control?.Controls.Add(lozenge);
				    lozenge.BringToFront();
			    }
		    }
		    lozengeArranger(lozenges, leftColumnsToPositions, rightColumnsToPositions, lozengeLocations, new Size (lozengeWidth, lozengeHeight.Value));

	        var innerBoundsLeft = lozengeLocations.Select(l => l.X + lozengeWidth).Where(p => p < bounds.Width / 2).Max();
	        var innerBoundsRight = lozengeLocations.Where(l => l.X > bounds.Width / 2).Select(l => l.X).Min();
            
            return new Rectangle(innerBoundsLeft, bounds.Top, innerBoundsRight - innerBoundsLeft, bounds.Height);
	    }

		void DefaultBusinessServiceArranger (IList<Control> businessServiceLozenges, List<List<Point>> leftColumns, List<List<Point>> rightColumns, List<Point> allLocations, Size lozengeSize)
		{
			int i = 0;
			foreach (var lozenge in businessServiceLozenges)
			{
				lozenge.Bounds = new Rectangle (allLocations[i], lozengeSize);
				i++;
			}
		}

		public Rectangle DoLayout (Control control, Rectangle bounds)
		{
			this.control = control;
			this.bounds = bounds;

			return DoLayout();
		}
	}
}
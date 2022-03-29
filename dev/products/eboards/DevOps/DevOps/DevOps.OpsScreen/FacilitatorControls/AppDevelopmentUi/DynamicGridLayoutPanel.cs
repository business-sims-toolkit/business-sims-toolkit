using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using CommonGUI;

namespace DevOps.OpsScreen.FacilitatorControls.AppDevelopmentUi
{
    public class DynamicGridLayoutPanel : FlickerFreePanel
    {
        // MinimumColumns ?? 1
        public int? MinimumColumns { get; set; }

        // MinimumRows ?? 1
        public int? MinimumRows { get; set; }

        public int? MaximumColumns { get; set; }
        

        public int? ColumnCount { get; set; }
        public int? RowCount { get; set; }

        public int HorizontalInnerPadding { get; set; }
        public int VerticalInnerPadding { get; set; }

        public int HorizontalOuterMargin { get; set; }
        public int VerticalOuterMargin { get; set; }

        // TODO restrict 
        public bool IsSpacingFixedSize { get; set; }
        public bool AreItemsFixedSize { get; set; }

        public int MinimumItemWidth { get; set; }
        public int MinimumItemHeight { get; set; }

        public int? MaximumItemWidth { get; set; }
        public int? MaximumItemHeight { get; set; }


        public int PreferredGridHeight { get; private set; }
        public int PreferredGridWidth { get; private set; }

        // TODO Assume the items passed in are of the same size?

        public DynamicGridLayoutPanel(IEnumerable<Control> gridItems, int minimumItemWidth, int minimumItemHeight)
        {
            this.gridItems = gridItems.ToList();

            foreach (var item in this.gridItems)
            {
                Controls.Add(item);
            }

            MinimumItemWidth = minimumItemWidth;
            MinimumItemHeight = minimumItemHeight;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            DoSize();
        }

        void DoSize()
        {
            var width = Width - 2 * HorizontalOuterMargin;
            var height = Height - 2 * VerticalOuterMargin;

            // 2 .. MaximumColumns ?? 8
            var divisors = Enumerable.Range(2, MaximumColumns - 1 ?? 7).Reverse().ToList();

            var itemWidth = Math.Max(gridItems.Max(i => i.Width), MinimumItemWidth);
            var itemHeight = Math.Max(gridItems.Max(i => i.Height), MinimumItemHeight);

            var horizontalSpacing = HorizontalInnerPadding;
            var verticalSpacing = VerticalInnerPadding;

            var horizontalMargin = HorizontalOuterMargin;
            var verticalMargin = VerticalOuterMargin;

            int numColumns;
            
            if (gridItems.Count == 1)
            {
                numColumns = 1;

                if (!AreItemsFixedSize)
                {
                    itemWidth = CalculateItemDimension(width, horizontalSpacing, numColumns, MinimumItemWidth, MaximumItemWidth);
                }
            }
            else
            {
                numColumns = ColumnCount ?? FindNumColumns(divisors, gridItems.Count, MinimumColumns ?? 1);

                var widthNeeded = numColumns * itemWidth + (numColumns - 1) * horizontalSpacing;

                if (widthNeeded > width)
                {
                    if (IsSpacingFixedSize && AreItemsFixedSize)
                    {
                        var divisorIndex = divisors.IndexOf(numColumns);

                        if (divisorIndex != -1 && divisorIndex < divisors.Count - 2)
                        {
                            divisorIndex++;

                            numColumns = FindNumColumns(divisors.Skip(divisorIndex), gridItems.Count, MinimumColumns ?? 1);
                        }
                    }
                    else if (!AreItemsFixedSize)
                    {
                        itemWidth = CalculateItemDimension(width, horizontalSpacing, numColumns, MinimumItemWidth, MaximumItemWidth);
                    }
                }

                if (widthNeeded < width)
                {
                    if (!AreItemsFixedSize)
                    {
                        itemWidth = CalculateItemDimension(width, horizontalSpacing, numColumns, MinimumItemWidth, MaximumItemWidth);

                        widthNeeded = numColumns * itemWidth + (numColumns - 1) * horizontalSpacing;

                        if (widthNeeded < width)
                        {
                            var widthRemaining = width - numColumns * itemWidth;
                            if (! IsSpacingFixedSize)
                            {
                                horizontalSpacing = widthRemaining / (numColumns - 1);
                            }
                        }
                    }

                }
            }

            PreferredGridWidth = 2 * horizontalMargin + numColumns * itemWidth +
                                 (numColumns - 1) * horizontalSpacing;
            
            var numRows = gridItems.Count / numColumns + (gridItems.Count % numColumns != 0 ? 1 : 0);

            var heightNeeded = numRows * itemHeight + (numRows - 1) * verticalSpacing;

            if (heightNeeded > height)
            {
                if (! AreItemsFixedSize)
                {
                    itemHeight = CalculateItemDimension(height, verticalSpacing, numRows, MinimumItemHeight,
                        MaximumItemHeight);
                }
            }
            else if (heightNeeded < height)
            {
                if (!AreItemsFixedSize)
                {
                    itemHeight = CalculateItemDimension(height, verticalSpacing, numRows, MinimumItemHeight,
                        MaximumItemHeight);

                    heightNeeded = numRows * itemHeight + (numRows - 1) * verticalSpacing;

                    if (heightNeeded < height)
                    {
                        var heightRemaining = height - numRows * itemHeight;

                        if (! IsSpacingFixedSize)
                        {
                            verticalSpacing = heightRemaining / Math.Max(numRows - 1, 1);
                        }
                    }
                }

            }

            PreferredGridHeight = numRows * itemHeight + (numRows - 1) * verticalSpacing + 2 * verticalMargin;

            for (var i = 0; i < gridItems.Count; i++)
            {
                var item = gridItems[i];

                var x = (i % numColumns) * (itemWidth + horizontalSpacing) + horizontalMargin;
                var y = (i / numColumns) * (itemHeight + verticalSpacing) + verticalMargin;

                item.Bounds = new Rectangle(x, y, itemWidth, itemHeight);
            }
        }

        static int CalculateItemDimension(int overallSize, int spacing, int count, int minimumSize, int? maximumSize)
        {
            var remaining = overallSize - (count - 1) * spacing;

            var size = Math.Max(minimumSize, remaining / count);

            if (maximumSize != null)
            {
                size = Math.Min(size, maximumSize.Value);
            }

            return size;
        }

        static int FindNumColumns(IEnumerable<int> divisors, int itemsCount, int minimumColumnCount)
        {
            var enumerable = divisors.ToList();
            var numColumns = enumerable.Where(d => itemsCount % d == 0).Max(d => d as int?);

            if (numColumns == null)
            {
                var max = 0;
                foreach (var divisor in enumerable)
                {
                    if (itemsCount % divisor < max)
                    {
                        continue;
                    }

                    max = itemsCount % divisor;
                    numColumns = divisor;
                }

                if (numColumns == null)
                {
                    numColumns = minimumColumnCount;
                }
            }

            return numColumns.Value;
        }

        readonly List<Control> gridItems;
    }
}

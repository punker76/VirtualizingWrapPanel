﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WpfToolkit.Controls
{
    /// <summary>
    /// A implementation of a wrap panel that supports virtualization and can be used in horizontal and vertical orientation.
    /// </summary>
    public class VirtualizingWrapPanel : VirtualizingPanelBase
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure, (obj, args) => ((VirtualizingWrapPanel)obj).Orientation_Changed()));

        public static readonly DependencyProperty ItemSizeProperty = DependencyProperty.Register(nameof(ItemSize), typeof(Size), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(Size.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure, (obj, args) => ((VirtualizingWrapPanel)obj).ItemSize_Changed()));

        public static readonly DependencyProperty AllowDifferentSizedItemsProperty = DependencyProperty.Register(nameof(AllowDifferentSizedItems), typeof(bool), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure, (obj, args) => ((VirtualizingWrapPanel)obj).AllowDifferentSizedItems_Changed()));

        public static readonly DependencyProperty ItemSizeProviderProperty = DependencyProperty.Register(nameof(ItemSizeProvider), typeof(IItemSizeProvider), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty ItemAlignmentProperty = DependencyProperty.Register(nameof(ItemAlignment), typeof(ItemAlignment), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(ItemAlignment.Start, FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty SpacingModeProperty = DependencyProperty.Register(nameof(SpacingMode), typeof(SpacingMode), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(SpacingMode.Uniform, FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty StretchItemsProperty = DependencyProperty.Register(nameof(StretchItems), typeof(bool), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange));

        public static readonly DependencyProperty IsGridLayoutEnabledProperty = DependencyProperty.Register(nameof(IsGridLayoutEnabled), typeof(bool), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Gets or sets a value that specifies the orientation in which items are arranged before wrapping. The default value is <see cref="Orientation.Horizontal"/>.
        /// </summary>
        public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

        /// <summary>
        /// Gets or sets a value that specifies the size of the items. The default value is <see cref="Size.Empty"/>. 
        /// If the value is <see cref="Size.Empty"/> the item size is determined by measuring the first realized item.
        /// </summary>
        public Size ItemSize { get => (Size)GetValue(ItemSizeProperty); set => SetValue(ItemSizeProperty, value); }

        /// <summary>
        /// Specifies whether items can have different sizes. The default value is false. If this property is enabled, 
        /// it is strongly recommended to also set the <see cref="ItemSizeProvider"/> property. Otherwise, the position 
        /// of the items is not always guaranteed to be correct.
        /// </summary>
        public bool AllowDifferentSizedItems { get => (bool)GetValue(AllowDifferentSizedItemsProperty); set => SetValue(AllowDifferentSizedItemsProperty, value); }

        /// <summary>
        /// Specifies an instance of <see cref="IItemSizeProvider"/> which provides the size of the items. In order to allow
        /// different sized items, also enable the <see cref="AllowDifferentSizedItems"/> property.
        /// </summary>
        public IItemSizeProvider? ItemSizeProvider { get => (IItemSizeProvider?)GetValue(ItemSizeProviderProperty); set => SetValue(ItemSizeProviderProperty, value); }

        /// <summary>
        /// Specifies how the item are aligned on the cross axis. The default value is <see cref="ItemAlignment.Start"/>.
        /// This property only applies when the <see cref="AllowDifferentSizedItems"/> property is enabled.
        /// </summary>
        public ItemAlignment ItemAlignment { get => (ItemAlignment)GetValue(ItemAlignmentProperty); set => SetValue(ItemAlignmentProperty, value); }

        /// <summary>
        /// Gets or sets the spacing mode used when arranging the items. The default value is <see cref="SpacingMode.Uniform"/>.
        /// </summary>
        public SpacingMode SpacingMode { get => (SpacingMode)GetValue(SpacingModeProperty); set => SetValue(SpacingModeProperty, value); }

        /// <summary>
        /// Gets or sets a value that specifies if the items get stretched to fill up remaining space. The default value is false.
        /// </summary>
        /// <remarks>
        /// The MaxWidth and MaxHeight properties of the ItemContainerStyle can be used to limit the stretching. 
        /// In this case the use of the remaining space will be determined by the SpacingMode property. 
        /// </remarks>
        public bool StretchItems { get => (bool)GetValue(StretchItemsProperty); set => SetValue(StretchItemsProperty, value); }

        /// <summary>
        /// Specifies whether the items are arranged in a grid-like layout. The default value is <c>true</c>.
        /// When set to <c>true</c>, the items are arranged based on the number of items that can fit in a line. 
        /// When set to <c>false</c>, the items are arranged based on the number of items that are actually placed in the line. 
        /// </summary>
        /// <remarks>
        /// If <see cref="AllowDifferentSizedItems"/> is enabled, this property has no effect and the items are always 
        /// arranged based on the number of items that are actually placed in the line.
        /// </remarks>
        public bool IsGridLayoutEnabled { get => (bool)GetValue(IsGridLayoutEnabledProperty); set => SetValue(IsGridLayoutEnabledProperty, value); }

        /// <summary>
        /// Gets value that indicates whether the <see cref="VirtualizingPanel"/> can virtualize items 
        /// that are grouped or organized in a hierarchy.
        /// </summary>
        /// <returns>always true for <see cref="VirtualizingWrapPanel"/></returns>
        protected override bool CanHierarchicallyScrollAndVirtualizeCore => true;

        protected override bool HasLogicalOrientation => false;

        protected override Orientation LogicalOrientation => Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;

        private static readonly Size InfiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

        private static readonly Size FallbackItemSize = new Size(48, 48);

        private ItemContainerManager ItemContainerManager
        {
            get
            {
                if (_itemContainerManager is null)
                {
                    _itemContainerManager = new ItemContainerManager(
                        ItemContainerGenerator,
                        AddInternalChild,
                        child => RemoveInternalChildRange(InternalChildren.IndexOf(child), 1));
                    _itemContainerManager.ItemsChanged += ItemContainerManager_ItemsChanged;
                }
                return _itemContainerManager;
            }
        }
        private ItemContainerManager? _itemContainerManager;

        /// <summary>
        /// The cache length before and after the viewport. 
        /// </summary>
        private VirtualizationCacheLength cacheLength;

        /// <summary>
        /// The Unit of the cache length. Can be Pixel, Item or Page. 
        /// When the ItemsOwner is a group item it can only be pixel or item.
        /// </summary>
        private VirtualizationCacheLengthUnit cacheLengthUnit;

        private Size sizeOfFirstItem = Size.Empty;

        private readonly Dictionary<object, Size> itemSizesCache = new Dictionary<object, Size>(ReferenceEqualityComparer.Instance);
        private Size averageItemSizeCache = Size.Empty;

        private int startItemIndex = -1;
        private int endItemIndex = -1;

        private double startItemOffsetX = 0;
        private double startItemOffsetY = 0;

        private double knownExtendX = 0;
        private double knownExtendY = 0;

        private int bringIntoViewItemIndex = -1;
        private FrameworkElement? bringIntoViewContainer;

        #region Variables for caching frequently read dependency properties.
        private ReadOnlyCollection<object> items = new([]);
        private Orientation orientation = Orientation.Horizontal;
        private Size itemSize = Size.Empty;
        private bool allowDifferentSizedItems = false;
        private IItemSizeProvider? itemSizeProvider;
        #endregion

        public void ClearItemSizeCache()
        {
            itemSizesCache.Clear();
            averageItemSizeCache = Size.Empty;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            CacheDependencyProperties();

            VerifyItemsControl();

            if (ShouldIgnoreMeasure())
            {
                return DesiredSize;
            }

            ItemContainerManager.IsRecycling = IsRecycling;

            MeasureBringIntoViewContainer(InfiniteSize);

            Size newViewportSize;

            if (ItemsOwner is IHierarchicalVirtualizationAndScrollInfo groupItem)
            {
                Rect viewport = GetViewportFromGroupItem(groupItem);
                ScrollOffset = viewport.Location;
                newViewportSize = viewport.Size;
                cacheLength = groupItem.Constraints.CacheLength;
                cacheLengthUnit = groupItem.Constraints.CacheLengthUnit;
            }
            else
            {
                newViewportSize = availableSize;
                cacheLength = GetCacheLength(ItemsOwner);
                cacheLengthUnit = GetCacheLengthUnit(ItemsOwner);
            }

            averageItemSizeCache = Size.Empty;

            UpdateViewportSize(newViewportSize);
            RealizeAndVirtualizeItems();
            UpdateExtent();

            const double Tolerance = 0.001;

            if (ItemsOwner is not IHierarchicalVirtualizationAndScrollInfo
                && GetY(ScrollOffset) != 0
                && GetY(ScrollOffset) + GetHeight(ViewportSize) > GetHeight(Extent) + Tolerance)
            {
                ScrollOffset = CreatePoint(GetX(ScrollOffset), Math.Max(0, GetHeight(Extent) - GetHeight(ViewportSize)));
                ScrollOwner?.InvalidateScrollInfo();
                return MeasureOverride(availableSize); // repeat measure with correct ScrollOffset
            }

            return CalculateDesiredSize(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            ViewportSize = finalSize;

            ArrangeBringIntoViewContainer();

            foreach (var cachedContainer in ItemContainerManager.CachedContainers)
            {
                cachedContainer.Arrange(new Rect(0, 0, 0, 0));
            }

            if (startItemIndex == -1)
            {
                return finalSize;
            }

            if (ItemContainerManager.RealizedContainers.Count < endItemIndex - startItemIndex + 1)
            {
                throw new InvalidOperationException("The items must be distinct and must not change their hash code.");
            }

            bool hierarchical = ItemsOwner is IHierarchicalVirtualizationAndScrollInfo;
            double x = startItemOffsetX + GetX(ScrollOffset);
            double y = hierarchical ? startItemOffsetY : startItemOffsetY - GetY(ScrollOffset);
            double rowHeight = 0;
            var rowChilds = new List<UIElement>();
            var childSizes = new List<Size>();

            for (int i = startItemIndex; i <= endItemIndex; i++)
            {
                var item = Items[i];
                var child = ItemContainerManager.RealizedContainers[item];

                Size upfrontKnownItemSize = GetUpfrontKnownItemSizeOrEmpty(item);

                Size childSize = !upfrontKnownItemSize.IsEmpty ? upfrontKnownItemSize : child.DesiredSize;

                if (rowChilds.Count > 0 && x + GetWidth(childSize) > GetWidth(finalSize))
                {
                    ArrangeLine(GetWidth(finalSize), rowChilds, childSizes, y, hierarchical);
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                    rowChilds.Clear();
                    childSizes.Clear();
                }

                x += GetWidth(childSize);
                rowHeight = Math.Max(rowHeight, GetHeight(childSize));
                rowChilds.Add(child);
                childSizes.Add(childSize);
            }

            if (rowChilds.Any())
            {
                ArrangeLine(GetWidth(finalSize), rowChilds, childSizes, y, hierarchical);
            }

            return finalSize;
        }

        protected override void BringIndexIntoView(int index)
        {
            if (index < 0 || index >= Items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"The argument {nameof(index)} must be >= 0 and < the count of items.");
            }

            var container = (FrameworkElement)ItemContainerManager.Realize(index);

            bringIntoViewItemIndex = index;
            bringIntoViewContainer = container;

            // make sure the container is measured and arranged before calling BringIntoView        
            InvalidateMeasure();
            UpdateLayout();

            container.BringIntoView();
        }

        protected override void OnClearChildren()
        {
            if (InternalChildren.Count == 0)
            {
                ItemContainerManager.OnClearChildren();
            }
            base.OnClearChildren();
        }

        private void ItemContainerManager_ItemsChanged(object? sender, ItemContainerManagerItemsChangedEventArgs e)
        {
            if (bringIntoViewItemIndex >= Items.Count)
            {
                bringIntoViewItemIndex = -1;
                bringIntoViewContainer = null;
            }

            if (e.Action == NotifyCollectionChangedAction.Remove
                || e.Action == NotifyCollectionChangedAction.Replace)
            {
                if (AllowDifferentSizedItems)
                {
                    foreach (var key in itemSizesCache.Keys.Except(Items).ToList())
                    {
                        itemSizesCache.Remove(key);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                itemSizesCache.Clear();

                if (AllowDifferentSizedItems && ItemSizeProvider is null)
                {
                    ScrollOffset = new Point(0, 0);
                }
            }
        }

        private void Orientation_Changed()
        {
            MouseWheelScrollDirection = Orientation == Orientation.Horizontal
                                        ? ScrollDirection.Vertical
                                        : ScrollDirection.Horizontal;
            SetVerticalOffset(0);
            SetHorizontalOffset(0);
        }

        private void AllowDifferentSizedItems_Changed()
        {
            if (!AllowDifferentSizedItems)
            {
                itemSizesCache.Clear();
            }

            foreach (var child in InternalChildren.Cast<UIElement>())
            {
                child.InvalidateMeasure();
            }
        }

        private void ItemSize_Changed()
        {
            foreach (var child in InternalChildren.Cast<UIElement>())
            {
                child.InvalidateMeasure();
            }
        }

        private Rect GetViewportFromGroupItem(IHierarchicalVirtualizationAndScrollInfo groupItem)
        {
            double viewportX = groupItem.Constraints.Viewport.Location.X;
            double viewportY = groupItem.Constraints.Viewport.Location.Y;
            double viewportWidth = Math.Max(groupItem.Constraints.Viewport.Size.Width, 0);
            double viewportHeight = Math.Max(groupItem.Constraints.Viewport.Size.Height, 0);

            if (VisualTreeHelper.GetParent(this) is ItemsPresenter itemsPresenter)
            {
                var margin = itemsPresenter.Margin;

                if (orientation == Orientation.Horizontal)
                {
                    viewportWidth = Math.Max(0, viewportWidth - (margin.Left + margin.Right));
                }
                else
                {
                    viewportHeight = Math.Max(0, viewportHeight - (margin.Top + margin.Bottom));
                }
            }

            if (orientation == Orientation.Horizontal)
            {
                viewportY = Math.Max(0, viewportY - groupItem.HeaderDesiredSizes.PixelSize.Height);
                double visibleHeaderHeight = Math.Max(0, groupItem.HeaderDesiredSizes.PixelSize.Height - Math.Max(0, groupItem.Constraints.Viewport.Location.Y));
                viewportHeight = Math.Max(0, viewportHeight - visibleHeaderHeight);
            }
            else
            {
                viewportHeight = Math.Max(0, viewportHeight - groupItem.HeaderDesiredSizes.PixelSize.Height);
            }

            return new Rect(viewportX, viewportY, viewportWidth, viewportHeight);
        }

        private void MeasureBringIntoViewContainer(Size availableSize)
        {
            if (bringIntoViewContainer is not null && !bringIntoViewContainer.IsMeasureValid)
            {
                var upfrontKnownItemSize = GetUpfrontKnownItemSizeOrEmpty(Items[bringIntoViewItemIndex]);
                bringIntoViewContainer.Measure(!upfrontKnownItemSize.IsEmpty ? upfrontKnownItemSize : availableSize);

                if (!allowDifferentSizedItems && sizeOfFirstItem.IsEmpty)
                {
                    sizeOfFirstItem = bringIntoViewContainer.DesiredSize;
                }
            }
        }

        private void ArrangeBringIntoViewContainer()
        {
            if (bringIntoViewContainer is not null)
            {
                bool hierarchical = ItemsOwner is IHierarchicalVirtualizationAndScrollInfo;
                var offset = FindItemOffset(bringIntoViewItemIndex);
                offset = new Point(offset.X - ScrollOffset.X, hierarchical ? offset.Y : offset.Y - ScrollOffset.Y);
                var upfrontKnownItemSize = GetUpfrontKnownItemSizeOrEmpty(Items[bringIntoViewItemIndex]);
                var size = !upfrontKnownItemSize.IsEmpty ? upfrontKnownItemSize : bringIntoViewContainer.DesiredSize;
                bringIntoViewContainer.Arrange(new Rect(offset, size));
            }
        }

        private void RealizeAndVirtualizeItems()
        {
            FindStartIndexAndOffset();
            FindEndIndexIfPossible();
            VirtualizeItems();
            RealizeItemsAndFindEndIndex();
            VirtualizeItems();
        }

        private Size GetAverageItemSize()
        {
            if (!itemSize.IsEmpty)
            {
                return itemSize;
            }
            else if (!allowDifferentSizedItems)
            {
                return !sizeOfFirstItem.IsEmpty ? sizeOfFirstItem : FallbackItemSize;
            }
            else
            {
                if (averageItemSizeCache.IsEmpty)
                {
                    averageItemSizeCache = CalculateAverageItemSize();
                }
                return averageItemSizeCache;
            }
        }

        private Point FindItemOffset(int itemIndex)
        {
            if (!allowDifferentSizedItems && (!itemSize.IsEmpty || !sizeOfFirstItem.IsEmpty))
            {
                Size uniformItemSize = !itemSize.IsEmpty ? itemSize : sizeOfFirstItem;
                var itemsPerRow = CalculateItemsPerRow(uniformItemSize);
                int rowIndex = itemIndex / itemsPerRow;
                int itemIndexInRow = itemIndex % itemsPerRow;
                double x = itemIndexInRow * GetWidth(uniformItemSize);
                double y = rowIndex * GetHeight(uniformItemSize);
                return CreatePoint(x, y);
            }
            else
            {
                double x = 0, y = 0, rowHeight = 0;

                for (int i = 0; i <= itemIndex; i++)
                {
                    Size itemSize = GetItemSizeOrAverage(Items[i]);

                    if (x != 0 && x + GetWidth(itemSize) > GetWidth(ViewportSize))
                    {
                        x = 0;
                        y += rowHeight;
                        rowHeight = 0;
                    }

                    if (i != itemIndex)
                    {
                        x += GetWidth(itemSize);
                        rowHeight = Math.Max(rowHeight, GetHeight(itemSize));
                    }
                }

                return CreatePoint(x, y);
            }
        }

        private void UpdateViewportSize(Size newViewportSize)
        {
            // Retain the current viewport size if the new viewport size
            // received from the parent virtualizing panel is zero. This 
            // is necessary for the BringIndexIntoView function to work.
            if (ItemsOwner is IHierarchicalVirtualizationAndScrollInfo
                && newViewportSize.Width == 0
                && newViewportSize.Height == 0)
            {
                return;
            }

            if (newViewportSize != ViewportSize)
            {
                ViewportSize = newViewportSize;
                ScrollOwner?.InvalidateScrollInfo();
            }
        }

        private void FindStartIndexAndOffset()
        {
            if (ViewportSize.Width == 0 && ViewportSize.Height == 0)
            {
                startItemIndex = -1;
                startItemOffsetX = 0;
                startItemOffsetY = 0;
                return;
            }

            double startOffsetY = DetermineStartOffsetY();

            if (startOffsetY <= 0)
            {
                startItemIndex = Items.Count > 0 ? 0 : -1;
                startItemOffsetX = 0;
                startItemOffsetY = 0;
                return;
            }

            if (!allowDifferentSizedItems && (!itemSize.IsEmpty || !sizeOfFirstItem.IsEmpty))
            {
                Size uniformItemSize = !itemSize.IsEmpty ? itemSize : sizeOfFirstItem;
                int itemsPerRow = CalculateItemsPerRow(uniformItemSize);
                int offsetInRows = (int)Math.Floor(startOffsetY / GetHeight(uniformItemSize));
                startItemIndex = Math.Min(offsetInRows * itemsPerRow, Items.Count - 1);

                if (cacheLengthUnit == VirtualizationCacheLengthUnit.Item)
                {
                    startItemIndex = Math.Max(startItemIndex - (int)cacheLength.CacheBeforeViewport, 0);
                    var itemOffset = FindItemOffset(startItemIndex);
                    startItemOffsetX = GetX(itemOffset);
                    startItemOffsetY = GetY(itemOffset);
                }
                else
                {
                    startItemOffsetX = 0;
                    startItemOffsetY = offsetInRows * GetHeight(uniformItemSize);
                }
            }
            else
            {
                startItemIndex = -1;

                double x = 0, y = 0, rowHeight = 0;
                int indexOfFirstRowItem = 0;

                for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
                {
                    var item = items[itemIndex];

                    Size itemSize = GetItemSizeOrAverage(item);

                    if (x + GetWidth(itemSize) > GetWidth(ViewportSize) && x != 0)
                    {
                        x = 0;
                        y += rowHeight;
                        rowHeight = 0;
                        indexOfFirstRowItem = itemIndex;
                    }
                    x += GetWidth(itemSize);
                    rowHeight = Math.Max(rowHeight, GetHeight(itemSize));

                    if (y + rowHeight > startOffsetY)
                    {
                        if (cacheLengthUnit == VirtualizationCacheLengthUnit.Item)
                        {
                            startItemIndex = Math.Max(indexOfFirstRowItem - (int)cacheLength.CacheBeforeViewport, 0);
                            var itemOffset = FindItemOffset(startItemIndex);
                            startItemOffsetX = GetX(itemOffset);
                            startItemOffsetY = GetY(itemOffset);
                        }
                        else
                        {
                            startItemIndex = indexOfFirstRowItem;
                            startItemOffsetX = 0;
                            startItemOffsetY = y;
                        }
                        break;
                    }
                }

                // make sure that at least one item is realized to allow correct calculation of the extend
                if (startItemIndex == -1 && Items.Count > 0)
                {
                    startItemIndex = Items.Count - 1;
                    startItemOffsetX = x;
                    startItemOffsetY = y;
                }
            }
        }

        /// <summary>
        /// If possible, find the end index to enable containers to be virtualized and reused when scrolling upwards.
        /// </summary>
        private void FindEndIndexIfPossible()
        {
            if (!allowDifferentSizedItems && (!itemSize.IsEmpty || !sizeOfFirstItem.IsEmpty))
            {
                Size uniformItemSize = !itemSize.IsEmpty ? itemSize : sizeOfFirstItem;
                double endOffsetY = DetermineEndOffsetY();
                int itemsPerRow = CalculateItemsPerRow(uniformItemSize);
                int rows = (int)Math.Ceiling(endOffsetY / GetHeight(uniformItemSize));

                endItemIndex = Math.Min((itemsPerRow * rows) - 1, Items.Count - 1);

                if (cacheLengthUnit == VirtualizationCacheLengthUnit.Item)
                {
                    endItemIndex = Math.Min(endItemIndex + (int)cacheLength.CacheAfterViewport, Items.Count - 1);
                }
            }
        }

        private void RealizeItemsAndFindEndIndex()
        {
            if (startItemIndex == -1)
            {
                endItemIndex = -1;
                knownExtendX = 0;
                return;
            }

            int newEndItemIndex = Items.Count - 1;
            bool endItemIndexFound = false;

            double endOffsetY = DetermineEndOffsetY();

            double x = startItemOffsetX;
            double y = startItemOffsetY;
            double rowHeight = 0;

            knownExtendX = 0;

            for (int itemIndex = startItemIndex; itemIndex <= newEndItemIndex; itemIndex++)
            {
                if (itemIndex == 0)
                {
                    sizeOfFirstItem = Size.Empty;
                }

                object item = Items[itemIndex];

                var container = ItemContainerManager.Realize(itemIndex);

                if (container == bringIntoViewContainer)
                {
                    bringIntoViewItemIndex = -1;
                    bringIntoViewContainer = null;
                }

                Size upfrontKnownItemSize = GetUpfrontKnownItemSizeOrEmpty(item);

                container.Measure(!upfrontKnownItemSize.IsEmpty ? upfrontKnownItemSize : InfiniteSize);

                var containerSize = DetermineContainerSize(item, container, upfrontKnownItemSize);

                if (allowDifferentSizedItems == false && sizeOfFirstItem.IsEmpty)
                {
                    sizeOfFirstItem = containerSize;
                }

                if (x != 0 && x + GetWidth(containerSize) > GetWidth(ViewportSize))
                {
                    x = 0;
                    y += rowHeight;
                    rowHeight = 0;
                }

                x += GetWidth(containerSize);
                knownExtendX = Math.Max(x, knownExtendX);
                rowHeight = Math.Max(rowHeight, GetHeight(containerSize));

                if (endItemIndexFound == false)
                {
                    if (y >= endOffsetY
                        || (allowDifferentSizedItems == false
                            && x + GetWidth(sizeOfFirstItem) > GetWidth(ViewportSize)
                            && y + rowHeight >= endOffsetY))
                    {
                        endItemIndexFound = true;

                        newEndItemIndex = itemIndex;

                        if (cacheLengthUnit == VirtualizationCacheLengthUnit.Item)
                        {
                            newEndItemIndex = Math.Min(newEndItemIndex + (int)cacheLength.CacheAfterViewport, Items.Count - 1);
                            // loop continues until newEndItemIndex is reached
                        }
                    }
                }

                knownExtendY = y + rowHeight;
            }

            endItemIndex = newEndItemIndex;
        }

        private Size DetermineContainerSize(object item, UIElement container, Size upfrontKnownItemSize)
        {
            Size containerSize = !upfrontKnownItemSize.IsEmpty ? upfrontKnownItemSize : container.DesiredSize;

            if (allowDifferentSizedItems)
            {
                itemSizesCache[item] = containerSize;
            }

            return containerSize;
        }

        private void VirtualizeItems()
        {
            var itemsToBeRealized = Utils.HashSetOfRange(Items, startItemIndex, endItemIndex);

            foreach (var (item, container) in ItemContainerManager.RealizedContainers.ToList())
            {
                if (container == bringIntoViewContainer)
                {
                    continue;
                }

                if (!itemsToBeRealized.Contains(item))
                {
                    ItemContainerManager.Virtualize(item);
                }
            }
        }

        private void UpdateExtent()
        {
            Size extent;

            if (startItemIndex == -1)
            {
                extent = new Size(0, 0);
            }
            else if (!allowDifferentSizedItems)
            {
                extent = CalculateExtentForUniformSizedItems();
            }
            else
            {
                extent = CalculateExtentForDifferentSizedItems();
            }

            if (extent != Extent)
            {
                Extent = extent;
                ScrollOwner?.InvalidateScrollInfo();
            }
        }

        private Size CalculateExtentForUniformSizedItems()
        {
            var uniformItemSize = !itemSize.IsEmpty ? itemSize : sizeOfFirstItem;
            int itemsPerRow = CalculateItemsPerRow(uniformItemSize);
            double extentY = Math.Ceiling(((double)Items.Count) / itemsPerRow) * GetHeight(uniformItemSize);
            return CreateSize(knownExtendX, extentY);
        }

        private Size CalculateExtentForDifferentSizedItems()
        {
            double itemsUntilEndCount = Items.Count - (endItemIndex + 1);
            double extendPerItem = knownExtendY / (endItemIndex + 1);
            double estimatedExtendY = knownExtendY + itemsUntilEndCount * extendPerItem;
            return CreateSize(knownExtendX, estimatedExtendY);
        }

        private Size CalculateDesiredSize(Size availableSize)
        {
            double desiredWidth = Math.Min(availableSize.Width, Extent.Width);
            double desiredHeight = Math.Min(availableSize.Height, Extent.Height);

            if (ItemsOwner is IHierarchicalVirtualizationAndScrollInfo)
            {
                if (orientation == Orientation.Horizontal)
                {
                    if (!double.IsPositiveInfinity(ViewportSize.Width))
                    {
                        desiredWidth = Math.Max(desiredWidth, ViewportSize.Width);
                    }
                }
                else
                {
                    if (!double.IsPositiveInfinity(ViewportSize.Height))
                    {
                        desiredHeight = Math.Max(desiredHeight, ViewportSize.Height);
                    }
                }
            }

            return new Size(desiredWidth, desiredHeight);
        }

        private double DetermineStartOffsetY()
        {
            double cacheLength = 0;

            if (cacheLengthUnit == VirtualizationCacheLengthUnit.Page)
            {
                cacheLength = this.cacheLength.CacheBeforeViewport * GetHeight(ViewportSize);
            }
            else if (cacheLengthUnit == VirtualizationCacheLengthUnit.Pixel)
            {
                cacheLength = this.cacheLength.CacheBeforeViewport;
            }

            return Math.Max(GetY(ScrollOffset) - cacheLength, 0);
        }

        private double DetermineEndOffsetY()
        {
            double cacheLength = 0;

            if (cacheLengthUnit == VirtualizationCacheLengthUnit.Page)
            {
                cacheLength = this.cacheLength.CacheAfterViewport * GetHeight(ViewportSize);
            }
            else if (cacheLengthUnit == VirtualizationCacheLengthUnit.Pixel)
            {
                cacheLength = this.cacheLength.CacheAfterViewport;
            }

            return Math.Max(GetY(ScrollOffset), 0) + GetHeight(ViewportSize) + cacheLength;
        }

        private Size GetUpfrontKnownItemSizeOrEmpty(object item)
        {
            if (!itemSize.IsEmpty)
            {
                return itemSize;
            }
            if (!allowDifferentSizedItems && !sizeOfFirstItem.IsEmpty)
            {
                return sizeOfFirstItem;
            }
            if (itemSizeProvider != null)
            {
                return itemSizeProvider.GetSizeForItem(item);
            }
            return Size.Empty;
        }

        private Size GetItemSizeOrAverage(object item)
        {
            if (itemSizeProvider != null)
            {
                return itemSizeProvider.GetSizeForItem(item);
            }
            if (itemSizesCache.TryGetValue(item, out Size cachedItemSize))
            {
                return cachedItemSize;
            }
            return GetAverageItemSize();
        }

        private void ArrangeLine(double rowWidth, List<UIElement> children, List<Size> childSizes, double linePositionCrossAxis, bool hierarchical)
        {
            double summedUpChildWidth;
            double extraWidth = 0;

            if (allowDifferentSizedItems)
            {
                summedUpChildWidth = childSizes.Sum(GetWidth);

                if (StretchItems)
                {
                    double unusedWidth = rowWidth - summedUpChildWidth;
                    extraWidth = unusedWidth / children.Count;
                    summedUpChildWidth = rowWidth;
                }
            }
            else
            {
                double childWidth = GetWidth(childSizes[0]);
                int itemsPerRow = IsGridLayoutEnabled ? Math.Max(1, (int)Math.Floor(rowWidth / childWidth)) : children.Count;

                if (StretchItems)
                {
                    var firstChild = (FrameworkElement)children[0];
                    double maxWidth = orientation == Orientation.Horizontal ? firstChild.MaxWidth : firstChild.MaxHeight;
                    double stretchedChildWidth = Math.Min(rowWidth / itemsPerRow, maxWidth);
                    stretchedChildWidth = Math.Max(stretchedChildWidth, childWidth); // ItemSize might be greater than MaxWidth/MaxHeight
                    extraWidth = stretchedChildWidth - childWidth;
                    summedUpChildWidth = itemsPerRow * stretchedChildWidth;
                }
                else
                {
                    summedUpChildWidth = itemsPerRow * childWidth;
                }
            }

            double innerSpacing = 0;
            double outerSpacing = 0;

            if (summedUpChildWidth < rowWidth)
            {
                CalculateRowSpacing(rowWidth, children, summedUpChildWidth, out innerSpacing, out outerSpacing);
            }

            double positionMainAxis = (hierarchical ? 0 : -GetX(ScrollOffset)) + outerSpacing;

            double lineSizeCrossAxis = Enumerable.Range(0, children.Count).Select(i => GetHeight(childSizes[i])).Max();

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                Size childSize = childSizes[i];
                double mainAxisSize = GetWidth(childSize) + extraWidth;
                double crossAxisSize = GetsArrangeSizeCrossAxis(child, childSize, lineSizeCrossAxis);
                double positionCrossAxis = GetArrangePositionCrossAxis(child, crossAxisSize, linePositionCrossAxis, lineSizeCrossAxis);
                child.Arrange(CreateOrientedRect(positionMainAxis, positionCrossAxis, mainAxisSize, crossAxisSize));
                positionMainAxis += GetWidth(childSize) + extraWidth + innerSpacing;
            }
        }

        private double GetArrangePositionCrossAxis(UIElement child, double childArrangeCrossAxisSize, double linePositionCrossAxis, double lineSizeCrossAxis)
        {
            if (ItemAlignment == ItemAlignment.End)
            {
                return linePositionCrossAxis + (lineSizeCrossAxis - childArrangeCrossAxisSize);
            }
            if (ItemAlignment == ItemAlignment.Center)
            {
                return linePositionCrossAxis + (lineSizeCrossAxis - childArrangeCrossAxisSize) / 2;
            }
            return linePositionCrossAxis;
        }

        private double GetsArrangeSizeCrossAxis(UIElement child, Size childSize, double lineSizeCrossAxis)
        {
            if (ItemAlignment == ItemAlignment.Stretch)
            {
                if (child is FrameworkElement fe)
                {
                    if (orientation == Orientation.Horizontal && double.IsNaN(fe.Height))
                    {
                        return Math.Min(lineSizeCrossAxis, fe.MaxHeight);
                    }
                    if (orientation == Orientation.Vertical && double.IsNaN(fe.Width))
                    {
                        return Math.Min(lineSizeCrossAxis, fe.MaxWidth);
                    }
                }
                return lineSizeCrossAxis;
            }
            return GetHeight(childSize);
        }

        private void CalculateRowSpacing(double rowWidth, List<UIElement> children, double summedUpChildWidth, out double innerSpacing, out double outerSpacing)
        {
            int childCount;

            if (allowDifferentSizedItems)
            {
                childCount = children.Count;
            }
            else
            {
                childCount = IsGridLayoutEnabled ? (int)Math.Max(1, Math.Floor(rowWidth / GetWidth(sizeOfFirstItem))) : children.Count;
            }

            double unusedWidth = Math.Max(0, rowWidth - summedUpChildWidth);

            switch (SpacingMode)
            {
                case SpacingMode.Uniform:
                    innerSpacing = outerSpacing = unusedWidth / (childCount + 1);
                    break;

                case SpacingMode.BetweenItemsOnly:
                    innerSpacing = unusedWidth / Math.Max(childCount - 1, 1);
                    outerSpacing = 0;
                    break;

                case SpacingMode.StartAndEndOnly:
                    innerSpacing = 0;
                    outerSpacing = unusedWidth / 2;
                    break;

                case SpacingMode.None:
                default:
                    innerSpacing = 0;
                    outerSpacing = 0;
                    break;
            }
        }

        private Size CalculateAverageItemSize()
        {
            if (itemSizesCache.Values.Count > 0)
            {
                return new Size(
                    Math.Round(itemSizesCache.Values.Average(size => size.Width)),
                    Math.Round(itemSizesCache.Values.Average(size => size.Height)));
            }
            return FallbackItemSize;
        }

        private int CalculateItemsPerRow(Size uniformItemSize)
        {
            return Math.Max(1, (int)Math.Floor(GetWidth(ViewportSize) / GetWidth(uniformItemSize)));
        }

        private void CacheDependencyProperties()
        {
            items = Items;
            orientation = Orientation;
            itemSize = ItemSize;
            allowDifferentSizedItems = AllowDifferentSizedItems;
            itemSizeProvider = ItemSizeProvider;
        }

        #region scroll info

        // TODO determine exact scroll amount for item based scrolling when AllowDifferentSizedItems is true

        protected override double GetLineUpScrollAmount()
        {
            return -Math.Min(GetAverageItemSize().Height * ScrollLineDeltaItem, ViewportSize.Height);
        }

        protected override double GetLineDownScrollAmount()
        {
            return Math.Min(GetAverageItemSize().Height * ScrollLineDeltaItem, ViewportSize.Height);
        }

        protected override double GetLineLeftScrollAmount()
        {
            return -Math.Min(GetAverageItemSize().Width * ScrollLineDeltaItem, ViewportSize.Width);
        }

        protected override double GetLineRightScrollAmount()
        {
            return Math.Min(GetAverageItemSize().Width * ScrollLineDeltaItem, ViewportSize.Width);
        }

        protected override double GetMouseWheelUpScrollAmount()
        {
            return -Math.Min(GetAverageItemSize().Height * MouseWheelDeltaItem, ViewportSize.Height);
        }

        protected override double GetMouseWheelDownScrollAmount()
        {
            return Math.Min(GetAverageItemSize().Height * MouseWheelDeltaItem, ViewportSize.Height);
        }

        protected override double GetMouseWheelLeftScrollAmount()
        {
            return -Math.Min(GetAverageItemSize().Width * MouseWheelDeltaItem, ViewportSize.Width);
        }

        protected override double GetMouseWheelRightScrollAmount()
        {
            return Math.Min(GetAverageItemSize().Width * MouseWheelDeltaItem, ViewportSize.Width);
        }

        protected override double GetPageUpScrollAmount()
        {
            return -ViewportSize.Height;
        }

        protected override double GetPageDownScrollAmount()
        {
            return ViewportSize.Height;
        }

        protected override double GetPageLeftScrollAmount()
        {
            return -ViewportSize.Width;
        }

        protected override double GetPageRightScrollAmount()
        {
            return ViewportSize.Width;
        }

        #endregion

        #region orientation aware helper methods

        private double GetX(Point point) => orientation == Orientation.Horizontal ? point.X : point.Y;
        private double GetY(Point point) => orientation == Orientation.Horizontal ? point.Y : point.X;
        private double GetWidth(Size size) => orientation == Orientation.Horizontal ? size.Width : size.Height;
        private double GetHeight(Size size) => orientation == Orientation.Horizontal ? size.Height : size.Width;
        private Point CreatePoint(double x, double y) => orientation == Orientation.Horizontal ? new Point(x, y) : new Point(y, x);
        private Size CreateSize(double width, double height) => orientation == Orientation.Horizontal ? new Size(width, height) : new Size(height, width);
        private Rect CreateOrientedRect(double x, double y, double width, double height) => orientation == Orientation.Horizontal ? new Rect(x, y, width, height) : new Rect(y, x, height, width);

        #endregion
    }
}

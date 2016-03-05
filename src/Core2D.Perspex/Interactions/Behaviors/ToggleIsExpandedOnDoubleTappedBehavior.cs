﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Perspex.Controls;
using Perspex.Interactivity;
using Perspex.Xaml.Interactivity;

namespace Core2D.Perspex.Interactions.Behaviors
{
    public class ToggleIsExpandedOnDoubleTappedBehavior : Behavior<Control>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.DoubleTapped += DoubleTapped;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.DoubleTapped -= DoubleTapped;
        }

        private void DoubleTapped(object sender, RoutedEventArgs args)
        {
            var treeViewItem = AssociatedObject.Parent as TreeViewItem;
            if (treeViewItem != null)
            {
                treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
            }
        }
    }
}

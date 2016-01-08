﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using System.Windows.Controls;

namespace Core2D.Wpf.Controls.Custom.Lists
{
    /// <summary>
    /// The <see cref="ListBox"/> control for <see cref="XGroup"/> items with drag and drop support.
    /// </summary>
    public class XGroupDragAndDropListBox : DragAndDropListBox<XGroup>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XGroupDragAndDropListBox"/> class.
        /// </summary>
        public XGroupDragAndDropListBox()
            : base()
        {
            this.Initialized += (s, e) => base.Initialize();
        }

        /// <summary>
        /// Updates DataContext collection ImmutableArray property.
        /// </summary>
        /// <param name="array">The updated immutable array.</param>
        public override void UpdateDataContext(ImmutableArray<XGroup> array)
        {
            var editor = (Core2D.Editor)this.Tag;

            var gl = editor.Project.CurrentGroupLibrary;

            var previous = gl.Items;
            var next = array;
            editor.project?.History?.Snapshot(previous, next, (p) => gl.Items = p);
            gl.Items = next;
        }
    }
}

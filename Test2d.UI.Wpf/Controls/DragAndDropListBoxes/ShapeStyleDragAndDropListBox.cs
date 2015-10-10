﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Core2D;

namespace Test.Controls
{
    /// <summary>
    /// 
    /// </summary>
    public class ShapeStyleDragAndDropListBox : DragAndDropListBox<Core2D.ShapeStyle>
    { 
        /// <summary>
        /// 
        /// </summary>
        public ShapeStyleDragAndDropListBox()
            : base()
        {
            this.Initialized += (s, e) => base.Initialize();
        }

        /// <summary>
        /// Updates DataContext binding to ImmutableArray collection property.
        /// </summary>
        /// <param name="array">The updated immutable array.</param>
        public override void UpdateDataContext(ImmutableArray<Core2D.ShapeStyle> array)
        {
            var editor = (Core2D.Editor)this.Tag;

            var sg = editor.Project.CurrentStyleLibrary;
            var previous = sg.Styles;
            var next = array;
            editor.History.Snapshot(previous, next, (p) => sg.Styles = p);
            sg.Styles = next;
        }
    }
}

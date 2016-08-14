﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using Core2D.Editor.Input;
using Core2D.Math;

namespace Core2D.Wpf.Util
{
    /// <summary>
    /// Provides mouse input from <see cref="UIElement"/>.
    /// </summary>
    public class WpfInputSource : InputSource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WpfInputSource"/> class.
        /// </summary>
        /// <param name="source">The source element.</param>
        /// <param name="relative">The relative element.</param>
        /// <param name="translate">The translate function.</param>
        public WpfInputSource(UIElement source, UIElement relative, Func<Point, Point> translate)
        {
            LeftDown = GetObservable(source, "PreviewMouseLeftButtonDown", relative, translate);
            LeftUp = GetObservable(source, "PreviewMouseLeftButtonUp", relative, translate);
            RightDown = GetObservable(source, "PreviewMouseRightButtonDown", relative, translate);
            RightUp = GetObservable(source, "PreviewMouseRightButtonUp", relative, translate);
            Move = GetObservable(source, "PreviewMouseMove", relative, translate);
        }

        private Vector2 ToVector2(Point point) => new Vector2(point.X, point.Y);

        private IObservable<Vector2> GetObservable(UIElement target, string eventName, UIElement relative, Func<Point, Point> translate)
        {
            return Observable.FromEventPattern<MouseEventArgs>(target, eventName).Select(
                e =>
                {
                    target.Focus();
                    return ToVector2(translate(e.EventArgs.GetPosition(relative)));
                });
        }
    }
}

﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Core2D.Data;
using Core2D.Data.Database;
using Core2D.Path;
using Core2D.Renderer;
using Core2D.Shape;
using Core2D.Style;

namespace Core2D.Shapes
{
    /// <summary>
    /// Path shape.
    /// </summary>
    public class XPath : BaseShape
    {
        private XPathGeometry _geometry;

        /// <summary>
        /// Gets or sets path geometry used to draw shape.
        /// </summary>
        /// <remarks>
        /// Path geometry is based on path markup syntax:
        /// - Xaml abbreviated geometry https://msdn.microsoft.com/en-us/library/ms752293(v=vs.110).aspx
        /// - Svg path specification http://www.w3.org/TR/SVG11/paths.html
        /// </remarks>
        public XPathGeometry Geometry
        {
            get { return _geometry; }
            set { Update(ref _geometry, value); }
        }

        /// <inheritdoc/>
        public override void Draw(object dc, ShapeRenderer renderer, double dx, double dy, ImmutableArray<XProperty> db, XRecord r)
        {
            var record = this.Data.Record ?? r;

            if (State.Flags.HasFlag(ShapeStateFlags.Visible))
            {
                renderer.Draw(dc, this, dx, dy, db, record);
            }

            if (renderer.State.SelectedShape != null)
            {
                if (this == renderer.State.SelectedShape)
                {
                    var points = this.GetPoints();
                    foreach (var point in points)
                    {
                        point.Draw(dc, renderer, dx, dy, db, record);
                    }
                }
                else
                {
                    var points = this.GetPoints();
                    foreach (var point in points)
                    {
                        if (point == renderer.State.SelectedShape)
                        {
                            point.Draw(dc, renderer, dx, dy, db, record);
                        }
                    }
                }
            }

            if (renderer.State.SelectedShapes != null)
            {
                if (renderer.State.SelectedShapes.Contains(this))
                {
                    var points = this.GetPoints();
                    foreach (var point in points)
                    {
                        point.Draw(dc, renderer, dx, dy, db, record);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void Move(double dx, double dy)
        {
            var points = this.GetPoints();
            foreach (var point in points)
            {
                point.Move(dx, dy);
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<XPoint> GetPoints()
        {
            return Geometry.Figures.SelectMany(f => f.GetPoints());
        }

        /// <summary>
        /// Creates a new <see cref="XPath"/> instance.
        /// </summary>
        /// <param name="name">The shape name.</param>
        /// <param name="style">The shape style.</param>
        /// <param name="geometry">The path geometry.</param>
        /// <param name="isStroked">The flag indicating whether shape is stroked.</param>
        /// <param name="isFilled">The flag indicating whether shape is filled.</param>
        /// <returns>The new instance of the <see cref="XPath"/> class.</returns>
        public static XPath Create(string name, ShapeStyle style, XPathGeometry geometry, bool isStroked = true, bool isFilled = true)
        {
            return new XPath()
            {
                Name = name,
                Style = style,
                IsStroked = isStroked,
                IsFilled = isFilled,
                Geometry = geometry
            };
        }
    }
}

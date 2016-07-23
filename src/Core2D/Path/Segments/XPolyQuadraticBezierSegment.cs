﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Collections.Immutable;
using Core2D.Shapes;

namespace Core2D.Path.Segments
{
    /// <summary>
    /// Poly quadratic bezier path segment.
    /// </summary>
    public class XPolyQuadraticBezierSegment : XPathPolySegment
    {
        /// <summary>
        /// Creates a new <see cref="XPolyQuadraticBezierSegment"/> instance.
        /// </summary>
        /// <param name="points">The points array.</param>
        /// <param name="isStroked">The flag indicating whether shape is stroked.</param>
        /// <param name="isSmoothJoin">The flag indicating whether shape is smooth join.</param>
        /// <returns>The new instance of the <see cref="XPolyQuadraticBezierSegment"/> class.</returns>
        public static XPolyQuadraticBezierSegment Create(ImmutableArray<XPoint> points, bool isStroked, bool isSmoothJoin)
        {
            return new XPolyQuadraticBezierSegment()
            {
                Points = points,
                IsStroked = isStroked,
                IsSmoothJoin = isSmoothJoin
            };
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return (Points != null) && (Points.Length >= 1) ? "Q" + ToString(Points) : "";
        }
    }
}

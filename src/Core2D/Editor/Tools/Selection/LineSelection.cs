﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Core2D.Project;
using Core2D.Shape;
using Core2D.Shapes;
using Core2D.Style;

namespace Core2D.Editor.Tools.Selection
{
    /// <summary>
    /// Helper class for <see cref="XLine"/> shape selection.
    /// </summary>
    public class LineSelection
    {
        private readonly XLayer _layer;
        private readonly XLine _shape;
        private readonly ShapeStyle _style;
        private readonly BaseShape _point;
        private XPoint _startHelperPoint;
        private XPoint _endHelperPoint;

        /// <summary>
        /// Initialize new instance of <see cref="LineSelection"/> class.
        /// </summary>
        /// <param name="layer">The selection shapes layer.</param>
        /// <param name="shape">The selected shape.</param>
        /// <param name="style">The selection shapes style.</param>
        /// <param name="point">The selection point shape.</param>
        public LineSelection(XLayer layer, XLine shape, ShapeStyle style, BaseShape point)
        {
            _layer = layer;
            _shape = shape;
            _style = style;
            _point = point;
        }

        /// <summary>
        /// Transfer selection state to <see cref="ToolState.One"/>.
        /// </summary>
        public void ToStateOne()
        {
            _startHelperPoint = XPoint.Create(0, 0, _point);
            _endHelperPoint = XPoint.Create(0, 0, _point);

            _layer.Shapes = _layer.Shapes.Add(_startHelperPoint);
            _layer.Shapes = _layer.Shapes.Add(_endHelperPoint);
        }

        /// <summary>
        /// Move selection.
        /// </summary>
        public void Move()
        {
            if (_startHelperPoint != null)
            {
                _startHelperPoint.X = _shape.Start.X;
                _startHelperPoint.Y = _shape.Start.Y;
            }

            if (_endHelperPoint != null)
            {
                _endHelperPoint.X = _shape.End.X;
                _endHelperPoint.Y = _shape.End.Y;
            }
        }

        /// <summary>
        /// Remove selection.
        /// </summary>
        public void Remove()
        {
            if (_startHelperPoint != null)
            {
                _layer.Shapes = _layer.Shapes.Remove(_startHelperPoint);
                _startHelperPoint = null;
            }

            if (_endHelperPoint != null)
            {
                _layer.Shapes = _layer.Shapes.Remove(_endHelperPoint);
                _endHelperPoint = null;
            }
        }
    }
}

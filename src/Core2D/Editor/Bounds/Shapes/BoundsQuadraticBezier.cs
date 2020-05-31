﻿using System;
using System.Collections.Generic;
using Core2D.Renderer;
using Core2D.Shapes;
using Spatial;

namespace Core2D.Editor.Bounds.Shapes
{
    public class BoundsQuadraticBezier : IBounds
    {
        private List<IPointShape> _points = new List<IPointShape>();
        public Type TargetType => typeof(IQuadraticBezierShape);

        public IPointShape TryToGetPoint(IBaseShape shape, Point2 target, double radius, double scale, IDictionary<Type, IBounds> registered)
        {
            if (!(shape is IQuadraticBezierShape quadratic))
            {
                throw new ArgumentNullException(nameof(shape));
            }

            var pointHitTest = registered[typeof(IPointShape)];

            if (pointHitTest.TryToGetPoint(quadratic.Point1, target, radius, scale, registered) != null)
            {
                return quadratic.Point1;
            }

            if (pointHitTest.TryToGetPoint(quadratic.Point2, target, radius, scale, registered) != null)
            {
                return quadratic.Point2;
            }

            if (pointHitTest.TryToGetPoint(quadratic.Point3, target, radius, scale, registered) != null)
            {
                return quadratic.Point3;
            }

            return null;
        }

        public bool Contains(IBaseShape shape, Point2 target, double radius, double scale, IDictionary<Type, IBounds> registered)
        {
            if (!(shape is IQuadraticBezierShape quadratic))
            {
                throw new ArgumentNullException(nameof(shape));
            }

            _points.Clear();
            quadratic.GetPoints(_points);

            if (quadratic.State.Flags.HasFlag(ShapeStateFlags.Size) && scale != 1.0)
            {
                return HitTestHelper.Contains(_points, target, scale);
            }
            else
            {
                return HitTestHelper.Contains(_points, target, 1.0);
            }
        }

        public bool Overlaps(IBaseShape shape, Rect2 target, double radius, double scale, IDictionary<Type, IBounds> registered)
        {
            if (!(shape is IQuadraticBezierShape quadratic))
            {
                throw new ArgumentNullException(nameof(shape));
            }

            _points.Clear();
            quadratic.GetPoints(_points);

            if (quadratic.State.Flags.HasFlag(ShapeStateFlags.Size) && scale != 1.0)
            {
                return HitTestHelper.Overlap(_points, target, scale);
            }
            else
            {
                return HitTestHelper.Overlap(_points, target, 1.0);
            }
        }
    }
}

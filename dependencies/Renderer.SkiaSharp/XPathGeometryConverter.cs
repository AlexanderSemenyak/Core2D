﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Linq;
using Core2D.Path;
using Core2D.Path.Segments;
using Core2D.Shapes;
using SkiaSharp;

namespace Renderer.SkiaSharp
{
    /// <summary>
    /// 
    /// </summary>
    public static class XPathGeometryConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="xpg"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static SKPath ToSKPath(this XPathGeometry xpg, double dx, double dy, Func<double, float> scale)
        {
            var path = new SKPath();
            path.FillType = xpg.FillRule == XFillRule.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;

            foreach (var xpf in xpg.Figures)
            {
                var previous = default(XPoint);

                // Begin new figure.
                path.MoveTo(
                    scale(xpf.StartPoint.X + dx),
                    scale(xpf.StartPoint.Y + dy));

                previous = xpf.StartPoint;

                foreach (var segment in xpf.Segments)
                {
                    if (segment is XArcSegment)
                    {
                        //var arcSegment = segment as XArcSegment;
                        // TODO: Convert WPF/SVG elliptical arc segment format to GDI+ bezier curves.
                        //startPoint = arcSegment.Point;
                    }
                    else if (segment is XCubicBezierSegment)
                    {
                        var cubicBezierSegment = segment as XCubicBezierSegment;
                        path.CubicTo(
                            scale(cubicBezierSegment.Point1.X + dx),
                            scale(cubicBezierSegment.Point1.Y + dy),
                            scale(cubicBezierSegment.Point2.X + dx),
                            scale(cubicBezierSegment.Point2.Y + dy),
                            scale(cubicBezierSegment.Point3.X + dx),
                            scale(cubicBezierSegment.Point3.Y + dy));

                        previous = cubicBezierSegment.Point3;
                    }
                    else if (segment is XLineSegment)
                    {
                        var lineSegment = segment as XLineSegment;
                        path.LineTo(
                            scale(lineSegment.Point.X + dx),
                            scale(lineSegment.Point.Y + dy));

                        previous = lineSegment.Point;
                    }
                    else if (segment is XPolyCubicBezierSegment)
                    {
                        var polyCubicBezierSegment = segment as XPolyCubicBezierSegment;
                        if (polyCubicBezierSegment.Points.Count >= 3)
                        {
                            path.CubicTo(
                                scale(polyCubicBezierSegment.Points[0].X + dx),
                                scale(polyCubicBezierSegment.Points[0].Y + dy),
                                scale(polyCubicBezierSegment.Points[1].X + dx),
                                scale(polyCubicBezierSegment.Points[1].Y + dy),
                                scale(polyCubicBezierSegment.Points[2].X + dx),
                                scale(polyCubicBezierSegment.Points[2].Y + dy));

                            previous = polyCubicBezierSegment.Points[2];
                        }

                        if (polyCubicBezierSegment.Points.Count > 3
                            && polyCubicBezierSegment.Points.Count % 3 == 0)
                        {
                            for (int i = 3; i < polyCubicBezierSegment.Points.Count; i += 3)
                            {
                                path.CubicTo(
                                    scale(polyCubicBezierSegment.Points[i].X + dx),
                                    scale(polyCubicBezierSegment.Points[i].Y + dy),
                                    scale(polyCubicBezierSegment.Points[i + 1].X + dx),
                                    scale(polyCubicBezierSegment.Points[i + 1].Y + dy),
                                    scale(polyCubicBezierSegment.Points[i + 2].X + dx),
                                    scale(polyCubicBezierSegment.Points[i + 2].Y + dy));

                                previous = polyCubicBezierSegment.Points[i + 2];
                            }
                        }
                    }
                    else if (segment is XPolyLineSegment)
                    {
                        var polyLineSegment = segment as XPolyLineSegment;
                        if (polyLineSegment.Points.Count >= 1)
                        {
                            path.LineTo(
                                scale(polyLineSegment.Points[0].X + dx),
                                scale(polyLineSegment.Points[0].Y + dy));

                            previous = polyLineSegment.Points[0];
                        }

                        if (polyLineSegment.Points.Count > 1)
                        {
                            for (int i = 1; i < polyLineSegment.Points.Count; i++)
                            {
                                path.LineTo(
                                    scale(polyLineSegment.Points[i].X + dx),
                                    scale(polyLineSegment.Points[i].Y + dy));

                                previous = polyLineSegment.Points[i];
                            }
                        }
                    }
                    else if (segment is XPolyQuadraticBezierSegment)
                    {
                        var polyQuadraticSegment = segment as XPolyQuadraticBezierSegment;
                        if (polyQuadraticSegment.Points.Count >= 2)
                        {
                            path.QuadTo(
                                scale(polyQuadraticSegment.Points[0].X + dx),
                                scale(polyQuadraticSegment.Points[0].Y + dy),
                                scale(polyQuadraticSegment.Points[1].X + dx),
                                scale(polyQuadraticSegment.Points[1].Y + dy));

                            previous = polyQuadraticSegment.Points[1];
                        }

                        if (polyQuadraticSegment.Points.Count > 2
                            && polyQuadraticSegment.Points.Count % 2 == 0)
                        {
                            for (int i = 3; i < polyQuadraticSegment.Points.Count; i += 3)
                            {
                                path.QuadTo(
                                    scale(polyQuadraticSegment.Points[i].X + dx),
                                    scale(polyQuadraticSegment.Points[i].Y + dy),
                                    scale(polyQuadraticSegment.Points[i + 1].X + dx),
                                    scale(polyQuadraticSegment.Points[i + 1].Y + dy));

                                previous = polyQuadraticSegment.Points[i + 1];
                            }
                        }
                    }
                    else if (segment is XQuadraticBezierSegment)
                    {
                        var quadraticBezierSegment = segment as XQuadraticBezierSegment;
                        path.QuadTo(
                            scale(quadraticBezierSegment.Point1.X + dx),
                            scale(quadraticBezierSegment.Point1.Y + dy),
                            scale(quadraticBezierSegment.Point2.X + dx),
                            scale(quadraticBezierSegment.Point2.Y + dy));

                        previous = quadraticBezierSegment.Point2;
                    }
                    else
                    {
                        throw new NotSupportedException("Not supported segment type: " + segment.GetType());
                    }
                }

                if (xpf.IsClosed)
                {
                    path.Close();
                }
            }

            return path;
        }
    }
}

﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Core2D.Attributes;

namespace Core2D.Style
{
    /// <summary>
    /// Line style.
    /// </summary>
    public class LineStyle : ObservableObject
    {
        private string _name;
        private bool _isCurved;
        private double _curvature;
        private CurveOrientation _curveOrientation;
        private LineFixedLength _fixedLength;

        /// <summary>
        /// Gets or sets line style name.
        /// </summary>
        [Name]
        public string Name
        {
            get { return _name; }
            set { Update(ref _name, value); }
        }

        /// <summary>
        /// Gets or sets value indicating whether line is curved.
        /// </summary>
        public bool IsCurved
        {
            get { return _isCurved; }
            set { Update(ref _isCurved, value); }
        }

        /// <summary>
        /// Gets or sets line curvature.
        /// </summary>
        public double Curvature
        {
            get { return _curvature; }
            set { Update(ref _curvature, value); }
        }

        /// <summary>
        /// Gets or sets curve orientation.
        /// </summary>
        public CurveOrientation CurveOrientation
        {
            get { return _curveOrientation; }
            set { Update(ref _curveOrientation, value); }
        }

        /// <summary>
        /// Gets or sets line fixed length.
        /// </summary>
        public LineFixedLength FixedLength
        {
            get { return _fixedLength; }
            set { Update(ref _fixedLength, value); }
        }

        /// <summary>
        /// Creates a new <see cref="LineStyle"/> instance.
        /// </summary>
        /// <param name="name">The line style name.</param>
        /// <param name="isCurved">The flag indicating whether line is curved.</param>
        /// <param name="curvature">The line curvature.</param>
        /// <param name="curveOrientation">The curve orientation.</param>
        /// <param name="fixedLength">The line style fixed length.</param>
        /// <returns>The new instance of the <see cref="LineStyle"/> class.</returns>
        public static LineStyle Create(
            string name = "", 
            bool isCurved = false, 
            double curvature = 50.0, 
            CurveOrientation curveOrientation = CurveOrientation.Auto, 
            LineFixedLength fixedLength = null)
        {
            return new LineStyle()
            {
                Name = name,
                IsCurved = isCurved,
                Curvature = curvature,
                CurveOrientation = curveOrientation,
                FixedLength = fixedLength ?? LineFixedLength.Create()
            };
        }

        /// <summary>
        /// Clones line style.
        /// </summary>
        /// <returns>The new instance of the <see cref="LineStyle"/> class.</returns>
        public LineStyle Clone()
        {
            return new LineStyle()
            {
                Name = _name,
                IsCurved = _isCurved,
                Curvature = _curvature,
                CurveOrientation = _curveOrientation,
                FixedLength = _fixedLength.Clone()
            };
        }
    }
}

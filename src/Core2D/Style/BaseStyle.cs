﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Core2D.Attributes;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Core2D.Style
{
    /// <summary>
    /// Base style.
    /// </summary>
    public abstract class BaseStyle : ObservableResource
    {
        private string _name;
        private ArgbColor _stroke;
        private ArgbColor _fill;
        private double _thickness;
        private LineCap _lineCap;
        private string _dashes;
        private double _dashOffset;

        /// <summary>
        /// Gets or sets style name.
        /// </summary>
        [Name]
        public string Name
        {
            get { return _name; }
            set { Update(ref _name, value); }
        }

        /// <summary>
        /// Gets or sets stroke color.
        /// </summary>
        public ArgbColor Stroke
        {
            get { return _stroke; }
            set { Update(ref _stroke, value); }
        }

        /// <summary>
        /// Gets or sets fill color.
        /// </summary>
        public ArgbColor Fill
        {
            get { return _fill; }
            set { Update(ref _fill, value); }
        }

        /// <summary>
        /// Gets or sets stroke thickness.
        /// </summary>
        public double Thickness
        {
            get { return _thickness; }
            set { Update(ref _thickness, value); }
        }

        /// <summary>
        /// Gets or sets line cap.
        /// </summary>
        public LineCap LineCap
        {
            get { return _lineCap; }
            set { Update(ref _lineCap, value); }
        }

        /// <summary>
        /// Gets or sets line dashes.
        /// </summary>
        public string Dashes
        {
            get { return _dashes; }
            set { Update(ref _dashes, value); }
        }

        /// <summary>
        /// Gets or sets line dash offset.
        /// </summary>
        public double DashOffset
        {
            get { return _dashOffset; }
            set { Update(ref _dashOffset, value); }
        }

        /// <summary>
        /// Convert line dashes doubles array to string format.
        /// </summary>
        /// <param name="value">The line dashes doubles array.</param>
        /// <returns>The converted line dashes string.</returns>
        public static string ConvertDoubleArrayToDashes(double[] value)
        {
            try
            {
                if (value != null)
                {
                    return string.Join(
                        " ",
                        value.Select(x => x.ToString(CultureInfo.InvariantCulture)));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }

            return null;
        }

        /// <summary>
        /// Convert line dashes floats array to string format.
        /// </summary>
        /// <param name="value">The line dashes floats array.</param>
        /// <returns>The converted line dashes string.</returns>
        public static string ConvertFloatArrayToDashes(float[] value)
        {
            try
            {
                if (value != null)
                {
                    return string.Join(
                        " ",
                        value.Select(x => x.ToString(CultureInfo.InvariantCulture)));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }

            return null;
        }

        /// <summary>
        /// Convert line dashes string format to doubles array.
        /// </summary>
        /// <param name="value">The line dashes string.</param>
        /// <returns>The converted line dashes doubles array.</returns>
        public static double[] ConvertDashesToDoubleArray(string value)
        {
            try
            {
                if (value != null)
                {
                    string[] a = value.Split(
                        new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);
                    if (a != null && a.Length > 0)
                    {
                        return a.Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }

            return null;
        }

        /// <summary>
        /// Convert line dashes string format to floats array.
        /// </summary>
        /// <param name="value">The line dashes string.</param>
        /// <returns>The converted line dashes floats array.</returns>
        public static float[] ConvertDashesToFloatArray(string value)
        {
            try
            {
                if (value != null)
                {
                    string[] a = value.Split(
                        new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);
                    if (a != null && a.Length > 0)
                    {
                        return a.Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }

            return null;
        }
    }
}

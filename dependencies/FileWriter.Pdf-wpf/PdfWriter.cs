﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Core2D.Interfaces;
using Core2D.Project;
using Core2D.Renderer;
using Core2D.Shape;
#if WPF
using Renderer.PdfSharp_wpf;
#elif CORE
using Renderer.PdfSharp_core;
#endif

#if WPF
namespace FileWriter.Pdf_wpf
#elif CORE
namespace FileWriter.Pdf_core
#endif
{
    /// <summary>
    /// PdfSharp file writer.
    /// </summary>
    public sealed class PdfWriter : IFileWriter
    {
        /// <inheritdoc/>
        string IFileWriter.Name { get; } = "Pdf";

        /// <inheritdoc/>
        string IFileWriter.Extension { get; } = "pdf";

        /// <inheritdoc/>
        void IFileWriter.Save(string path, object item, object options)
        {
            if (string.IsNullOrEmpty(path) || item == null)
                return;

            var ic = options as IImageCache;
            if (options == null)
                return;

            var r = new PdfRenderer();
            r.State.DrawShapeState.Flags = ShapeStateFlags.Printable;
            r.State.ImageCache = ic;

            if (item is XPage)
            {
                r.Save(path, item as XPage);
            }
            else if (item is XDocument)
            {
                r.Save(path, item as XDocument);
            }
            else if (item is XProject)
            {
                r.Save(path, item as XProject);
            }
        }
    }
}

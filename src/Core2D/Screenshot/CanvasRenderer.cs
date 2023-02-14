using System;
using Avalonia.Controls;
using Avalonia.Rendering;
using SkiaSharp;

namespace Core2D.Screenshot;

public static class CanvasRenderer
{
    public static void Render(Control target, SKCanvas canvas, double dpi = 96, bool useDeferredRenderer = false)
    {
        // TODO:
        /*
        var renderTarget = new CanvasRenderTarget(canvas, dpi);
        if (useDeferredRenderer)
        {
            using var renderer = new DeferredRenderer(target, renderTarget);
            renderer.Start();
            var renderLoopTask = renderer as IRenderLoopTask;
            renderLoopTask.Update(TimeSpan.Zero);
            renderLoopTask.Render();
        }
        else
        {
            ImmediateRenderer.Render(target, renderTarget);
        }
        */
    }
}

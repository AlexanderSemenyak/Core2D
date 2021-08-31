﻿#nullable enable
using Core2D.Model.Renderer;
using Core2D.ViewModels.Style;

namespace Core2D.Model
{
    public interface IDrawable
    {
        ShapeStyleViewModel? Style { get; set; }

        bool IsStroked { get; set; }

        bool IsFilled { get; set; }

        void DrawShape(object? dc, IShapeRenderer? renderer, ISelection? selection);

        void DrawPoints(object? dc, IShapeRenderer? renderer, ISelection? selection);

        bool Invalidate(IShapeRenderer? renderer);
    }
}

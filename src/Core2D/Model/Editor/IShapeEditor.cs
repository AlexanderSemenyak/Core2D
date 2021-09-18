﻿#nullable enable
using System.Collections.Generic;
using Core2D.ViewModels.Path;
using Core2D.ViewModels.Shapes;
using Core2D.ViewModels.Style;

namespace Core2D.Model.Editor
{
    public interface IShapeEditor
    {
        void BreakPathFigure(PathFigureViewModel pathFigure, ShapeStyleViewModel style, bool isStroked, bool isFilled, List<BaseShapeViewModel> result);

        bool BreakPathShape(PathShapeViewModel pathShape, List<BaseShapeViewModel> result);

        void BreakShape(BaseShapeViewModel shape, List<BaseShapeViewModel> result, List<BaseShapeViewModel> remove);
    }
}

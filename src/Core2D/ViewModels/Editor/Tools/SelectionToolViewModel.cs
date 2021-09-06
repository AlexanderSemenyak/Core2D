﻿#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Core2D.Model;
using Core2D.Model.Editor;
using Core2D.Model.Input;
using Core2D.Model.Renderer;
using Core2D.ViewModels.Shapes;
using Core2D.Spatial;

namespace Core2D.ViewModels.Editor.Tools
{
    public partial class SelectionToolViewModel : ViewModelBase, IEditorTool
    {
        public enum State { None, Selected }
        private State _currentState = State.None;
        private RectangleShapeViewModel _rectangleShape;
        private decimal _startX;
        private decimal _startY;
        private decimal _historyX;
        private decimal _historyY;
        private IEnumerable<PointShapeViewModel> _pointsCache;
        private IEnumerable<BaseShapeViewModel> _shapesCache;

        public string Title => "Selection";

        public SelectionToolViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override object Copy(IDictionary<object, object>? shared)
        {
            throw new NotImplementedException();
        }

        private static bool IsPointMovable(BaseShapeViewModel shape, PointShapeViewModel point)
        {
            if (point.State.HasFlag(ShapeStateFlags.Locked) || (point.Owner is BaseShapeViewModel ower && ower.State.HasFlag(ShapeStateFlags.Locked)))
            {
                return false;
            }

            if (point.State.HasFlag(ShapeStateFlags.Connector) && point.Owner != shape)
            {
                return false;
            }

            return true;
        }

        private static IEnumerable<PointShapeViewModel> GetMovePoints(IEnumerable<BaseShapeViewModel> shapes)
        {
            var points = new List<PointShapeViewModel>();

            foreach (var shape in shapes)
            {
                shape.GetPoints(points);
            }

            return points.Where(p => IsPointMovable(p.Owner as BaseShapeViewModel, p)).Distinct();
        }

        private void GenerateMoveSelectionCache()
        {
            var editor = ServiceProvider.GetService<ProjectEditorViewModel>();

            if (editor.Project.SelectedShapes is { })
            {
                var shapes = editor.Project.SelectedShapes
                    .Where(s => !s.State.HasFlag(ShapeStateFlags.Locked)
                             && !s.State.HasFlag(ShapeStateFlags.Connector));

                switch (editor.Project.Options.MoveMode)
                {
                    case MoveMode.Point:
                        {
                            _pointsCache = GetMovePoints(shapes).ToList();
                        }
                        break;
                    case MoveMode.Shape:
                        {
                            _shapesCache = shapes.ToList();
                        }
                        break;
                }
            }
        }

        private void DisposeMoveSelectionCache()
        {
            _pointsCache = null;
            _shapesCache = null;
        }

        private void MoveSelectionCacheTo(InputArgs args)
        {
            var editor = ServiceProvider.GetService<ProjectEditorViewModel>();
            (decimal sx, decimal sy) = editor.TryToSnap(args);
            decimal dx = sx - _startX;
            decimal dy = sy - _startY;

            _startX = sx;
            _startY = sy;

            if (_pointsCache is { })
            {
                editor.MoveShapesBy(_pointsCache, dx, dy);
            }

            if (_shapesCache is { })
            {
                editor.MoveShapesBy(_shapesCache, dx, dy);
            }
        }

        private bool IsSelectionAvailable()
        {
            var editor = ServiceProvider.GetService<ProjectEditorViewModel>();
            return editor?.Project?.SelectedShapes is { };
        }

        private bool HitTestDecorator(InputArgs args, bool isControl, bool isHover)
        {
            var editor = ServiceProvider.GetService<ProjectEditorViewModel>();

            if (isControl == false && editor.PageState.Decorator is { } && editor.PageState.Decorator.IsVisible)
            {
                bool decoratorResult = editor.PageState.Decorator.HitTest(args);
                if (decoratorResult == true && isHover == false)
                {
                    editor.IsToolIdle = false;
                    _currentState = State.Selected;
                    return true;
                }
            }

            return false;
        }

        public void BeginDown(InputArgs args)
        {
            var factory = ServiceProvider.GetService<IViewModelFactory>();
            var editor = ServiceProvider.GetService<ProjectEditorViewModel>();
            (double x, double y) = args;
            (decimal sx, decimal sy) = editor.TryToSnap(args);
            switch (_currentState)
            {
                case State.None:
                    {
                        if (editor.PageState is null)
                        {
                            return;
                        }

                        bool isControl = args.Modifier.HasFlag(ModifierFlags.Control);

                        editor.Dehover(editor.Project.CurrentContainer.CurrentLayer);

                        if (isControl == false && editor.PageState.DrawDecorators == true && editor.PageState.Decorator is { } && editor.PageState.Decorator.IsVisible == true)
                        {
                            if (HitTestDecorator(args, isControl, false) == true)
                            {
                                return;
                            }
                        }

                        if (isControl == true)
                        {
                            var shapes = editor.Project.CurrentContainer.CurrentLayer.Shapes.Reverse();
                            double radius = editor.Project.Options.HitThreshold / editor.PageState.ZoomX;
                            BaseShapeViewModel result = editor.HitTest.TryToGetPoint(shapes, new Point2(x, y), radius, editor.PageState.ZoomX);
                            if (result is null)
                            {
                                result = editor.HitTest.TryToGetShape(shapes, new Point2(x, y), radius, editor.PageState.ZoomX);
                            }

                            if (result is { })
                            {
                                if (editor.Project.SelectedShapes is null)
                                {
                                    editor.Project.SelectedShapes = new HashSet<BaseShapeViewModel>() { result };
                                    editor.Project.CurrentContainer.CurrentLayer.RaiseInvalidateLayer();
                                    editor.OnShowOrHideDecorator();
                                    HitTestDecorator(args, isControl, false);
                                    break;
                                }
                                else if (editor.Project.SelectedShapes is { })
                                {
                                    if (editor.Project.SelectedShapes.Contains(result))
                                    {
                                        var selected = new HashSet<BaseShapeViewModel>(editor.Project.SelectedShapes);
                                        selected.Remove(result);

                                        if (selected.Count == 0)
                                        {
                                            editor.Project.SelectedShapes = null;
                                            editor.OnHideDecorator();
                                        }
                                        else
                                        {
                                            editor.Project.SelectedShapes = selected;
                                            editor.OnShowOrHideDecorator();
                                            HitTestDecorator(args, isControl, false);
                                        }
                                        editor.Project.CurrentContainer.CurrentLayer.RaiseInvalidateLayer();
                                        break;
                                    }
                                    else
                                    {
                                        var selected = new HashSet<BaseShapeViewModel>(editor.Project.SelectedShapes);
                                        selected.Add(result);

                                        editor.Project.SelectedShapes = selected;

                                        editor.Project.CurrentContainer.CurrentLayer.RaiseInvalidateLayer();
                                        editor.OnShowOrHideDecorator();
                                        HitTestDecorator(args, isControl, false);
                                        break;
                                    }
                                }
                            }
                        }

                        if (isControl == false && editor.PageState.DrawDecorators == true && editor.PageState.Decorator is { } && editor.PageState.Decorator.IsVisible == true)
                        {
                            editor.OnHideDecorator();
                        }

                        if (editor.Project.SelectedShapes is { })
                        {
                            var shapes = editor.Project.CurrentContainer.CurrentLayer.Shapes.Reverse();

                            double radius = editor.Project.Options.HitThreshold / editor.PageState.ZoomX;
                            BaseShapeViewModel result = editor.HitTest.TryToGetPoint(shapes, new Point2(x, y), radius, editor.PageState.ZoomX);
                            if (result is null)
                            {
                                result = editor.HitTest.TryToGetShape(shapes, new Point2(x, y), radius, editor.PageState.ZoomX);
                            }

                            if (result is { } && editor.Project.SelectedShapes.Contains(result))
                            {
                                editor.IsToolIdle = false;
                                _startX = sx;
                                _startY = sy;
                                _historyX = _startX;
                                _historyY = _startY;
                                GenerateMoveSelectionCache();
                                _currentState = State.Selected;
                                editor.OnShowOrHideDecorator();
                                HitTestDecorator(args, isControl, false);
                                break;
                            }
                        }

                        var deselect = !isControl;

                        if (editor.TryToSelectShape(editor.Project.CurrentContainer.CurrentLayer, x, y, deselect))
                        {
                            editor.IsToolIdle = false;
                            _startX = sx;
                            _startY = sy;
                            _historyX = _startX;
                            _historyY = _startY;
                            GenerateMoveSelectionCache();
                            _currentState = State.Selected;
                            editor.OnShowOrHideDecorator();
                            HitTestDecorator(args, isControl, false);
                            break;
                        }

                        editor.IsToolIdle = false;
                        _rectangleShape = factory.CreateRectangleShape(
                            x, y,
                            editor.PageState.SelectionStyle,
                            true, true);
                        _rectangleShape.State |= ShapeStateFlags.Thickness;
                        editor.Project.CurrentContainer.WorkingLayer.Shapes = editor.Project.CurrentContainer.WorkingLayer.Shapes.Add(_rectangleShape);
                        editor.Project.CurrentContainer.WorkingLayer.RaiseInvalidateLayer();
                        _currentState = State.Selected;
                    }
                    break;
                case State.Selected:
                    {
                        if (_rectangleShape is { })
                        {
                            _rectangleShape.BottomRight.X = x;
                            _rectangleShape.BottomRight.Y = y;
                            editor.Project.CurrentContainer.WorkingLayer.Shapes = editor.Project.CurrentContainer.WorkingLayer.Shapes.Remove(_rectangleShape);
                            editor.Project.CurrentContainer.WorkingLayer.RaiseInvalidateLayer();
                            _currentState = State.None;
                            editor.IsToolIdle = true;
                        }
                    }
                    break;
            }
        }

        public void BeginUp(InputArgs args)
        {
            var editor = ServiceProvider.GetService<ProjectEditorViewModel>();
            switch (_currentState)
            {
                case State.None:
                    break;
                case State.Selected:
                    {
                        bool isControl = args.Modifier.HasFlag(ModifierFlags.Control);

                        if (_rectangleShape is { })
                        {
                            _rectangleShape.BottomRight.X = args.X;
                            _rectangleShape.BottomRight.Y = args.Y;
                            editor.Project.CurrentContainer.WorkingLayer.Shapes = editor.Project.CurrentContainer.WorkingLayer.Shapes.Remove(_rectangleShape);
                            editor.Project.CurrentContainer.WorkingLayer.RaiseInvalidateLayer();
                        }

                        if (IsSelectionAvailable() && !isControl)
                        {
                            (decimal sx, decimal sy) = editor.TryToSnap(args);
                            if (_historyX != sx || _historyY != sy)
                            {
                                decimal dx = sx - _historyX;
                                decimal dy = sy - _historyY;

                                var previous = new
                                {
                                    DeltaX = -dx,
                                    DeltaY = -dy,
                                    Points = _pointsCache,
                                    Shapes = _shapesCache
                                };
                                var next = new
                                {
                                    DeltaX = dx,
                                    DeltaY = dy,
                                    Points = _pointsCache,
                                    Shapes = _shapesCache
                                };
                                editor.Project?.History?.Snapshot(previous, next,
                                    (state) =>
                                    {
                                        if (state.Points is { })
                                        {
                                            editor.MoveShapesBy(state.Points, state.DeltaX, state.DeltaY);
                                        }

                                        if (state.Shapes is { })
                                        {
                                            editor.MoveShapesBy(state.Shapes, state.DeltaX, state.DeltaY);
                                        }
                                    });
                            }

                            DisposeMoveSelectionCache();
                            _currentState = State.None;
                            editor.IsToolIdle = true;
                            break;
                        }

                        var deselect = !isControl;
                        var includeSelected = isControl;

                        if (_rectangleShape is { })
                        {
                            _currentState = State.None;
                            editor.TryToSelectShapes(editor.Project.CurrentContainer.CurrentLayer, _rectangleShape, deselect, includeSelected);
                            editor.OnShowOrHideDecorator();
                            editor.IsToolIdle = true;
                        }
                    }
                    break;
            }
        }

        public void EndDown(InputArgs args)
        {
            var editor = ServiceProvider.GetService<ProjectEditorViewModel>();
            switch (_currentState)
            {
                case State.None:
                    {
                        editor.Dehover(editor.Project.CurrentContainer.CurrentLayer);
                    }
                    break;
                case State.Selected:
                    {
                        DisposeMoveSelectionCache();
                        editor.OnHideDecorator();
                        editor.IsToolIdle = true;
                    }
                    break;
            }
        }

        public void EndUp(InputArgs args)
        {
        }

        public void Move(InputArgs args)
        {
            var editor = ServiceProvider.GetService<ProjectEditorViewModel>();
            switch (_currentState)
            {
                case State.None:
                    {
                        if (editor.PageState is null)
                        {
                            return;
                        }

                        bool isControl = args.Modifier.HasFlag(ModifierFlags.Control);

                        if (!isControl)
                        {
                            editor.TryToHoverShape(args.X, args.Y);
                            HitTestDecorator(args, isControl, true);
                        }
                    }
                    break;
                case State.Selected:
                    {
                        bool isControl = args.Modifier.HasFlag(ModifierFlags.Control);

                        if (isControl == false && editor.PageState.DrawDecorators == true && editor.PageState.Decorator is { } && editor.PageState.Decorator.IsVisible == true)
                        {
                            editor.PageState.Decorator.Move(args);
                            editor.PageState.Decorator.Update(false);
                            return;
                        }

                        HitTestDecorator(args, isControl, true);

                        if (IsSelectionAvailable() && !isControl)
                        {
                            MoveSelectionCacheTo(args);
                            editor.OnUpdateDecorator();
                            editor.Project.CurrentContainer.CurrentLayer.RaiseInvalidateLayer();
                            break;
                        }

                        if (_rectangleShape is { })
                        {
                            _rectangleShape.BottomRight.X = args.X;
                            _rectangleShape.BottomRight.Y = args.Y;
                            editor.Project.CurrentContainer.WorkingLayer.RaiseInvalidateLayer();
                        }
                    }
                    break;
            }
        }

        public void Move(BaseShapeViewModel shape)
        {
        }

        public void Finalize(BaseShapeViewModel shape)
        {
        }

        public void Reset()
        {
            var editor = ServiceProvider.GetService<ProjectEditorViewModel>();

            _currentState = State.None;

            editor.Dehover(editor.Project?.CurrentContainer?.CurrentLayer);

            DisposeMoveSelectionCache();
            editor.OnHideDecorator();

            editor.Project?.CurrentContainer?.CurrentLayer?.RaiseInvalidateLayer();
            editor.Project?.CurrentContainer?.WorkingLayer?.RaiseInvalidateLayer();

            editor.IsToolIdle = true;
        }
    }
}

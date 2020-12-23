﻿#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive.Disposables;
using Core2D.Model;
using Core2D.Model.Renderer;
using Core2D.ViewModels.Data;

namespace Core2D.ViewModels.Shapes
{
    public partial class RectangleShapeViewModel : BaseShapeViewModel
    {
        [AutoNotify] private PointShapeViewModel _topLeft;
        [AutoNotify] private PointShapeViewModel _bottomRight;

        public RectangleShapeViewModel(IServiceProvider serviceProvider) : base(serviceProvider, typeof(RectangleShapeViewModel))
        {
        }

        public override void DrawShape(object dc, IShapeRenderer renderer, ISelection selection)
        {
            if (State.HasFlag(ShapeStateFlags.Visible))
            {
                renderer.DrawRectangle(dc, this, Style);
            }
        }

        public override void DrawPoints(object dc, IShapeRenderer renderer, ISelection selection)
        {
            if (selection?.SelectedShapes is { })
            {
                if (selection.SelectedShapes.Contains(this))
                {
                    _topLeft.DrawShape(dc, renderer, selection);
                    _bottomRight.DrawShape(dc, renderer, selection);
                }
                else
                {
                    if (selection.SelectedShapes.Contains(_topLeft))
                    {
                        _topLeft.DrawShape(dc, renderer, selection);
                    }

                    if (selection.SelectedShapes.Contains(_bottomRight))
                    {
                        _bottomRight.DrawShape(dc, renderer, selection);
                    }
                }
            }
        }

        public override void Bind(DataFlow dataFlow, object db, object r)
        {
            var record = Record ?? r;

            dataFlow.Bind(this, db, record);

            _topLeft.Bind(dataFlow, db, record);
            _bottomRight.Bind(dataFlow, db, record);
        }

        public override void Move(ISelection selection, decimal dx, decimal dy)
        {
            if (!_topLeft.State.HasFlag(ShapeStateFlags.Connector))
            {
                _topLeft.Move(selection, dx, dy);
            }

            if (!_bottomRight.State.HasFlag(ShapeStateFlags.Connector))
            {
                _bottomRight.Move(selection, dx, dy);
            }
        }

        public override void Select(ISelection selection)
        {
            base.Select(selection);
            _topLeft.Select(selection);
            _bottomRight.Select(selection);
        }

        public override void Deselect(ISelection selection)
        {
            base.Deselect(selection);
            _topLeft.Deselect(selection);
            _bottomRight.Deselect(selection);
        }

        public override void GetPoints(IList<PointShapeViewModel> points)
        {
            points.Add(_topLeft);
            points.Add(_bottomRight);
        }

        public override bool IsDirty()
        {
            var isDirty = base.IsDirty();

            isDirty |= _topLeft.IsDirty();
            isDirty |= _bottomRight.IsDirty();

            return isDirty;
        }

        public override void Invalidate()
        {
            base.Invalidate();
            _topLeft.Invalidate();
            _bottomRight.Invalidate();
        }

        public override IDisposable Subscribe(IObserver<(object sender, PropertyChangedEventArgs e)> observer)
        {
            var mainDisposable = new CompositeDisposable();
            var disposablePropertyChanged = default(IDisposable);
            var disposableStyle = default(IDisposable);
            var disposableProperties = default(CompositeDisposable);
            var disposableRecord = default(IDisposable);
            var disposableTopLeft = default(IDisposable);
            var disposableBottomRight = default(IDisposable);

            ObserveSelf(Handler, ref disposablePropertyChanged, mainDisposable);
            ObserveObject(_style, ref disposableStyle, mainDisposable, observer);
            ObserveList(_properties, ref disposableProperties, mainDisposable, observer);
            ObserveObject(_record, ref disposableRecord, mainDisposable, observer);
            ObserveObject(_topLeft, ref disposableTopLeft, mainDisposable, observer);
            ObserveObject(_bottomRight, ref disposableBottomRight, mainDisposable, observer);

            void Handler(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(Style))
                {
                    ObserveObject(_style, ref disposableStyle, mainDisposable, observer);
                }

                if (e.PropertyName == nameof(Properties))
                {
                    ObserveList(_properties, ref disposableProperties, mainDisposable, observer);
                }

                if (e.PropertyName == nameof(Record))
                {
                    ObserveObject(_record, ref disposableRecord, mainDisposable, observer);
                }

                if (e.PropertyName == nameof(TopLeft))
                {
                    ObserveObject(_topLeft, ref disposableTopLeft, mainDisposable, observer);
                }

                if (e.PropertyName == nameof(BottomRight))
                {
                    ObserveObject(_bottomRight, ref disposableBottomRight, mainDisposable, observer);
                }

                observer.OnNext((sender, e));
            }

            return mainDisposable;
        }
    }
}

﻿#nullable disable
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Core2D.Model.Renderer;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Core2D.Modules.Renderer;
using Core2D.Model;
using Core2D.ViewModels.Containers;
using Core2D.ViewModels.Data;
using Core2D.ViewModels.Renderer.Presenters;

namespace Core2D.Views.Renderer
{
    public enum PresenterType
    {
        None = 0,

        Data = 1,

        Template = 2,

        Editor = 3,

        Export = 4
    }

    internal struct CustomState
    {
        public FrameContainerViewModel Container;
        public IShapeRenderer Renderer;
        public ISelection Selection;
        public DataFlow DataFlow;
        public PresenterType PresenterType;
    }

    internal class CustomDrawOperation : ICustomDrawOperation
    {
        public PresenterView PresenterView { get; set; }

        public CustomState CustomState { get; set; }

        public Rect Bounds { get; set; }

        public void Dispose()
        {
        }

        public bool HitTest(Point p) => false;

        public bool Equals(ICustomDrawOperation other) => false;

        public void Render(IDrawingContextImpl context)
        {
            PresenterView.Draw(CustomState, context);
        }
    }

    public class PresenterView : UserControl
    {
        private static readonly IContainerPresenter s_editorPresenter = new EditorPresenter();
        private static readonly IContainerPresenter s_templatePresenter = new TemplatePresenter();
        private static readonly IContainerPresenter s_exportPresenter = new ExportPresenter();

        public static readonly StyledProperty<ZoomBorder> ZoomBorderProperty =
            AvaloniaProperty.Register<PresenterView, ZoomBorder>(nameof(ZoomBorder), null);

        public static readonly StyledProperty<FrameContainerViewModel> ContainerProperty =
            AvaloniaProperty.Register<PresenterView, FrameContainerViewModel>(nameof(Container), null);

        public static readonly StyledProperty<IShapeRenderer> RendererProperty =
            AvaloniaProperty.Register<PresenterView, IShapeRenderer>(nameof(Renderer), null);

        public static readonly StyledProperty<ISelection> SelectionProperty =
            AvaloniaProperty.Register<PresenterView, ISelection>(nameof(Selection), null);

        public static readonly StyledProperty<DataFlow> DataFlowProperty =
            AvaloniaProperty.Register<PresenterView, DataFlow>(nameof(DataFlow), null);

        public static readonly StyledProperty<PresenterType> PresenterTypeProperty =
            AvaloniaProperty.Register<PresenterView, PresenterType>(nameof(PresenterType), PresenterType.None);

        public ZoomBorder ZoomBorder
        {
            get => GetValue(ZoomBorderProperty);
            set => SetValue(ZoomBorderProperty, value);
        }

        public FrameContainerViewModel Container
        {
            get => GetValue(ContainerProperty);
            set => SetValue(ContainerProperty, value);
        }

        public IShapeRenderer Renderer
        {
            get => GetValue(RendererProperty);
            set => SetValue(RendererProperty, value);
        }

        public ISelection Selection
        {
            get => GetValue(SelectionProperty);
            set => SetValue(SelectionProperty, value);
        }

        public DataFlow DataFlow
        {
            get => GetValue(DataFlowProperty);
            set => SetValue(DataFlowProperty, value);
        }

        public PresenterType PresenterType
        {
            get => GetValue(PresenterTypeProperty);
            set => SetValue(PresenterTypeProperty, value);
        }

        public PresenterView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            RenderImpl(context);
        }

        private void RenderImpl(DrawingContext context)
        {
            var customState = new CustomState()
            {
                Container = Container,
                Renderer = Renderer ?? GetValue(RendererOptions.RendererProperty),
                Selection = Selection ?? GetValue(RendererOptions.SelectionProperty),
                DataFlow = DataFlow ?? GetValue(RendererOptions.DataFlowProperty),
                PresenterType = PresenterType,
            };

            var offset = this.TranslatePoint(new Point(0, 0), ZoomBorder) ?? default;
            var bounds = new Rect(
                offset.X > 0.0 ? -offset.X : 0.0,
                offset.Y > 0.0 ? -offset.Y : 0.0,
                ZoomBorder.Bounds.Width + (offset.X > 0.0 ? offset.X : -offset.X),
                ZoomBorder.Bounds.Height + (offset.Y > 0.0 ? offset.Y : -offset.Y));

            var customDrawOperation = new CustomDrawOperation
            {
                PresenterView = this,
                CustomState = customState,
                Bounds = bounds
            };

            context.Custom(customDrawOperation);
        }

        internal static void Draw(CustomState customState, object context)
        {
            switch (customState.PresenterType)
            {
                case PresenterType.None:
                    break;

                case PresenterType.Data:
                    {
                        if (customState.Container is { } && customState.DataFlow is { })
                        {
                            var db = (object)customState.Container.Properties;
                            var record = (object)customState.Container.Record;

                            if (customState.Container is PageContainerViewModel page)
                            {
                                customState.DataFlow.Bind(page.Template, db, record);
                            }
                            customState.DataFlow.Bind(customState.Container, db, record);
                        }
                    }
                    break;

                case PresenterType.Template:
                    {
                        if (customState.Container is { } && customState.Renderer is { })
                        {
                            s_templatePresenter.Render(context, customState.Renderer, customState.Selection, customState.Container, 0.0, 0.0);
                            if (customState.Container is PageContainerViewModel page)
                            {
                                page.Template?.Invalidate();
                            }
                        }
                    }
                    break;

                case PresenterType.Editor:
                    {
                        if (customState.Container is { } && customState.Renderer is { })
                        {
                            s_editorPresenter.Render(context, customState.Renderer, customState.Selection, customState.Container, 0.0, 0.0);

                            customState.Container?.Invalidate();
                            customState.Renderer.State.PointStyle.Invalidate();
                            customState.Renderer.State.SelectedPointStyle.Invalidate();
                        }
                    }
                    break;

                case PresenterType.Export:
                    {
                        if (customState.Container is { } && customState.Renderer is { })
                        {
                            s_exportPresenter.Render(context, customState.Renderer, customState.Selection, customState.Container, 0.0, 0.0);
                        }
                    }
                    break;
            }
        }
    }
}

﻿#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Core2D.Model;
using Core2D.Model.Editor;
using Core2D.Model.Input;
using Core2D.Model.Renderer;
using Core2D.ViewModels.Containers;
using Core2D.ViewModels.Data;
using Core2D.ViewModels.Editor.History;
using Core2D.ViewModels.Editor.Recent;
using Core2D.ViewModels.Editor.Tools.Decorators;
using Core2D.ViewModels.Editors;
using Core2D.ViewModels.Layout;
using Core2D.ViewModels.Renderer;
using Core2D.ViewModels.Scripting;
using Core2D.ViewModels.Shapes;
using Core2D.ViewModels.Style;
using Core2D.Spatial;
using Core2D.ViewModels.Docking;
using Dock.Model.Controls;
using Dock.Model.Core;
using static System.Math;

namespace Core2D.ViewModels.Editor
{
    public partial class ProjectEditorViewModel : ViewModelBase, IDialogPresenter
    {
        private readonly ShapeEditor _shapeEditor;
        private readonly DockFactory _dockFactory;
        [AutoNotify] private IRootDock _rootDock;
        [AutoNotify] private ProjectContainerViewModel _project;
        [AutoNotify] private string _projectPath;
        [AutoNotify] private bool _isProjectDirty;
        [AutoNotify] private IDisposable _observer;
        [AutoNotify] private bool _isToolIdle;
        [AutoNotify] private IEditorTool _currentTool;
        [AutoNotify] private IPathTool _currentPathTool;
        [AutoNotify] private ImmutableArray<RecentFileViewModel> _recentProjects;
        [AutoNotify] private RecentFileViewModel _currentRecentProject;
        [AutoNotify] private AboutInfoViewModel _aboutInfo;
        [AutoNotify] private IList<DialogViewModel> _dialogs;
        private readonly Lazy<ImmutableArray<IEditorTool>> _tools;
        private readonly Lazy<ImmutableArray<IPathTool>> _pathTools;
        private readonly Lazy<IHitTest> _hitTest;
        private readonly Lazy<ILog> _log;
        private readonly Lazy<DataFlow> _dataFlow;
        private readonly Lazy<IShapeRenderer> _renderer;
        private readonly Lazy<IFileSystem> _fileSystem;
        private readonly Lazy<IViewModelFactory> _factory;
        private readonly Lazy<IContainerFactory> _containerFactory;
        private readonly Lazy<IShapeFactory> _shapeFactory;
        private readonly Lazy<ITextClipboard> _textClipboard;
        private readonly Lazy<IJsonSerializer> _jsonSerializer;
        private readonly Lazy<ImmutableArray<IFileWriter>> _fileWriters;
        private readonly Lazy<ImmutableArray<ITextFieldReader<DatabaseViewModel>>> _textFieldReaders;
        private readonly Lazy<ImmutableArray<ITextFieldWriter<DatabaseViewModel>>> _textFieldWriters;
        private readonly Lazy<IImageImporter> _imageImporter;
        private readonly Lazy<IScriptRunner> _scriptRunner;
        private readonly Lazy<IProjectEditorPlatform> _platform;
        private readonly Lazy<IEditorCanvasPlatform> _canvasPlatform;
        private readonly Lazy<StyleEditorViewModel> _styleEditor;
        private readonly Lazy<IPathConverter> _pathConverter;
        private readonly Lazy<ISvgConverter> _svgConverter;

        public ImmutableArray<IEditorTool> Tools => _tools.Value;

        public ImmutableArray<IPathTool> PathTools => _pathTools.Value;

        public IHitTest HitTest => _hitTest.Value;

        public ILog Log => _log.Value;

        public DataFlow DataFlow => _dataFlow.Value;

        public IShapeRenderer Renderer => _renderer.Value;

        public ShapeRendererStateViewModel PageState => _renderer.Value?.State;

        public ISelection Selection => _project;

        public IFileSystem FileSystem => _fileSystem.Value;

        public IViewModelFactory ViewModelFactory => _factory.Value;

        public IContainerFactory ContainerFactory => _containerFactory.Value;

        public IShapeFactory ShapeFactory => _shapeFactory.Value;

        public ITextClipboard TextClipboard => _textClipboard.Value;

        public IJsonSerializer JsonSerializer => _jsonSerializer.Value;

        public ImmutableArray<IFileWriter> FileWriters => _fileWriters.Value;

        public ImmutableArray<ITextFieldReader<DatabaseViewModel>> TextFieldReaders => _textFieldReaders.Value;

        public ImmutableArray<ITextFieldWriter<DatabaseViewModel>> TextFieldWriters => _textFieldWriters.Value;

        public IImageImporter ImageImporter => _imageImporter.Value;

        public IScriptRunner ScriptRunner => _scriptRunner.Value;

        public IProjectEditorPlatform Platform => _platform.Value;

        public IEditorCanvasPlatform CanvasPlatform => _canvasPlatform.Value;

        public StyleEditorViewModel StyleEditor => _styleEditor.Value;

        public IPathConverter PathConverter => _pathConverter.Value;

        public ISvgConverter SvgConverter => _svgConverter.Value;

        private object ScriptState { get; set; }

        private PageContainerViewModel PageToCopy { get; set; }

        private DocumentContainerViewModel DocumentToCopy { get; set; }

        private BaseShapeViewModel HoveredShapeViewModel { get; set; }

        public ProjectEditorViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _shapeEditor = new ShapeEditor(serviceProvider);
            _recentProjects = ImmutableArray.Create<RecentFileViewModel>();
            _currentRecentProject = default;
            _dialogs = new ObservableCollection<DialogViewModel>();
            _tools = serviceProvider.GetServiceLazily<IEditorTool[], ImmutableArray<IEditorTool>>((tools) => tools.ToImmutableArray());
            _pathTools = serviceProvider.GetServiceLazily<IPathTool[], ImmutableArray<IPathTool>>((tools) => tools.ToImmutableArray());
            _hitTest = serviceProvider.GetServiceLazily<IHitTest>(hitTests => hitTests.Register(serviceProvider.GetService<IBounds[]>()));
            _log = serviceProvider.GetServiceLazily<ILog>();
            _dataFlow = serviceProvider.GetServiceLazily<DataFlow>();
            _renderer = serviceProvider.GetServiceLazily<IShapeRenderer>();
            _fileSystem = serviceProvider.GetServiceLazily<IFileSystem>();
            _factory = serviceProvider.GetServiceLazily<IViewModelFactory>();
            _containerFactory = serviceProvider.GetServiceLazily<IContainerFactory>();
            _shapeFactory = serviceProvider.GetServiceLazily<IShapeFactory>();
            _textClipboard = serviceProvider.GetServiceLazily<ITextClipboard>();
            _jsonSerializer = serviceProvider.GetServiceLazily<IJsonSerializer>();
            _fileWriters = serviceProvider.GetServiceLazily<IFileWriter[], ImmutableArray<IFileWriter>>((writers) => writers.ToImmutableArray());
            _textFieldReaders = serviceProvider.GetServiceLazily<ITextFieldReader<DatabaseViewModel>[], ImmutableArray<ITextFieldReader<DatabaseViewModel>>>((readers) => readers.ToImmutableArray());
            _textFieldWriters = serviceProvider.GetServiceLazily<ITextFieldWriter<DatabaseViewModel>[], ImmutableArray<ITextFieldWriter<DatabaseViewModel>>>((writers) => writers.ToImmutableArray());
            _imageImporter = serviceProvider.GetServiceLazily<IImageImporter>();
            _scriptRunner = serviceProvider.GetServiceLazily<IScriptRunner>();
            _platform = serviceProvider.GetServiceLazily<IProjectEditorPlatform>();
            _canvasPlatform = serviceProvider.GetServiceLazily<IEditorCanvasPlatform>();
            _styleEditor = serviceProvider.GetServiceLazily<StyleEditorViewModel>();
            _pathConverter = serviceProvider.GetServiceLazily<IPathConverter>();
            _svgConverter = serviceProvider.GetServiceLazily<ISvgConverter>();

            _dockFactory = new DockFactory(this);
            _rootDock = _dockFactory.CreateLayout();
            _dockFactory?.InitLayout(_rootDock);
            // TODO:
            _dockFactory.PagesDock?.CreateDocument?.Execute(null);
            NavigateTo("Dashboard");
        }

        public void NavigateTo(string id)
        {
            _rootDock?.Navigate.Execute(id);
        }

        public void OnToggleDockableVisibility(string id)
        {
            // TODO:
        }

        public void ShowDialog(DialogViewModel dialog)
        {
            _dialogs.Add(dialog);
        }

        public void CloseDialog(DialogViewModel dialog)
        {
            _dialogs.Remove(dialog);
        }

        public DialogViewModel CreateTextBindingDialog(TextShapeViewModel text)
        {
            var textBindingEditor = new TextBindingEditorViewModel(_serviceProvider)
            {
                Editor = this,
                Text = text
            };

            return new DialogViewModel(_serviceProvider, this)
            {
                Title = "Text Binding",
                IsOverlayVisible = false,
                IsTitleBarVisible = true,
                IsCloseButtonVisible = true,
                ViewModel = textBindingEditor
            };
        }

        public (decimal sx, decimal sy) TryToSnap(InputArgs args)
        {
            if (Project is { } && Project.Options.SnapToGrid == true)
            {
                return (
                    PointUtil.Snap((decimal)args.X, (decimal)Project.Options.SnapX),
                    PointUtil.Snap((decimal)args.Y, (decimal)Project.Options.SnapY));
            }
            else
            {
                return ((decimal)args.X, (decimal)args.Y);
            }
        }

        public string GetName(object item)
        {
            if (item is ViewModelBase observable)
            {
                return observable.Name;
            }
            return string.Empty;
        }

        public void SetShapeName(BaseShapeViewModel shape, IEnumerable<BaseShapeViewModel> source = null)
        {
            var input = source ?? _project.GetAllShapes();
            var shapes = input.Where(s => s.GetType() == shape.GetType() && s != shape).ToList();
            var count = shapes.Count + 1;
            var update = string.IsNullOrEmpty(shape.Name) || input.Any(x => x != shape && x.Name == shape.Name);
            if (update)
            {
                shape.Name = shape.GetType().Name.Replace("ShapeViewModel", " ") + count;
            }
        }

        public void OnNew(object item)
        {
            if (item is LayerContainerViewModel layer)
            {
                if (layer.Owner is PageContainerViewModel page)
                {
                    OnAddLayer(page);
                }
            }
            else if (item is PageContainerViewModel page)
            {
                OnNewPage(page);
            }
            else if (item is DocumentContainerViewModel document)
            {
                OnNewPage(document);
            }
            else if (item is ProjectContainerViewModel)
            {
                OnNewDocument();
            }
            else if (item is ProjectEditorViewModel)
            {
                OnNewProject();
            }
            else if (item is null)
            {
                if (Project is null)
                {
                    OnNewProject();
                }
                else
                {
                    if (Project.CurrentDocument is null)
                    {
                        OnNewDocument();
                    }
                    else
                    {
                        OnNewPage(Project.CurrentDocument);
                    }
                }
            }
        }

        public void OnNewPage(PageContainerViewModel selected)
        {
            var document = Project?.Documents.FirstOrDefault(d => d.Pages.Contains(selected));
            if (document is { })
            {
                var page =
                    ContainerFactory?.GetPage(Project, ProjectEditorConfiguration.DefaultPageName)
                    ?? ViewModelFactory.CreatePageContainer(ProjectEditorConfiguration.DefaultPageName);

                Project?.AddPage(document, page);
                Project?.SetCurrentContainer(page);
            }
        }

        public void OnNewPage(DocumentContainerViewModel selected)
        {
            var page =
                ContainerFactory?.GetPage(Project, ProjectEditorConfiguration.DefaultPageName)
                ?? ViewModelFactory.CreatePageContainer(ProjectEditorConfiguration.DefaultPageName);

            Project?.AddPage(selected, page);
            Project?.SetCurrentContainer(page);
        }

        public void OnNewDocument()
        {
            var document =
                ContainerFactory?.GetDocument(Project, ProjectEditorConfiguration.DefaultDocumentName)
                ?? ViewModelFactory.CreateDocumentContainer(ProjectEditorConfiguration.DefaultDocumentName);

            Project?.AddDocument(document);
            Project?.SetCurrentDocument(document);
            Project?.SetCurrentContainer(document?.Pages.FirstOrDefault());
        }

        public void OnNewProject()
        {
            OnUnload();
            OnLoad(ContainerFactory?.GetProject() ?? ViewModelFactory.CreateProjectContainer(), string.Empty);
            CanvasPlatform?.ResetZoom?.Invoke();
            CanvasPlatform?.InvalidateControl?.Invoke();
            NavigateTo("Home");
        }

        public void OnOpenProject(string path)
        {
            try
            {
                if (FileSystem is { } && JsonSerializer is { })
                {
                    if (!string.IsNullOrEmpty(path) && FileSystem.Exists(path))
                    {
                        var project = ViewModelFactory.OpenProjectContainer(path, FileSystem, JsonSerializer);
                        if (project is { })
                        {
                            OnOpenProjectImpl(project, path);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        private void OnOpenProjectImpl(ProjectContainerViewModel project, string path)
        {
            try
            {
                if (project is { })
                {
                    OnUnload();
                    OnLoad(project, path);
                    OnAddRecent(path, project.Name);
                    CanvasPlatform?.ResetZoom?.Invoke();
                    CanvasPlatform?.InvalidateControl?.Invoke();
                    NavigateTo("Home");
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnCloseProject()
        {
            Project?.History?.Reset();
            OnUnload();
            NavigateTo("Dashboard");
        }

        public void OnSaveProject(string path)
        {
            try
            {
                if (Project is { } && FileSystem is { } && JsonSerializer is { })
                {
                    var isDecoratorVisible = PageState.Decorator?.IsVisible == true;
                    if (isDecoratorVisible)
                    {
                        OnHideDecorator();
                    }

                    ViewModelFactory.SaveProjectContainer(Project, path, FileSystem, JsonSerializer);
                    OnAddRecent(path, Project.Name);

                    if (string.IsNullOrEmpty(ProjectPath))
                    {
                        ProjectPath = path;
                    }

                    IsProjectDirty = false;

                    if (isDecoratorVisible)
                    {
                        OnShowDecorator();
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnImportData(ProjectContainerViewModel project, string path, ITextFieldReader<DatabaseViewModel> reader)
        {
            try
            {
                if (project is { })
                {
                    using var stream = FileSystem.Open(path);
                    var db = reader?.Read(stream);
                    if (db is { })
                    {
                        project.AddDatabase(db);
                        project.SetCurrentDatabase(db);
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnExportData(string path, DatabaseViewModel database, ITextFieldWriter<DatabaseViewModel> writer)
        {
            try
            {
                using var stream = FileSystem.Create(path);
                writer?.Write(stream, database);
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnUpdateData(string path, DatabaseViewModel database, ITextFieldReader<DatabaseViewModel> reader)
        {
            try
            {
                using var stream = FileSystem.Open(path);
                var db = reader?.Read(stream);
                if (db is { })
                {
                    Project?.UpdateDatabase(database, db);
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnImportObject(object item, bool restore)
        {
            if (item is ShapeStyleViewModel style)
            {
                Project?.AddStyle(Project?.CurrentStyleLibrary, style);
            }
            else if (item is IList<ShapeStyleViewModel> styleList)
            {
                Project.AddItems(Project?.CurrentStyleLibrary, styleList);
            }
            else if (item is BaseShapeViewModel)
            {
                if (item is GroupShapeViewModel group)
                {
                    if (restore)
                    {
                        var shapes = Enumerable.Repeat(group, 1);
                        TryToRestoreRecords(shapes);
                    }
                    Project.AddGroup(Project?.CurrentGroupLibrary, group);
                }
                else
                {
                    Project?.AddShape(Project?.CurrentContainer?.CurrentLayer, item as BaseShapeViewModel);
                }
            }
            else if (item is IList<GroupShapeViewModel> groups)
            {
                if (restore)
                {
                    TryToRestoreRecords(groups);
                }
                Project.AddItems(Project?.CurrentGroupLibrary, groups);
            }
            else if (item is LibraryViewModel<ShapeStyleViewModel> sl)
            {
                Project.AddStyleLibrary(sl);
            }
            else if (item is IList<LibraryViewModel<ShapeStyleViewModel>> sll)
            {
                Project.AddStyleLibraries(sll);
            }
            else if (item is LibraryViewModel<GroupShapeViewModel> gl)
            {
                TryToRestoreRecords(gl.Items);
                Project.AddGroupLibrary(gl);
            }
            else if (item is IList<LibraryViewModel<GroupShapeViewModel>> gll)
            {
                var shapes = gll.SelectMany(x => x.Items);
                TryToRestoreRecords(shapes);
                Project.AddGroupLibraries(gll);
            }
            else if (item is DatabaseViewModel db)
            {
                Project?.AddDatabase(db);
                Project?.SetCurrentDatabase(db);
            }
            else if (item is LayerContainerViewModel layer)
            {
                if (restore)
                {
                    TryToRestoreRecords(layer.Shapes);
                }
                Project?.AddLayer(Project?.CurrentContainer, layer);
            }
            else if (item is TemplateContainerViewModel template)
            {
                if (restore)
                {
                    var shapes = template.Layers.SelectMany(x => x.Shapes);
                    TryToRestoreRecords(shapes);
                }
                Project?.AddTemplate(template);
            }
            else if (item is PageContainerViewModel page)
            {
                if (restore)
                {
                    var shapes = Enumerable.Concat(
                        page.Layers.SelectMany(x => x.Shapes),
                        page.Template?.Layers.SelectMany(x => x.Shapes));
                    TryToRestoreRecords(shapes);
                }
                Project?.AddPage(Project?.CurrentDocument, page);
            }
            else if (item is IList<TemplateContainerViewModel> templates)
            {
                if (restore)
                {
                    var shapes = templates.SelectMany(x => x.Layers).SelectMany(x => x.Shapes);
                    TryToRestoreRecords(shapes);
                }

                // Import as templates.
                Project.AddTemplates(templates);
            }
            else if (item is IList<ScriptViewModel> scripts)
            {
                // Import as scripts.
                Project.AddScripts(scripts);
            }
            else if (item is ScriptViewModel script)
            {
                Project?.AddScript(script);
            }
            else if (item is DocumentContainerViewModel document)
            {
                if (restore)
                {
                    var shapes = Enumerable.Concat(
                        document.Pages.SelectMany(x => x.Layers).SelectMany(x => x.Shapes),
                        document.Pages.SelectMany(x => x.Template.Layers).SelectMany(x => x.Shapes));
                    TryToRestoreRecords(shapes);
                }
                Project?.AddDocument(document);
            }
            else if (item is OptionsViewModel options)
            {
                if (Project is { })
                {
                    Project.Options = options;
                }
            }
            else if (item is ProjectContainerViewModel project)
            {
                OnUnload();
                OnLoad(project, string.Empty);
            }
            else
            {
                throw new NotSupportedException("Not supported import object.");
            }
        }

        public void OnImportJson(string path)
        {
            try
            {
                var json = FileSystem?.ReadUtf8Text(path);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var item = JsonSerializer.Deserialize<object>(json);
                    if (item is { })
                    {
                        OnImportObject(item, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnImportSvg(string path)
        {
            if (SvgConverter is null)
            {
                return;
            }
            var shapes = SvgConverter.Convert(path, out _, out _);
            if (shapes is { })
            {
                OnPasteShapes(shapes);
            }
        }

        public void OnExportJson(string path, object item)
        {
            try
            {
                var json = JsonSerializer?.Serialize(item);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    FileSystem?.WriteUtf8Text(path, json);
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnExport(string path, object item, IFileWriter writer)
        {
            try
            {
                using var stream = FileSystem.Create(path);
                writer?.Save(stream, item, Project);
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public async Task OnExecuteCode(string csharp)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(csharp))
                {
                    await ScriptRunner?.Execute(csharp, null);
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public async Task OnExecuteRepl(string csharp)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(csharp))
                {
                    ScriptState = await ScriptRunner?.Execute(csharp, ScriptState);
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnResetRepl()
        {
            ScriptState = null;
        }

        public async Task OnExecuteScriptFile(string path)
        {
            try
            {
                var csharp = FileSystem?.ReadUtf8Text(path);
                if (!string.IsNullOrWhiteSpace(csharp))
                {
                    await OnExecuteCode(csharp);
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public async Task OnExecuteScriptFile(string[] paths)
        {
            foreach (var path in paths)
            {
                await OnExecuteScriptFile(path);
            }
        }

        public async Task OnExecuteScript(ScriptViewModel script)
        {
            try
            {
                var csharp = script?.Code;
                if (!string.IsNullOrWhiteSpace(csharp))
                {
                    await OnExecuteRepl(csharp);
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnUndo()
        {
            try
            {
                if (Project?.History.CanUndo() ?? false)
                {
                    Deselect();
                    Project?.History.Undo();
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnRedo()
        {
            try
            {
                if (Project?.History.CanRedo() ?? false)
                {
                    Deselect();
                    Project?.History.Redo();
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnCut(object item)
        {
            if (item is BaseShapeViewModel shape)
            {
                // TODO:
            }
            else if (item is PageContainerViewModel page)
            {
                PageToCopy = page;
                DocumentToCopy = default;
                Project?.RemovePage(page);
                Project?.SetCurrentContainer(Project?.CurrentDocument?.Pages.FirstOrDefault());
            }
            else if (item is DocumentContainerViewModel document)
            {
                PageToCopy = default;
                DocumentToCopy = document;
                Project?.RemoveDocument(document);

                var selected = Project?.Documents.FirstOrDefault();
                Project?.SetCurrentDocument(selected);
                Project?.SetCurrentContainer(selected?.Pages.FirstOrDefault());
            }
            else if (item is ProjectEditorViewModel || item is null)
            {
                if (CanCopy())
                {
                    OnCopy(item);
                    OnDeleteSelected();
                }
            }
        }

        public void OnCopy(object item)
        {
            if (item is BaseShapeViewModel shape)
            {
                // TODO:
            }
            else if (item is PageContainerViewModel page)
            {
                PageToCopy = page;
                DocumentToCopy = default;
            }
            else if (item is DocumentContainerViewModel document)
            {
                PageToCopy = default;
                DocumentToCopy = document;
            }
            else if (item is ProjectEditorViewModel || item is null)
            {
                if (CanCopy())
                {
                    if (Project?.SelectedShapes is { })
                    {
                        OnCopyShapes(Project.SelectedShapes.ToList());
                    }
                }
            }
        }

        public async void OnPaste(object item)
        {
            if (Project is { } && item is BaseShapeViewModel shape)
            {
                // TODO:
            }
            else if (Project is { } && item is PageContainerViewModel page)
            {
                if (PageToCopy is { })
                {
                    var document = Project?.Documents.FirstOrDefault(d => d.Pages.Contains(page));
                    if (document is { })
                    {
                        int index = document.Pages.IndexOf(page);
                        var clone = Clone(PageToCopy);
                        Project.ReplacePage(document, clone, index);
                        Project.SetCurrentContainer(clone);
                    }
                }
            }
            else if (Project is { } && item is DocumentContainerViewModel document)
            {
                if (PageToCopy is { })
                {
                    var clone = Clone(PageToCopy);
                    Project?.AddPage(document, clone);
                    Project.SetCurrentContainer(clone);
                }
                else if (DocumentToCopy is { })
                {
                    int index = Project.Documents.IndexOf(document);
                    var clone = Clone(DocumentToCopy);
                    Project.ReplaceDocument(clone, index);
                    Project.SetCurrentDocument(clone);
                    Project.SetCurrentContainer(clone?.Pages.FirstOrDefault());
                }
            }
            else if (item is ProjectEditorViewModel || item is null)
            {
                if (await CanPaste())
                {
                    var text = await (TextClipboard?.GetText() ?? Task.FromResult(string.Empty));
                    if (!string.IsNullOrEmpty(text))
                    {
                        OnTryPaste(text);
                    }
                }
            }
            else if (item is string text)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    OnTryPaste(text);
                }
            }
        }

        private void Delete(object item)
        {
            if (item is BaseShapeViewModel shape)
            {
                Project?.RemoveShape(shape);
                OnDeselectAll();
            }
            else if (item is LayerContainerViewModel layer)
            {
                Project?.RemoveLayer(layer);

                var selected = Project?.CurrentContainer?.Layers.FirstOrDefault();
                if (layer.Owner is FrameContainerViewModel owner)
                {
                    owner.SetCurrentLayer(selected);
                }
            }
            else if (item is PageContainerViewModel page)
            {
                Project?.RemovePage(page);

                var selected = Project?.CurrentDocument?.Pages.FirstOrDefault();
                Project?.SetCurrentContainer(selected);
            }
            else if (item is DocumentContainerViewModel document)
            {
                Project?.RemoveDocument(document);

                var selected = Project?.Documents.FirstOrDefault();
                Project?.SetCurrentDocument(selected);
                Project?.SetCurrentContainer(selected?.Pages.FirstOrDefault());
            }
            else if (item is ProjectEditorViewModel || item is null)
            {
                OnDeleteSelected();
            }
        }

        public void OnDelete(object item)
        {
            if (item is IList<object> objects)
            {
                var copy = objects.ToList();

                foreach (var obj in copy)
                {
                    Delete(obj);
                }
            }
            else
            {
                Delete(item);
            }
        }

        public void OnShowDecorator()
        {
            if (PageState is null)
            {
                return;
            }

            if (PageState.DrawDecorators == false)
            {
                return;
            }

            var shapes = Project.SelectedShapes?.ToList();
            if (shapes is null || shapes.Count <= 0)
            {
                return;
            }

            if (PageState.Decorator is null)
            {
                PageState.Decorator = new BoxDecoratorViewModel(_serviceProvider);
            }

            PageState.Decorator.Layer = Project.CurrentContainer.WorkingLayer;
            PageState.Decorator.Shapes = shapes;
            PageState.Decorator.Update(true);
            PageState.Decorator.Show();
        }

        public void OnUpdateDecorator()
        {
            if (PageState is null)
            {
                return;
            }

            if (PageState.DrawDecorators == false)
            {
                return;
            }

            PageState.Decorator?.Update(false);
        }

        public void OnHideDecorator()
        {
            if (PageState is null)
            {
                return;
            }

            if (PageState.DrawDecorators == false)
            {
                return;
            }

            PageState.Decorator?.Hide();
        }

        public void OnShowOrHideDecorator()
        {
            if (PageState is null)
            {
                return;
            }

            if (PageState.DrawDecorators == false)
            {
                return;
            }

            if (Project.SelectedShapes?.Count == 1 && Project.SelectedShapes?.FirstOrDefault() is PointShapeViewModel)
            {
                OnHideDecorator();
                return;
            }

            if (Project.SelectedShapes?.Count == 1 && Project.SelectedShapes?.FirstOrDefault() is LineShapeViewModel)
            {
                OnHideDecorator();
                return;
            }

            if (Project.SelectedShapes is { })
            {
                OnShowDecorator();
            }
            else
            {
                OnHideDecorator();
            }
        }

        public void OnSelectAll()
        {
            try
            {
                Deselect(Project?.CurrentContainer?.CurrentLayer);
                Select(
                    Project?.CurrentContainer?.CurrentLayer,
                    new HashSet<BaseShapeViewModel>(Project?.CurrentContainer?.CurrentLayer?.Shapes));
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnDeselectAll()
        {
            try
            {
                Deselect(Project?.CurrentContainer?.CurrentLayer);
                OnUpdateDecorator();
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnClearAll()
        {
            try
            {
                var container = Project?.CurrentContainer;
                if (container is { })
                {
                    foreach (var layer in container.Layers)
                    {
                        Project?.ClearLayer(layer);
                    }

                    container.WorkingLayer.Shapes = ImmutableArray.Create<BaseShapeViewModel>();
                    container.HelperLayer.Shapes = ImmutableArray.Create<BaseShapeViewModel>();

                    Project.CurrentContainer.InvalidateLayer();
                    OnHideDecorator();
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnCancel()
        {
            OnDeselectAll();
            OnResetTool();
        }

        public void OnDuplicateSelected()
        {
            try
            {
                if (Project?.SelectedShapes is null)
                {
                    return;
                }

                var json = JsonSerializer?.Serialize(Project.SelectedShapes.ToList());
                if (string.IsNullOrEmpty(json))
                {
                    return;
                }

                var shapes = JsonSerializer?.Deserialize<IList<BaseShapeViewModel>>(json);
                if (shapes?.Count() <= 0)
                {
                    return;
                }

                OnPasteShapes(shapes);
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnGroupSelected()
        {
            var group = Group(Project?.SelectedShapes, ProjectEditorConfiguration.DefaulGroupName);
            if (group is { })
            {
                Select(Project?.CurrentContainer?.CurrentLayer, group);
            }
        }

        public void OnUngroupSelected()
        {
            var result = Ungroup(Project?.SelectedShapes);
            if (result == true && PageState is { })
            {
                Project.SelectedShapes = null;
                OnHideDecorator();
            }
        }

        public void OnRotateSelected(string degrees)
        {
            if (!double.TryParse(degrees, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
            {
                return;
            }

            var sources = Project?.SelectedShapes;
            if (sources is { })
            {
                BoxLayout.Rotate(sources, (decimal)value, Project?.History);
                OnUpdateDecorator();
            }
        }

        public void OnFlipHorizontalSelected()
        {
            var sources = Project?.SelectedShapes;
            if (sources is { })
            {
                BoxLayout.Flip(sources, FlipMode.Horizontal, Project?.History);
                OnUpdateDecorator();
            }
        }

        public void OnFlipVerticalSelected()
        {
            var sources = Project?.SelectedShapes;
            if (sources is { })
            {
                BoxLayout.Flip(sources, FlipMode.Vertical, Project?.History);
                OnUpdateDecorator();
            }
        }

        public void OnMoveUpSelected()
        {
            MoveBy(
                Project?.SelectedShapes,
                0m,
                Project.Options.SnapToGrid ? (decimal)-Project.Options.SnapY : -1m);
        }

        public void OnMoveDownSelected()
        {
            MoveBy(
                Project?.SelectedShapes,
                0m,
                Project.Options.SnapToGrid ? (decimal)Project.Options.SnapY : 1m);
        }

        public void OnMoveLeftSelected()
        {
            MoveBy(
                Project?.SelectedShapes,
                Project.Options.SnapToGrid ? (decimal)-Project.Options.SnapX : -1m,
                0m);
        }

        public void OnMoveRightSelected()
        {
            MoveBy(
                Project?.SelectedShapes,
                Project.Options.SnapToGrid ? (decimal)Project.Options.SnapX : 1m,
                0m);
        }

        public void OnStackHorizontallySelected()
        {
            var shapes = Project?.SelectedShapes;
            if (shapes is { })
            {
                var items = shapes.Where(s => !s.State.HasFlag(ShapeStateFlags.Locked));
                BoxLayout.Stack(items, StackMode.Horizontal, Project?.History);
                OnUpdateDecorator();
            }
        }

        public void OnStackVerticallySelected()
        {
            var shapes = Project?.SelectedShapes;
            if (shapes is { })
            {
                var items = shapes.Where(s => !s.State.HasFlag(ShapeStateFlags.Locked));
                BoxLayout.Stack(items, StackMode.Vertical, Project?.History);
                OnUpdateDecorator();
            }
        }

        public void OnDistributeHorizontallySelected()
        {
            var shapes = Project?.SelectedShapes;
            if (shapes is { })
            {
                var items = shapes.Where(s => !s.State.HasFlag(ShapeStateFlags.Locked));
                BoxLayout.Distribute(items, DistributeMode.Horizontal, Project?.History);
                OnUpdateDecorator();
            }
        }

        public void OnDistributeVerticallySelected()
        {
            var shapes = Project?.SelectedShapes;
            if (shapes is { })
            {
                var items = shapes.Where(s => !s.State.HasFlag(ShapeStateFlags.Locked));
                BoxLayout.Distribute(items, DistributeMode.Vertical, Project?.History);
                OnUpdateDecorator();
            }
        }

        public void OnAlignLeftSelected()
        {
            var shapes = Project?.SelectedShapes;
            if (shapes is { })
            {
                var items = shapes.Where(s => !s.State.HasFlag(ShapeStateFlags.Locked));
                BoxLayout.Align(items, AlignMode.Left, Project?.History);
                OnUpdateDecorator();
            }
        }

        public void OnAlignCenteredSelected()
        {
            var shapes = Project?.SelectedShapes;
            if (shapes is { })
            {
                var items = shapes.Where(s => !s.State.HasFlag(ShapeStateFlags.Locked));
                BoxLayout.Align(items, AlignMode.Centered, Project?.History);
                OnUpdateDecorator();
            }
        }

        public void OnAlignRightSelected()
        {
            var shapes = Project?.SelectedShapes;
            if (shapes is { })
            {
                var items = shapes.Where(s => !s.State.HasFlag(ShapeStateFlags.Locked));
                BoxLayout.Align(items, AlignMode.Right, Project?.History);
                OnUpdateDecorator();
            }
        }

        public void OnAlignTopSelected()
        {
            var shapes = Project?.SelectedShapes;
            if (shapes is { })
            {
                var items = shapes.Where(s => !s.State.HasFlag(ShapeStateFlags.Locked));
                BoxLayout.Align(items, AlignMode.Top, Project?.History);
                OnUpdateDecorator();
            }
        }

        public void OnAlignCenterSelected()
        {
            var shapes = Project?.SelectedShapes;
            if (shapes is { })
            {
                var items = shapes.Where(s => !s.State.HasFlag(ShapeStateFlags.Locked));
                BoxLayout.Align(items, AlignMode.Center, Project?.History);
                OnUpdateDecorator();
            }
        }

        public void OnAlignBottomSelected()
        {
            var shapes = Project?.SelectedShapes;
            if (shapes is { })
            {
                var items = shapes.Where(s => !s.State.HasFlag(ShapeStateFlags.Locked));
                BoxLayout.Align(items, AlignMode.Bottom, Project?.History);
                OnUpdateDecorator();
            }
        }

        public void OnBringToFrontSelected()
        {
            var sources = Project?.SelectedShapes;
            if (sources is { })
            {
                foreach (var s in sources)
                {
                    BringToFront(s);
                }
            }
        }

        public void OnBringForwardSelected()
        {
            var sources = Project?.SelectedShapes;
            if (sources is { })
            {
                foreach (var s in sources)
                {
                    BringForward(s);
                }
            }
        }

        public void OnSendBackwardSelected()
        {
            var sources = Project?.SelectedShapes;
            if (sources is { })
            {
                foreach (var s in sources.Reverse())
                {
                    SendBackward(s);
                }
            }
        }

        public void OnSendToBackSelected()
        {
            var sources = Project?.SelectedShapes;
            if (sources is { })
            {
                foreach (var s in sources.Reverse())
                {
                    SendToBack(s);
                }
            }
        }

        public void OnCreatePath()
        {
            if (PathConverter is null)
            {
                return;
            }

            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is null)
            {
                return;
            }

            var sources = Project?.SelectedShapes;
            var source = Project?.SelectedShapes?.FirstOrDefault();

            if (sources is { } && sources.Count == 1)
            {
                var path = PathConverter.ToPathShape(source);
                if (path is { })
                {
                    var shapesBuilder = layer.Shapes.ToBuilder();

                    var index = shapesBuilder.IndexOf(source);
                    shapesBuilder[index] = path;

                    var previous = layer.Shapes;
                    var next = shapesBuilder.ToImmutable();
                    Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                    layer.Shapes = next;

                    Select(layer, path);
                }
            }

            if (sources is { } && sources.Count > 1)
            {
                var path = PathConverter.ToPathShape(sources);
                if (path is null)
                {
                    return;
                }

                var shapesBuilder = layer.Shapes.ToBuilder();

                foreach (var shape in sources)
                {
                    shapesBuilder.Remove(shape);
                }
                shapesBuilder.Add(path);

                var previous = layer.Shapes;
                var next = shapesBuilder.ToImmutable();
                Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                layer.Shapes = next;

                Select(layer, path);
            }
        }

        public void OnCreateStrokePath()
        {
            if (PathConverter is null)
            {
                return;
            }

            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is null)
            {
                return;
            }

            var sources = Project?.SelectedShapes;
            var source = Project?.SelectedShapes?.FirstOrDefault();

            if (sources is { } && sources.Count == 1)
            {
                var path = PathConverter.ToStrokePathShape(source);
                if (path is { })
                {
                    path.IsStroked = false;
                    path.IsFilled = true;

                    var shapesBuilder = layer.Shapes.ToBuilder();

                    var index = shapesBuilder.IndexOf(source);
                    shapesBuilder[index] = path;

                    var previous = layer.Shapes;
                    var next = shapesBuilder.ToImmutable();
                    Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                    layer.Shapes = next;

                    Select(layer, path);
                }
            }

            if (sources is { } && sources.Count > 1)
            {
                var paths = new List<PathShapeViewModel>();
                var shapes = new List<BaseShapeViewModel>();

                foreach (var s in sources)
                {
                    var path = PathConverter.ToStrokePathShape(s);
                    if (path is { })
                    {
                        path.IsStroked = false;
                        path.IsFilled = true;

                        paths.Add(path);
                        shapes.Add(s);
                    }
                }

                if (paths.Count > 0)
                {
                    var shapesBuilder = layer.Shapes.ToBuilder();

                    for (int i = 0; i < paths.Count; i++)
                    {
                        var index = shapesBuilder.IndexOf(shapes[i]);
                        shapesBuilder[index] = paths[i];
                    }

                    var previous = layer.Shapes;
                    var next = shapesBuilder.ToImmutable();
                    Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                    layer.Shapes = next;

                    Select(layer, new HashSet<BaseShapeViewModel>(paths));
                }
            }
        }

        public void OnCreateFillPath()
        {
            if (PathConverter is null)
            {
                return;
            }

            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is null)
            {
                return;
            }

            var sources = Project?.SelectedShapes;
            var source = Project?.SelectedShapes?.FirstOrDefault();

            if (sources is { } && sources.Count == 1)
            {
                var path = PathConverter.ToFillPathShape(source);
                if (path is { })
                {
                    path.IsStroked = false;
                    path.IsFilled = true;

                    var shapesBuilder = layer.Shapes.ToBuilder();

                    var index = shapesBuilder.IndexOf(source);
                    shapesBuilder[index] = path;

                    var previous = layer.Shapes;
                    var next = shapesBuilder.ToImmutable();
                    Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                    layer.Shapes = next;

                    Select(layer, path);
                }
            }

            if (sources is { } && sources.Count > 1)
            {
                var paths = new List<PathShapeViewModel>();
                var shapes = new List<BaseShapeViewModel>();

                foreach (var s in sources)
                {
                    var path = PathConverter.ToFillPathShape(s);
                    if (path is { })
                    {
                        path.IsStroked = false;
                        path.IsFilled = true;

                        paths.Add(path);
                        shapes.Add(s);
                    }
                }

                if (paths.Count > 0)
                {
                    var shapesBuilder = layer.Shapes.ToBuilder();

                    for (int i = 0; i < paths.Count; i++)
                    {
                        var index = shapesBuilder.IndexOf(shapes[i]);
                        shapesBuilder[index] = paths[i];
                    }

                    var previous = layer.Shapes;
                    var next = shapesBuilder.ToImmutable();
                    Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                    layer.Shapes = next;

                    Select(layer, new HashSet<BaseShapeViewModel>(paths));
                }
            }
        }

        public void OnCreateWindingPath()
        {
            if (PathConverter is null)
            {
                return;
            }

            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is null)
            {
                return;
            }

            var sources = Project?.SelectedShapes;
            var source = Project?.SelectedShapes?.FirstOrDefault();

            if (sources is { } && sources.Count == 1)
            {
                var path = PathConverter.ToWindingPathShape(source);
                if (path is { })
                {
                    path.IsStroked = false;
                    path.IsFilled = true;

                    var shapesBuilder = layer.Shapes.ToBuilder();

                    var index = shapesBuilder.IndexOf(source);
                    shapesBuilder[index] = path;

                    var previous = layer.Shapes;
                    var next = shapesBuilder.ToImmutable();
                    Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                    layer.Shapes = next;

                    Select(layer, path);
                }
            }

            if (sources is { } && sources.Count > 1)
            {
                var paths = new List<PathShapeViewModel>();
                var shapes = new List<BaseShapeViewModel>();

                foreach (var s in sources)
                {
                    var path = PathConverter.ToWindingPathShape(s);
                    if (path is { })
                    {
                        path.IsStroked = false;
                        path.IsFilled = true;

                        paths.Add(path);
                        shapes.Add(s);
                    }
                }

                if (paths.Count > 0)
                {
                    var shapesBuilder = layer.Shapes.ToBuilder();

                    for (int i = 0; i < paths.Count; i++)
                    {
                        var index = shapesBuilder.IndexOf(shapes[i]);
                        shapesBuilder[index] = paths[i];
                    }

                    var previous = layer.Shapes;
                    var next = shapesBuilder.ToImmutable();
                    Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                    layer.Shapes = next;

                    Select(layer, new HashSet<BaseShapeViewModel>(paths));
                }
            }
        }

        public void OnPathSimplify()
        {
            if (PathConverter is null)
            {
                return;
            }

            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is null)
            {
                return;
            }

            var sources = Project?.SelectedShapes;
            var source = Project?.SelectedShapes?.FirstOrDefault();

            if (sources is { } && sources.Count == 1)
            {
                var path = PathConverter.Simplify(source);
                if (path is { })
                {
                    var shapesBuilder = layer.Shapes.ToBuilder();

                    var index = shapesBuilder.IndexOf(source);
                    shapesBuilder[index] = path;

                    var previous = layer.Shapes;
                    var next = shapesBuilder.ToImmutable();
                    Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                    layer.Shapes = next;

                    Select(layer, path);
                }
            }

            if (sources is { } && sources.Count > 1)
            {
                var paths = new List<PathShapeViewModel>();
                var shapes = new List<BaseShapeViewModel>();

                foreach (var s in sources)
                {
                    var path = PathConverter.Simplify(s);
                    if (path is { })
                    {
                        paths.Add(path);
                        shapes.Add(s);
                    }
                }

                if (paths.Count > 0)
                {
                    var shapesBuilder = layer.Shapes.ToBuilder();

                    for (int i = 0; i < paths.Count; i++)
                    {
                        var index = shapesBuilder.IndexOf(shapes[i]);
                        shapesBuilder[index] = paths[i];
                    }

                    var previous = layer.Shapes;
                    var next = shapesBuilder.ToImmutable();
                    Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                    layer.Shapes = next;

                    Select(layer, new HashSet<BaseShapeViewModel>(paths));
                }
            }
        }

        public void OnPathBreak()
        {
            if (PathConverter is null)
            {
                return;
            }

            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is null)
            {
                return;
            }

            var sources = Project?.SelectedShapes;

            if (sources is { } && sources.Count >= 1)
            {
                var result = new List<BaseShapeViewModel>();
                var remove = new List<BaseShapeViewModel>();

                foreach (var s in sources)
                {
                    _shapeEditor.BreakShape(s, result, remove);
                }

                if (result.Count > 0)
                {
                    var shapesBuilder = layer.Shapes.ToBuilder();

                    for (int i = 0; i < remove.Count; i++)
                    {
                        shapesBuilder.Remove(remove[i]);
                    }

                    for (int i = 0; i < result.Count; i++)
                    {
                        shapesBuilder.Add(result[i]);
                    }

                    var previous = layer.Shapes;
                    var next = shapesBuilder.ToImmutable();
                    Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                    layer.Shapes = next;

                    Select(layer, new HashSet<BaseShapeViewModel>(result));
                }
            }
        }

        public void OnPathOp(string op)
        {
            if (!Enum.TryParse<PathOp>(op, true, out var pathOp))
            {
                return;
            }

            if (PathConverter is null)
            {
                return;
            }

            var sources = Project?.SelectedShapes;
            if (sources is null)
            {
                return;
            }

            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is null)
            {
                return;
            }

            var path = PathConverter.Op(sources, pathOp);
            if (path is null)
            {
                return;
            }

            var shapesBuilder = layer.Shapes.ToBuilder();
            foreach (var shape in sources)
            {
                shapesBuilder.Remove(shape);
            }
            shapesBuilder.Add(path);

            var previous = layer.Shapes;
            var next = shapesBuilder.ToImmutable();
            Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
            layer.Shapes = next;

            Select(layer, path);
        }

        public void OnToolNone()
        {
            CurrentTool?.Reset();
            CurrentTool = Tools.FirstOrDefault(t => t.Title == "None");
        }

        public void OnToolSelection()
        {
            CurrentTool?.Reset();
            CurrentTool = Tools.FirstOrDefault(t => t.Title == "Selection");
        }

        public void OnToolPoint()
        {
            CurrentTool?.Reset();
            CurrentTool = Tools.FirstOrDefault(t => t.Title == "Point");
        }

        public void OnToolLine()
        {
            if (CurrentTool.Title == "Path" && CurrentPathTool.Title != "Line")
            {
                CurrentPathTool?.Reset();
                CurrentPathTool = PathTools.FirstOrDefault(t => t.Title == "Line");
            }
            else
            {
                CurrentTool?.Reset();
                CurrentTool = Tools.FirstOrDefault(t => t.Title == "Line");
            }
        }

        public void OnToolArc()
        {
            if (CurrentTool.Title == "Path" && CurrentPathTool.Title != "Arc")
            {
                CurrentPathTool?.Reset();
                CurrentPathTool = PathTools.FirstOrDefault(t => t.Title == "Arc");
            }
            else
            {
                CurrentTool?.Reset();
                CurrentTool = Tools.FirstOrDefault(t => t.Title == "Arc");
            }
        }

        public void OnToolCubicBezier()
        {
            if (CurrentTool.Title == "Path" && CurrentPathTool.Title != "CubicBezier")
            {
                CurrentPathTool?.Reset();
                CurrentPathTool = PathTools.FirstOrDefault(t => t.Title == "CubicBezier");
            }
            else
            {
                CurrentTool?.Reset();
                CurrentTool = Tools.FirstOrDefault(t => t.Title == "CubicBezier");
            }
        }

        public void OnToolQuadraticBezier()
        {
            if (CurrentTool.Title == "Path" && CurrentPathTool.Title != "QuadraticBezier")
            {
                CurrentPathTool?.Reset();
                CurrentPathTool = PathTools.FirstOrDefault(t => t.Title == "QuadraticBezier");
            }
            else
            {
                CurrentTool?.Reset();
                CurrentTool = Tools.FirstOrDefault(t => t.Title == "QuadraticBezier");
            }
        }

        public void OnToolPath()
        {
            CurrentTool?.Reset();
            CurrentTool = Tools.FirstOrDefault(t => t.Title == "Path");
        }

        public void OnToolRectangle()
        {
            CurrentTool?.Reset();
            CurrentTool = Tools.FirstOrDefault(t => t.Title == "Rectangle");
        }

        public void OnToolEllipse()
        {
            CurrentTool?.Reset();
            CurrentTool = Tools.FirstOrDefault(t => t.Title == "Ellipse");
        }

        public void OnToolText()
        {
            CurrentTool?.Reset();
            CurrentTool = Tools.FirstOrDefault(t => t.Title == "Text");
        }

        public void OnToolImage()
        {
            CurrentTool?.Reset();
            CurrentTool = Tools.FirstOrDefault(t => t.Title == "Image");
        }

        public void OnToolMove()
        {
            if (CurrentTool.Title == "Path" && CurrentPathTool.Title != "Move")
            {
                CurrentPathTool?.Reset();
                CurrentPathTool = PathTools.FirstOrDefault(t => t.Title == "Move");
            }
        }

        public void OnResetTool()
        {
            CurrentTool?.Reset();
        }

        public void OnToggleDefaultIsStroked()
        {
            if (Project?.Options is { })
            {
                Project.Options.DefaultIsStroked = !Project.Options.DefaultIsStroked;
            }
        }

        public void OnToggleDefaultIsFilled()
        {
            if (Project?.Options is { })
            {
                Project.Options.DefaultIsFilled = !Project.Options.DefaultIsFilled;
            }
        }

        public void OnToggleDefaultIsClosed()
        {
            if (Project?.Options is { })
            {
                Project.Options.DefaultIsClosed = !Project.Options.DefaultIsClosed;
            }
        }

        public void OnToggleSnapToGrid()
        {
            if (Project?.Options is { })
            {
                Project.Options.SnapToGrid = !Project.Options.SnapToGrid;
            }
        }

        public void OnToggleTryToConnect()
        {
            if (Project?.Options is { })
            {
                Project.Options.TryToConnect = !Project.Options.TryToConnect;
            }
        }

        public void OnAddDatabase()
        {
            var db = ViewModelFactory.CreateDatabase(ProjectEditorConfiguration.DefaultDatabaseName);
            Project.AddDatabase(db);
            Project.SetCurrentDatabase(db);
        }

        public void OnRemoveDatabase(DatabaseViewModel db)
        {
            Project.RemoveDatabase(db);
            Project.SetCurrentDatabase(Project.Databases.FirstOrDefault());
        }

        public void OnAddColumn(DatabaseViewModel db)
        {
            Project.AddColumn(db, ViewModelFactory.CreateColumn(db, ProjectEditorConfiguration.DefaulColumnName));
        }

        public void OnRemoveColumn(ColumnViewModel column)
        {
            Project.RemoveColumn(column);
        }

        public void OnAddRecord(DatabaseViewModel db)
        {
            Project.AddRecord(db, ViewModelFactory.CreateRecord(db, ProjectEditorConfiguration.DefaulValue));
        }

        public void OnRemoveRecord(RecordViewModel record)
        {
            Project.RemoveRecord(record);
        }

        public void OnResetRecord(IDataObject data)
        {
            Project.ResetRecord(data);
        }

        public void OnApplyRecord(RecordViewModel record)
        {
            if (record is { })
            {
                if (Project?.SelectedShapes?.Count > 0)
                {
                    foreach (var shape in Project.SelectedShapes)
                    {
                        Project.ApplyRecord(shape, record);
                    }
                }

                if (Project.SelectedShapes is null)
                {
                    var container = Project?.CurrentContainer;
                    if (container is { })
                    {
                        Project?.ApplyRecord(container, record);
                    }
                }
            }
        }

        public void OnAddProperty(ViewModelBase owner)
        {
            if (owner is IDataObject data)
            {
                Project.AddProperty(data, ViewModelFactory.CreateProperty(owner, ProjectEditorConfiguration.DefaulPropertyName, ProjectEditorConfiguration.DefaulValue));
            }
        }

        public void OnRemoveProperty(PropertyViewModel property)
        {
            Project.RemoveProperty(property);
        }

        public void OnAddGroupLibrary()
        {
            var gl = ViewModelFactory.CreateLibrary<GroupShapeViewModel>(ProjectEditorConfiguration.DefaulGroupLibraryName);
            Project.AddGroupLibrary(gl);
            Project.SetCurrentGroupLibrary(gl);
        }

        public void OnRemoveGroupLibrary(LibraryViewModel<GroupShapeViewModel> libraryViewModel)
        {
            Project.RemoveGroupLibrary(libraryViewModel);
            Project.SetCurrentGroupLibrary(Project?.GroupLibraries.FirstOrDefault());
        }

        public void OnAddGroup(LibraryViewModel<GroupShapeViewModel> libraryViewModel)
        {
            if (Project is { } && libraryViewModel is { })
            {
                if (Project.SelectedShapes?.Count == 1 && Project.SelectedShapes?.FirstOrDefault() is GroupShapeViewModel group)
                {
                    var clone = CloneShape(group);
                    if (clone is { })
                    {
                        Project?.AddGroup(libraryViewModel, clone);
                    }
                }
            }
        }

        public void OnRemoveGroup(GroupShapeViewModel group)
        {
            if (Project is { } && group is { })
            {
                var library = Project.RemoveGroup(group);
                library?.SetSelected(library?.Items.FirstOrDefault());
            }
        }

        public void OnInsertGroup(GroupShapeViewModel group)
        {
            if (Project?.CurrentContainer is { })
            {
                OnDropShapeAsClone(group, 0.0, 0.0);
            }
        }

        public void OnAddLayer(FrameContainerViewModel container)
        {
            if (container is { })
            {
                Project.AddLayer(container, ViewModelFactory.CreateLayerContainer(ProjectEditorConfiguration.DefaultLayerName, container));
            }
        }

        public void OnRemoveLayer(LayerContainerViewModel layer)
        {
            if (layer is { })
            {
                Project.RemoveLayer(layer);
                if (layer.Owner is FrameContainerViewModel owner)
                {
                    owner.SetCurrentLayer(owner.Layers.FirstOrDefault());
                }
            }
        }

        public void OnAddStyleLibrary()
        {
            var sl = ViewModelFactory.CreateLibrary<ShapeStyleViewModel>(ProjectEditorConfiguration.DefaulStyleLibraryName);
            Project.AddStyleLibrary(sl);
            Project.SetCurrentStyleLibrary(sl);
        }

        public void OnRemoveStyleLibrary(LibraryViewModel<ShapeStyleViewModel> libraryViewModel)
        {
            Project.RemoveStyleLibrary(libraryViewModel);
            Project.SetCurrentStyleLibrary(Project?.StyleLibraries.FirstOrDefault());
        }

        public void OnAddStyle(LibraryViewModel<ShapeStyleViewModel> libraryViewModel)
        {
            if (Project?.SelectedShapes is { })
            {
                foreach (var shape in Project.SelectedShapes)
                {
                    if (shape.Style is { })
                    {
                        var style = (ShapeStyleViewModel)shape.Style.Copy(null);
                        Project.AddStyle(libraryViewModel, style);
                    }
                }
            }
            else
            {
                var style = ViewModelFactory.CreateShapeStyle(ProjectEditorConfiguration.DefaulStyleName);
                Project.AddStyle(libraryViewModel, style);
            }
        }

        public void OnRemoveStyle(ShapeStyleViewModel style)
        {
            var library = Project.RemoveStyle(style);
            library?.SetSelected(library?.Items.FirstOrDefault());
        }

        public void OnApplyStyle(ShapeStyleViewModel style)
        {
            if (style is { })
            {
                if (Project?.SelectedShapes?.Count > 0)
                {
                    foreach (var shape in Project.SelectedShapes)
                    {
                        Project?.ApplyStyle(shape, style);
                    }
                }
            }
        }

        public void OnAddShape(BaseShapeViewModel shape)
        {
            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is { } && shape is { })
            {
                Project.AddShape(layer, shape);
            }
        }

        public void OnRemoveShape(BaseShapeViewModel shape)
        {
            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is { } && shape is { })
            {
                Project.RemoveShape(layer, shape);
                Project.CurrentContainer.CurrentShape = layer.Shapes.FirstOrDefault();
            }
        }

        public void OnAddTemplate()
        {
            if (Project is { })
            {
                var template = ContainerFactory.GetTemplate(Project, "Empty");
                if (template is null)
                {
                    template = ViewModelFactory.CreateTemplateContainer(ProjectEditorConfiguration.DefaultTemplateName);
                }

                Project.AddTemplate(template);
            }
        }

        public void OnRemoveTemplate(TemplateContainerViewModel template)
        {
            if (template is { })
            {
                Project?.RemoveTemplate(template);
                Project?.SetCurrentTemplate(Project?.Templates.FirstOrDefault());
            }
        }

        public void OnEditTemplate(FrameContainerViewModel template)
        {
            if (Project is { } && template is { })
            {
                Project.SetCurrentContainer(template);
                Project.CurrentContainer?.InvalidateLayer();
            }
        }

        public void OnAddScript()
        {
            if (Project is { })
            {
                var script = ViewModelFactory.CreateScript(ProjectEditorConfiguration.DefaultScriptName);
                Project.AddScript(script);
            }
        }

        public void OnRemoveScript(ScriptViewModel script)
        {
            if (script is { })
            {
                Project?.RemoveScript(script);
                Project?.SetCurrentScript(Project?.Scripts.FirstOrDefault());
            }
        }

        public void OnApplyTemplate(TemplateContainerViewModel template)
        {
            var container = Project?.CurrentContainer;
            if (container is PageContainerViewModel page)
            {
                Project.ApplyTemplate(page, template);
                Project.CurrentContainer.InvalidateLayer();
            }
        }

        public string OnGetImageKey(string path)
        {
            using var stream = FileSystem.Open(path);
            var bytes = FileSystem.ReadBinary(stream);
            if (Project is IImageCache imageCache)
            {
                var key = imageCache.AddImageFromFile(path, bytes);
                return key;
            }
            return default;
        }

        public async Task<string> OnAddImageKey(string path)
        {
            if (Project is { })
            {
                if (path is null || string.IsNullOrEmpty(path))
                {
                    var key = await (ImageImporter.GetImageKeyAsync() ?? Task.FromResult(string.Empty));
                    if (key is null || string.IsNullOrEmpty(key))
                    {
                        return null;
                    }

                    return key;
                }
                else
                {
                    byte[] bytes;
                    using (var stream = FileSystem?.Open(path))
                    {
                        bytes = FileSystem?.ReadBinary(stream);
                    }
                    if (Project is IImageCache imageCache)
                    {
                        var key = imageCache.AddImageFromFile(path, bytes);
                        return key;
                    }
                    return null;
                }
            }

            return null;
        }

        public void OnRemoveImageKey(string key)
        {
            if (key is { })
            {
                if (Project is IImageCache imageCache)
                {
                    imageCache.RemoveImage(key);
                }
            }
        }

        public void OnAddPage(object item)
        {
            if (Project?.CurrentDocument is { })
            {
                var page =
                    ContainerFactory?.GetPage(Project, ProjectEditorConfiguration.DefaultPageName)
                    ?? ViewModelFactory.CreatePageContainer(ProjectEditorConfiguration.DefaultPageName);

                Project.AddPage(Project.CurrentDocument, page);
                Project.SetCurrentContainer(page);
            }
        }

        public void OnInsertPageBefore(object item)
        {
            if (Project?.CurrentDocument is { })
            {
                if (item is PageContainerViewModel selected)
                {
                    int index = Project.CurrentDocument.Pages.IndexOf(selected);
                    var page =
                        ContainerFactory?.GetPage(Project, ProjectEditorConfiguration.DefaultPageName)
                        ?? ViewModelFactory.CreatePageContainer(ProjectEditorConfiguration.DefaultPageName);

                    Project.AddPageAt(Project.CurrentDocument, page, index);
                    Project.SetCurrentContainer(page);
                }
            }
        }

        public void OnInsertPageAfter(object item)
        {
            if (Project?.CurrentDocument is { })
            {
                if (item is PageContainerViewModel selected)
                {
                    int index = Project.CurrentDocument.Pages.IndexOf(selected);
                    var page =
                        ContainerFactory?.GetPage(Project, ProjectEditorConfiguration.DefaultPageName)
                        ?? ViewModelFactory.CreatePageContainer(ProjectEditorConfiguration.DefaultPageName);

                    Project.AddPageAt(Project.CurrentDocument, page, index + 1);
                    Project.SetCurrentContainer(page);
                }
            }
        }

        public void OnAddDocument(object item)
        {
            if (Project is { })
            {
                var document =
                    ContainerFactory?.GetDocument(Project, ProjectEditorConfiguration.DefaultDocumentName)
                    ?? ViewModelFactory.CreateDocumentContainer(ProjectEditorConfiguration.DefaultDocumentName);

                Project.AddDocument(document);
                Project.SetCurrentDocument(document);
                Project.SetCurrentContainer(document?.Pages.FirstOrDefault());
            }
        }

        public void OnInsertDocumentBefore(object item)
        {
            if (Project is { })
            {
                if (item is DocumentContainerViewModel selected)
                {
                    int index = Project.Documents.IndexOf(selected);
                    var document =
                        ContainerFactory?.GetDocument(Project, ProjectEditorConfiguration.DefaultDocumentName)
                        ?? ViewModelFactory.CreateDocumentContainer(ProjectEditorConfiguration.DefaultDocumentName);

                    Project.AddDocumentAt(document, index);
                    Project.SetCurrentDocument(document);
                    Project.SetCurrentContainer(document?.Pages.FirstOrDefault());
                }
            }
        }

        public void OnInsertDocumentAfter(object item)
        {
            if (Project is { })
            {
                if (item is DocumentContainerViewModel selected)
                {
                    int index = Project.Documents.IndexOf(selected);
                    var document =
                        ContainerFactory?.GetDocument(Project, ProjectEditorConfiguration.DefaultDocumentName)
                        ?? ViewModelFactory.CreateDocumentContainer(ProjectEditorConfiguration.DefaultDocumentName);

                    Project.AddDocumentAt(document, index + 1);
                    Project.SetCurrentDocument(document);
                    Project.SetCurrentContainer(document?.Pages.FirstOrDefault());
                }
            }
        }

        private void SetRenderersImageCache(IImageCache cache)
        {
            if (Renderer is { })
            {
                Renderer.ClearCache();
                Renderer.State.ImageCache = cache;
            }
        }

        public void OnLoad(ProjectContainerViewModel project, string path = null)
        {
            if (project is { })
            {
                Deselect();
                if (project is IImageCache imageCache)
                {
                    SetRenderersImageCache(imageCache);
                }
                Project = project;
                Project.History = new StackHistory();
                ProjectPath = path;
                IsProjectDirty = false;

                var propertyChangedSubject = new Subject<(object sender, PropertyChangedEventArgs e)>();
                var propertyChangedDisposable = Project.Subscribe(propertyChangedSubject);
                var observable = propertyChangedSubject.Subscribe(ProjectChanged);

                Observer = new CompositeDisposable(propertyChangedDisposable, observable, propertyChangedSubject);

                void ProjectChanged((object sender, PropertyChangedEventArgs e) arg)
                {
                    // Debug.WriteLine($"[Changed] {arg.sender}.{arg.e.PropertyName}");
                    // _project?.CurrentContainer?.InvalidateLayer();
                    CanvasPlatform?.InvalidateControl?.Invoke();
                    IsProjectDirty = true;
                }
            }
        }

        public void OnUnload()
        {
            if (Observer is { })
            {
                Observer?.Dispose();
                Observer = null;
            }

            if (Project?.History is { })
            {
                Project.History.Reset();
                Project.History = null;
            }

            if (Project is { })
            {
                if (Project is IImageCache imageCache)
                {
                    imageCache.PurgeUnusedImages(new HashSet<string>());
                }
                Deselect();
                SetRenderersImageCache(null);
                Project = null;
                ProjectPath = string.Empty;
                IsProjectDirty = false;
                GC.Collect();
            }
        }

        public void OnInvalidateCache()
        {
            try
            {
                Renderer?.ClearCache();
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnAddRecent(string path, string name)
        {
            if (_recentProjects is { })
            {
                var q = _recentProjects.Where(x => x.Path.ToLower() == path.ToLower()).ToList();
                var builder = _recentProjects.ToBuilder();

                if (q.Count() > 0)
                {
                    foreach (var r in q)
                    {
                        builder.Remove(r);
                    }
                }

                builder.Insert(0, RecentFileViewModel.Create(_serviceProvider, name, path));

                RecentProjects = builder.ToImmutable();
                CurrentRecentProject = _recentProjects.FirstOrDefault();
            }
        }

        public void OnLoadRecent(string path)
        {
            if (JsonSerializer is { })
            {
                try
                {
                    var json = FileSystem.ReadUtf8Text(path);
                    var recent = JsonSerializer.Deserialize<RecentsViewModel>(json);
                    if (recent is { })
                    {
                        var remove = recent.Files.Where(x => FileSystem?.Exists(x.Path) == false).ToList();
                        var builder = recent.Files.ToBuilder();

                        foreach (var file in remove)
                        {
                            builder.Remove(file);
                        }

                        RecentProjects = builder.ToImmutable();

                        if (recent.Current is { }
                            && (FileSystem?.Exists(recent.Current.Path) ?? false))
                        {
                            CurrentRecentProject = recent.Current;
                        }
                        else
                        {
                            CurrentRecentProject = _recentProjects.FirstOrDefault();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogException(ex);
                }
            }
        }

        public void OnSaveRecent(string path)
        {
            if (JsonSerializer is { })
            {
                try
                {
                    var recent = RecentsViewModel.Create(_serviceProvider, _recentProjects, _currentRecentProject);
                    var json = JsonSerializer.Serialize(recent);
                    FileSystem.WriteUtf8Text(path, json);
                }
                catch (Exception ex)
                {
                    Log?.LogException(ex);
                }
            }
        }

        public bool CanUndo()
        {
            return Project?.History?.CanUndo() ?? false;
        }

        public bool CanRedo()
        {
            return Project?.History?.CanRedo() ?? false;
        }

        public bool CanCopy()
        {
            return Project?.SelectedShapes is { };
        }

        public async Task<bool> CanPaste()
        {
            try
            {
                return await (TextClipboard?.ContainsText() ?? Task.FromResult(false));
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
            return false;
        }

        public void OnCopyShapes(IList<BaseShapeViewModel> shapes)
        {
            try
            {
                var json = JsonSerializer?.Serialize(shapes);
                if (!string.IsNullOrEmpty(json))
                {
                    TextClipboard?.SetText(json);
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnTryPaste(string text)
        {
            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    var pathShape = PathConverter?.FromSvgPathData(text, isStroked: false, isFilled: true);
                    if (pathShape is { })
                    {
                        OnPasteShapes(Enumerable.Repeat<BaseShapeViewModel>(pathShape, 1));
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }

            try
            {
                var shapes = JsonSerializer?.Deserialize<IList<BaseShapeViewModel>>(text);
                if (shapes?.Count() > 0)
                {
                    OnPasteShapes(shapes);
                    return;
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        private IDictionary<string, RecordViewModel> GenerateRecordDictionaryById()
        {
            return Project?.Databases
                .Where(d => d?.Records is { } && d?.Records.Length > 0)
                .SelectMany(d => d.Records)
                .ToDictionary(s => s.Id);
        }

        private void TryToRestoreRecords(IEnumerable<BaseShapeViewModel> shapes)
        {
            try
            {
                if (Project?.Databases is null)
                {
                    return;
                }

                var records = GenerateRecordDictionaryById();

                // Try to restore shape record.
                foreach (var shape in shapes.GetAllShapes())
                {
                    if (shape?.Record is null)
                    {
                        continue;
                    }

                    if (records.TryGetValue(shape.Record.Id, out var record))
                    {
                        // Use existing record.
                        shape.Record = record;
                    }
                    else
                    {
                        // Create Imported database.
                        if (Project?.CurrentDatabase is null && shape.Record.Owner is DatabaseViewModel owner)
                        {
                            var db = ViewModelFactory.CreateDatabase(
                                ProjectEditorConfiguration.ImportedDatabaseName,
                                owner.Columns);
                            Project.AddDatabase(db);
                            Project.SetCurrentDatabase(db);
                        }

                        // Add missing data record.
                        shape.Record.Owner = Project.CurrentDatabase;
                        Project?.AddRecord(Project?.CurrentDatabase, shape.Record);

                        // Recreate records dictionary.
                        records = GenerateRecordDictionaryById();
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        private void RestoreShape(BaseShapeViewModel shape)
        {
            var shapes = Enumerable.Repeat(shape, 1).ToList();
            TryToRestoreRecords(shapes);
        }

        private void UpdateShapeNames(IEnumerable<BaseShapeViewModel> shapes)
        {
            var all = _project.GetAllShapes().ToList();
            var source = new List<BaseShapeViewModel>();

            foreach (var shape in shapes)
            {
                SetShapeName(shape, all.Concat(source));
                source.Add(shape);
            }
        }

        public void OnPasteShapes(IEnumerable<BaseShapeViewModel> shapes)
        {
            try
            {
                Deselect(Project?.CurrentContainer?.CurrentLayer);
                TryToRestoreRecords(shapes);
                UpdateShapeNames(shapes);
                Project.AddShapes(Project?.CurrentContainer?.CurrentLayer, shapes);
                OnSelect(shapes);
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public void OnSelect(IEnumerable<BaseShapeViewModel> shapes)
        {
            if (shapes?.Count() == 1)
            {
                Select(Project?.CurrentContainer?.CurrentLayer, shapes.FirstOrDefault());
            }
            else
            {
                Select(Project?.CurrentContainer?.CurrentLayer, new HashSet<BaseShapeViewModel>(shapes));
            }
        }

        public T CloneShape<T>(T shape) where T : BaseShapeViewModel
        {
            try
            {
                if (JsonSerializer is IJsonSerializer serializer)
                {
                    var json = serializer.Serialize(shape);
                    if (!string.IsNullOrEmpty(json))
                    {
                        var clone = serializer.Deserialize<T>(json);
                        if (clone is { })
                        {
                            RestoreShape(clone);
                            return clone;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }

            return default;
        }

        public LayerContainerViewModel Clone(LayerContainerViewModel container)
        {
            try
            {
                var json = JsonSerializer?.Serialize(container);
                if (!string.IsNullOrEmpty(json))
                {
                    var clone = JsonSerializer?.Deserialize<LayerContainerViewModel>(json);
                    if (clone is { })
                    {
                        var shapes = clone.Shapes;
                        TryToRestoreRecords(shapes);
                        return clone;
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }

            return default;
        }

        public PageContainerViewModel Clone(PageContainerViewModel container)
        {
            try
            {
                var template = container?.Template;
                var json = JsonSerializer?.Serialize(container);
                if (!string.IsNullOrEmpty(json))
                {
                    var clone = JsonSerializer?.Deserialize<PageContainerViewModel>(json);
                    if (clone is { })
                    {
                        var shapes = clone.Layers.SelectMany(l => l.Shapes);
                        TryToRestoreRecords(shapes);
                        clone.Template = template;
                        return clone;
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }

            return default;
        }

        public DocumentContainerViewModel Clone(DocumentContainerViewModel document)
        {
            try
            {
                var templates = document?.Pages.Select(c => c?.Template)?.ToArray();
                var json = JsonSerializer?.Serialize(document);
                if (!string.IsNullOrEmpty(json))
                {
                    var clone = JsonSerializer?.Deserialize<DocumentContainerViewModel>(json);
                    if (clone is { })
                    {
                        for (int i = 0; i < clone.Pages.Length; i++)
                        {
                            var container = clone.Pages[i];
                            var shapes = container.Layers.SelectMany(l => l.Shapes);
                            TryToRestoreRecords(shapes);
                            container.Template = templates[i];
                        }
                        return clone;
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }

            return default;
        }

        public async Task<bool> OnDropFiles(string[] files, double x, double y)
        {
            try
            {
                if (files?.Length < 1)
                {
                    return false;
                }

                bool result = false;

                foreach (var path in files)
                {
                    if (string.IsNullOrEmpty(path))
                    {
                        continue;
                    }

                    string ext = System.IO.Path.GetExtension(path);

                    if (string.Compare(ext, ProjectEditorConfiguration.ProjectExtension, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        OnOpenProject(path);
                        result = true;
                    }
                    else if (string.Compare(ext, ProjectEditorConfiguration.CsvExtension, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        var reader = TextFieldReaders.FirstOrDefault(x => x.Extension == "csv");
                        if (reader is { })
                        {
                            OnImportData(Project, path, reader);
                            result = true;
                        }
                    }
                    else if (string.Compare(ext, ProjectEditorConfiguration.XlsxExtension, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        var reader = TextFieldReaders.FirstOrDefault(x => x.Extension == "xlsx");
                        if (reader is { })
                        {
                            OnImportData(Project, path, reader);
                            result = true;
                        }
                    }
                    else if (string.Compare(ext, ProjectEditorConfiguration.JsonExtension, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        OnImportJson(path);
                        result = true;
                    }
                    else if (string.Compare(ext, ProjectEditorConfiguration.ScriptExtension, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        await OnExecuteScriptFile(path);
                        result = true;
                    }
                    else if (string.Compare(ext, ProjectEditorConfiguration.SvgExtension, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        OnImportSvg(path);
                        result = true;
                    }
                    else if (ProjectEditorConfiguration.ImageExtensions.Any(x => string.Compare(ext, x, StringComparison.OrdinalIgnoreCase) == 0))
                    {
                        var key = OnGetImageKey(path);
                        if (key is { } && !string.IsNullOrEmpty(key))
                        {
                            OnDropImageKey(key, x, y);
                        }
                        result = true;
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }

            return false;
        }

        public void OnDropImageKey(string key, double x, double y)
        {
            var selected = Project.CurrentStyleLibrary?.Selected is { } ?
                Project.CurrentStyleLibrary.Selected :
                ViewModelFactory.CreateShapeStyle(ProjectEditorConfiguration.DefaulStyleName);
            var style = (ShapeStyleViewModel)selected.Copy(null);
            var layer = Project?.CurrentContainer?.CurrentLayer;
            decimal sx = Project.Options.SnapToGrid ? PointUtil.Snap((decimal)x, (decimal)Project.Options.SnapX) : (decimal)x;
            decimal sy = Project.Options.SnapToGrid ? PointUtil.Snap((decimal)y, (decimal)Project.Options.SnapY) : (decimal)y;

            var image = ViewModelFactory.CreateImageShape((double)sx, (double)sy, style, key);
            image.BottomRight.X = (double)(sx + 320m);
            image.BottomRight.Y = (double)(sy + 180m);

            Project.AddShape(layer, image);
        }

        public bool OnDropShape(BaseShapeViewModel shape, double x, double y, bool bExecute = true)
        {
            try
            {
                var layer = Project?.CurrentContainer?.CurrentLayer;
                if (layer is { })
                {
                    if (bExecute == true)
                    {
                        OnDropShapeAsClone(shape, x, y);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
            return false;
        }

        public void OnDropShapeAsClone<T>(T shape, double x, double y) where T : BaseShapeViewModel
        {
            decimal sx = Project.Options.SnapToGrid ? PointUtil.Snap((decimal)x, (decimal)Project.Options.SnapX) : (decimal)x;
            decimal sy = Project.Options.SnapToGrid ? PointUtil.Snap((decimal)y, (decimal)Project.Options.SnapY) : (decimal)y;

            try
            {
                var clone = CloneShape(shape);
                if (clone is { })
                {
                    Deselect(Project?.CurrentContainer?.CurrentLayer);
                    clone.Move(null, sx, sy);

                    Project.AddShape(Project?.CurrentContainer?.CurrentLayer, clone);

                    // Select(Project?.CurrentContainer?.CurrentLayer, clone);

                    if (Project.Options.TryToConnect)
                    {
                        if (clone is GroupShapeViewModel group)
                        {
                            var shapes = Project?.CurrentContainer?.CurrentLayer?.Shapes.GetAllShapes<LineShapeViewModel>();
                            TryToConnectLines(shapes, group.Connectors);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
        }

        public bool OnDropRecord(RecordViewModel record, double x, double y, bool bExecute = true)
        {
            try
            {
                if (Project?.SelectedShapes?.Count > 0)
                {
                    if (bExecute)
                    {
                        OnApplyRecord(record);
                    }
                    return true;
                }
                else
                {
                    var layer = Project?.CurrentContainer?.CurrentLayer;
                    if (layer is { })
                    {
                        var shapes = layer.Shapes.Reverse();
                        double radius = Project.Options.HitThreshold / PageState.ZoomX;
                        var result = HitTest.TryToGetShape(shapes, new Point2(x, y), radius, PageState.ZoomX);
                        if (result is { })
                        {
                            if (bExecute)
                            {
                                Project?.ApplyRecord(result, record);
                            }
                            return true;
                        }
                        else
                        {
                            if (bExecute)
                            {
                                OnDropRecordAsGroup(record, x, y);
                            }
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
            return false;
        }

        public void OnDropRecordAsGroup(RecordViewModel record, double x, double y)
        {
            var selected = Project.CurrentStyleLibrary?.Selected is { } ?
                Project.CurrentStyleLibrary.Selected :
                ViewModelFactory.CreateShapeStyle(ProjectEditorConfiguration.DefaulStyleName);
            var style = (ShapeStyleViewModel)selected.Copy(null);
            var layer = Project?.CurrentContainer?.CurrentLayer;
            decimal sx = Project.Options.SnapToGrid ? PointUtil.Snap((decimal)x, (decimal)Project.Options.SnapX) : (decimal)x;
            decimal sy = Project.Options.SnapToGrid ? PointUtil.Snap((decimal)y, (decimal)Project.Options.SnapY) : (decimal)y;

            var g = ViewModelFactory.CreateGroupShape(ProjectEditorConfiguration.DefaulGroupName);

            g.Record = record;

            var length = record.Values.Length;
            double px = (double)sx;
            double py = (double)sy;
            double width = 150;
            double height = 15;

            var db = record.Owner as DatabaseViewModel;

            for (int i = 0; i < length; i++)
            {
                var column = db.Columns[i];
                if (column.IsVisible)
                {
                    var binding = "{" + db.Columns[i].Name + "}";
                    var text = ViewModelFactory.CreateTextShape(px, py, px + width, py + height, style, binding);
                    g.AddShape(text);
                    py += height;
                }
            }

            var rectangle = ViewModelFactory.CreateRectangleShape((double)sx, (double)sy, (double)sx + width, (double)sy + (length * height), style);
            g.AddShape(rectangle);

            var pt = ViewModelFactory.CreatePointShape((double)sx + (width / 2), (double)sy);
            var pb = ViewModelFactory.CreatePointShape((double)sx + (width / 2), (double)sy + (length * height));
            var pl = ViewModelFactory.CreatePointShape((double)sx, (double)sy + ((length * height) / 2));
            var pr = ViewModelFactory.CreatePointShape((double)sx + width, (double)sy + ((length * height) / 2));

            g.AddConnectorAsNone(pt);
            g.AddConnectorAsNone(pb);
            g.AddConnectorAsNone(pl);
            g.AddConnectorAsNone(pr);

            Project.AddShape(layer, g);
        }

        public bool OnDropStyle(ShapeStyleViewModel style, double x, double y, bool bExecute = true)
        {
            try
            {
                if (Project?.SelectedShapes?.Count > 0)
                {
                    if (bExecute == true)
                    {
                        OnApplyStyle(style);
                    }
                    return true;
                }
                else
                {
                    var layer = Project.CurrentContainer?.CurrentLayer;
                    if (layer is { })
                    {
                        var shapes = layer.Shapes.Reverse();
                        double radius = Project.Options.HitThreshold / PageState.ZoomX;
                        var result = HitTest.TryToGetShape(shapes, new Point2(x, y), radius, PageState.ZoomX);
                        if (result is { })
                        {
                            if (bExecute == true)
                            {
                                Project.ApplyStyle(result, style);
                            }
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
            return false;
        }

        public bool OnDropTemplate(TemplateContainerViewModel template, double x, double y, bool bExecute = true)
        {
            try
            {
                var container = Project?.CurrentContainer;
                if (container is PageContainerViewModel page && template is { })
                {
                    if (bExecute)
                    {
                        OnApplyTemplate(template);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log?.LogException(ex);
            }
            return false;
        }

        public void OnDeleteSelected()
        {
            if (Project?.CurrentContainer?.CurrentLayer is null || PageState is null)
            {
                return;
            }

            if (Project.SelectedShapes?.Count > 0)
            {
                var layer = Project.CurrentContainer.CurrentLayer;

                var builder = layer.Shapes.ToBuilder();
                foreach (var shape in Project.SelectedShapes)
                {
                    builder.Remove(shape);
                }

                var previous = layer.Shapes;
                var next = builder.ToImmutable();
                Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                layer.Shapes = next;

                Project.SelectedShapes = default;
                layer.RaiseInvalidateLayer();

                OnHideDecorator();
            }
        }

        public void Deselect()
        {
            if (Project?.SelectedShapes is { })
            {
                Project.SelectedShapes = default;
            }

            OnHideDecorator();
        }

        public void Select(LayerContainerViewModel layer, BaseShapeViewModel shape)
        {
            if (PageState is { })
            {
                Project.SelectedShapes = new HashSet<BaseShapeViewModel>() { shape };

                if (PageState.DrawPoints == true)
                {
                    OnHideDecorator();
                }
                else
                {
                    if (shape is PointShapeViewModel || shape is LineShapeViewModel)
                    {
                        OnHideDecorator();
                    }
                    else
                    {
                        OnShowDecorator();
                    }
                }
            }

            if (layer.Owner is FrameContainerViewModel owner)
            {
                owner.CurrentShape = shape;
            }

            if (layer is { })
            {
                layer.RaiseInvalidateLayer();
            }
            else
            {
                CanvasPlatform?.InvalidateControl?.Invoke();
            }
        }

        public void Select(LayerContainerViewModel layer, ISet<BaseShapeViewModel> shapes)
        {
            if (PageState is { })
            {
                Project.SelectedShapes = shapes;

                OnShowDecorator();
            }

            if (layer.Owner is FrameContainerViewModel owner && owner.CurrentShape is { })
            {
                owner.CurrentShape = default;
            }

            if (layer is { })
            {
                layer.RaiseInvalidateLayer();
            }
            else
            {
                CanvasPlatform?.InvalidateControl?.Invoke();
            }
        }

        public void Deselect(LayerContainerViewModel layer)
        {
            Deselect();

            if (layer.Owner is FrameContainerViewModel owner && owner.CurrentShape is { })
            {
                owner.CurrentShape = default;
            }

            if (layer is { })
            {
                layer.RaiseInvalidateLayer();
            }
            else
            {
                CanvasPlatform?.InvalidateControl?.Invoke();
            }
        }

        public bool TryToSelectShape(LayerContainerViewModel layer, double x, double y, bool deselect = true)
        {
            if (layer is { })
            {
                var shapes = layer.Shapes.Reverse();
                double radius = Project.Options.HitThreshold / PageState.ZoomX;

                var point = HitTest.TryToGetPoint(shapes, new Point2(x, y), radius, PageState.ZoomX);
                if (point is { })
                {
                    Select(layer, point);
                    return true;
                }

                var shape = HitTest.TryToGetShape(shapes, new Point2(x, y), radius, PageState.ZoomX);
                if (shape is { })
                {
                    Select(layer, shape);
                    return true;
                }

                if (deselect)
                {
                    Deselect(layer);
                }
            }

            return false;
        }

        public bool TryToSelectShapes(LayerContainerViewModel layer, RectangleShapeViewModel rectangle, bool deselect = true, bool includeSelected = false)
        {
            if (layer is { })
            {
                var rect = Rect2.FromPoints(
                    rectangle.TopLeft.X,
                    rectangle.TopLeft.Y,
                    rectangle.BottomRight.X,
                    rectangle.BottomRight.Y);
                var shapes = layer.Shapes;
                double radius = Project.Options.HitThreshold / PageState.ZoomX;
                var result = HitTest.TryToGetShapes(shapes, rect, radius, PageState.ZoomX);
                if (result is { })
                {
                    if (result.Count > 0)
                    {
                        if (includeSelected)
                        {
                            if (Project?.SelectedShapes is { })
                            {
                                foreach (var shape in Project.SelectedShapes)
                                {
                                    if (result.Contains(shape))
                                    {
                                        result.Remove(shape);
                                    }
                                    else
                                    {
                                        result.Add(shape);
                                    }
                                }
                            }

                            if (result.Count > 0)
                            {
                                if (result.Count == 1)
                                {
                                    Select(layer, result.FirstOrDefault());
                                }
                                else
                                {
                                    Select(layer, result);
                                }
                                return true;
                            }
                        }
                        else
                        {
                            if (result.Count == 1)
                            {
                                Select(layer, result.FirstOrDefault());
                            }
                            else
                            {
                                Select(layer, result);
                            }
                            return true;
                        }
                    }
                }

                if (deselect)
                {
                    Deselect(layer);
                }
            }

            return false;
        }

        public void Hover(LayerContainerViewModel layer, BaseShapeViewModel shape)
        {
            if (layer is { })
            {
                Select(layer, shape);
                HoveredShapeViewModel = shape;
            }
        }

        public void Dehover(LayerContainerViewModel layer)
        {
            if (layer is { } && HoveredShapeViewModel is { })
            {
                HoveredShapeViewModel = default;
                Deselect(layer);
            }
        }

        public bool TryToHoverShape(double x, double y)
        {
            if (Project?.CurrentContainer?.CurrentLayer is null)
            {
                return false;
            }

            if (Project.SelectedShapes?.Count > 1)
            {
                return false;
            }

            if (!(Project.SelectedShapes?.Count == 1 && HoveredShapeViewModel != Project.SelectedShapes?.FirstOrDefault()))
            {
                var shapes = Project.CurrentContainer?.CurrentLayer?.Shapes.Reverse();

                double radius = Project.Options.HitThreshold / PageState.ZoomX;
                var point = HitTest.TryToGetPoint(shapes, new Point2(x, y), radius, PageState.ZoomX);
                if (point is { })
                {
                    Hover(Project.CurrentContainer?.CurrentLayer, point);
                    return true;
                }
                else
                {
                    var shape = HitTest.TryToGetShape(shapes, new Point2(x, y), radius, PageState.ZoomX);
                    if (shape is { })
                    {
                        Hover(Project.CurrentContainer?.CurrentLayer, shape);
                        return true;
                    }
                    else
                    {
                        if (Project.SelectedShapes?.Count == 1 && HoveredShapeViewModel == Project.SelectedShapes?.FirstOrDefault())
                        {
                            Dehover(Project.CurrentContainer?.CurrentLayer);
                        }
                    }
                }
            }

            return false;
        }

        public PointShapeViewModel TryToGetConnectionPoint(double x, double y)
        {
            if (Project.Options.TryToConnect)
            {
                var shapes = Project.CurrentContainer.CurrentLayer.Shapes.Reverse();
                double radius = Project.Options.HitThreshold / PageState.ZoomX;
                return HitTest.TryToGetPoint(shapes, new Point2(x, y), radius, PageState.ZoomX);
            }
            return null;
        }

        private void SwapLineStart(LineShapeViewModel line, PointShapeViewModel point)
        {
            if (line?.Start is { } && point is { })
            {
                var previous = line.Start;
                var next = point;
                Project?.History?.Snapshot(previous, next, (p) => line.Start = p);
                line.Start = next;
            }
        }

        private void SwapLineEnd(LineShapeViewModel line, PointShapeViewModel point)
        {
            if (line?.End is { } && point is { })
            {
                var previous = line.End;
                var next = point;
                Project?.History?.Snapshot(previous, next, (p) => line.End = p);
                line.End = next;
            }
        }

        public bool TryToSplitLine(double x, double y, PointShapeViewModel point, bool select = false)
        {
            if (Project?.CurrentContainer is null || Project?.Options is null)
            {
                return false;
            }

            var shapes = Project.CurrentContainer.CurrentLayer.Shapes.Reverse();
            double radius = Project.Options.HitThreshold / PageState.ZoomX;
            var result = HitTest.TryToGetShape(shapes, new Point2(x, y), radius, PageState.ZoomX);

            if (result is LineShapeViewModel line)
            {
                if (!Project.Options.SnapToGrid)
                {
                    var a = new Point2(line.Start.X, line.Start.Y);
                    var b = new Point2(line.End.X, line.End.Y);
                    var target = new Point2(x, y);
                    var nearest = target.NearestOnLine(a, b);
                    point.X = nearest.X;
                    point.Y = nearest.Y;
                }

                var split = ViewModelFactory.CreateLineShape(
                    x, y,
                    (ShapeStyleViewModel)line.Style.Copy(null),
                    line.IsStroked);

                double ds = point.DistanceTo(line.Start);
                double de = point.DistanceTo(line.End);

                if (ds < de)
                {
                    split.Start = line.Start;
                    split.End = point;
                    SwapLineStart(line, point);
                }
                else
                {
                    split.Start = point;
                    split.End = line.End;
                    SwapLineEnd(line, point);
                }

                Project.AddShape(Project.CurrentContainer.CurrentLayer, split);

                if (select)
                {
                    Select(Project.CurrentContainer.CurrentLayer, point);
                }

                return true;
            }

            return false;
        }

        public bool TryToSplitLine(LineShapeViewModel line, PointShapeViewModel p0, PointShapeViewModel p1)
        {
            if (Project?.Options is null)
            {
                return false;
            }

            // Points must be aligned horizontally or vertically.
            if (p0.X != p1.X && p0.Y != p1.Y)
            {
                return false;
            }

            // Line must be horizontal or vertical.
            if (line.Start.X != line.End.X && line.Start.Y != line.End.Y)
            {
                return false;
            }

            LineShapeViewModel split;
            if (line.Start.X > line.End.X || line.Start.Y > line.End.Y)
            {
                split = ViewModelFactory.CreateLineShape(
                    p0,
                    line.End,
                    (ShapeStyleViewModel)line.Style.Copy(null),
                    line.IsStroked);

                SwapLineEnd(line, p1);
            }
            else
            {
                split = ViewModelFactory.CreateLineShape(
                    p1,
                    line.End,
                    (ShapeStyleViewModel)line.Style.Copy(null),
                    line.IsStroked);

                SwapLineEnd(line, p0);
            }

            Project.AddShape(Project.CurrentContainer.CurrentLayer, split);

            return true;
        }

        public bool TryToConnectLines(IEnumerable<LineShapeViewModel> lines, ImmutableArray<PointShapeViewModel> connectors)
        {
            if (connectors.Length > 0)
            {
                var lineToPoints = new Dictionary<LineShapeViewModel, IList<PointShapeViewModel>>();

                double threshold = Project.Options.HitThreshold / PageState.ZoomX;
                double scale = PageState.ZoomX;

                // Find possible connector to line connections.
                foreach (var connector in connectors)
                {
                    LineShapeViewModel result = null;
                    foreach (var line in lines)
                    {
                        double radius = Project.Options.HitThreshold / PageState.ZoomX;
                        if (HitTest.Contains(line, new Point2(connector.X, connector.Y), threshold, scale))
                        {
                            result = line;
                            break;
                        }
                    }

                    if (result is { })
                    {
                        if (lineToPoints.ContainsKey(result))
                        {
                            lineToPoints[result].Add(connector);
                        }
                        else
                        {
                            lineToPoints.Add(result, new List<PointShapeViewModel>());
                            lineToPoints[result].Add(connector);
                        }
                    }
                }

                // Try to split lines using connectors.
                bool success = false;
                foreach (var kv in lineToPoints)
                {
                    var line = kv.Key;
                    var points = kv.Value;
                    if (points.Count == 2)
                    {
                        var p0 = points[0];
                        var p1 = points[1];
                        bool horizontal = Abs(p0.Y - p1.Y) < threshold;
                        bool vertical = Abs(p0.X - p1.X) < threshold;

                        // Points are aligned horizontally.
                        if (horizontal && !vertical)
                        {
                            if (p0.X <= p1.X)
                            {
                                success = TryToSplitLine(line, p0, p1);
                            }
                            else
                            {
                                success = TryToSplitLine(line, p1, p0);
                            }
                        }

                        // Points are aligned vertically.
                        if (!horizontal && vertical)
                        {
                            if (p0.Y >= p1.Y)
                            {
                                success = TryToSplitLine(line, p1, p0);
                            }
                            else
                            {
                                success = TryToSplitLine(line, p0, p1);
                            }
                        }
                    }
                }

                return success;
            }

            return false;
        }

        private GroupShapeViewModel Group(LayerContainerViewModel layer, ISet<BaseShapeViewModel> shapes, string name)
        {
            if (layer is { } && shapes is { })
            {
                var source = layer.Shapes.ToBuilder();
                var group = ViewModelFactory.CreateGroupShape(name);
                group.Group(shapes, source);

                var previous = layer.Shapes;
                var next = source.ToImmutable();
                Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                layer.Shapes = next;

                return group;
            }

            return null;
        }

        private void Ungroup(LayerContainerViewModel layer, ISet<BaseShapeViewModel> shapes)
        {
            if (layer is { } && shapes is { })
            {
                var source = layer.Shapes.ToBuilder();

                foreach (var shape in shapes)
                {
                    if (shape is GroupShapeViewModel group)
                    {
                        group.Ungroup(source);
                    }
                }

                var previous = layer.Shapes;
                var next = source.ToImmutable();
                Project?.History?.Snapshot(previous, next, (p) => layer.Shapes = p);
                layer.Shapes = next;
            }
        }

        public GroupShapeViewModel Group(ISet<BaseShapeViewModel> shapes, string name)
        {
            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is { })
            {
                return Group(layer, shapes, name);
            }

            return null;
        }

        public bool Ungroup(ISet<BaseShapeViewModel> shapes)
        {
            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is { } && shapes is { })
            {
                Ungroup(layer, shapes);
                return true;
            }

            return false;
        }

        private void Swap(BaseShapeViewModel shape, int sourceIndex, int targetIndex)
        {
            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer?.Shapes is { })
            {
                if (sourceIndex < targetIndex)
                {
                    Project.SwapShape(layer, shape, targetIndex + 1, sourceIndex);
                }
                else
                {
                    if (layer.Shapes.Length + 1 > sourceIndex + 1)
                    {
                        Project.SwapShape(layer, shape, targetIndex, sourceIndex + 1);
                    }
                }
            }
        }

        public void BringToFront(BaseShapeViewModel source)
        {
            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is { })
            {
                var items = layer.Shapes;
                int sourceIndex = items.IndexOf(source);
                int targetIndex = items.Length - 1;
                if (targetIndex >= 0 && sourceIndex != targetIndex)
                {
                    Swap(source, sourceIndex, targetIndex);
                }
            }
        }

        public void BringForward(BaseShapeViewModel source)
        {
            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is { })
            {
                var items = layer.Shapes;
                int sourceIndex = items.IndexOf(source);
                int targetIndex = sourceIndex + 1;
                if (targetIndex < items.Length)
                {
                    Swap(source, sourceIndex, targetIndex);
                }
            }
        }

        public void SendBackward(BaseShapeViewModel source)
        {
            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is { })
            {
                var items = layer.Shapes;
                int sourceIndex = items.IndexOf(source);
                int targetIndex = sourceIndex - 1;
                if (targetIndex >= 0)
                {
                    Swap(source, sourceIndex, targetIndex);
                }
            }
        }

        public void SendToBack(BaseShapeViewModel source)
        {
            var layer = Project?.CurrentContainer?.CurrentLayer;
            if (layer is { })
            {
                var items = layer.Shapes;
                int sourceIndex = items.IndexOf(source);
                int targetIndex = 0;
                if (sourceIndex != targetIndex)
                {
                    Swap(source, sourceIndex, targetIndex);
                }
            }
        }

        public void MoveShapesBy(IEnumerable<BaseShapeViewModel> shapes, decimal dx, decimal dy)
        {
            foreach (var shape in shapes)
            {
                if (!shape.State.HasFlag(ShapeStateFlags.Locked))
                {
                    shape.Move(null, dx, dy);
                }
            }
            OnUpdateDecorator();
        }

        private void MoveShapesByWithHistory(IEnumerable<BaseShapeViewModel> shapes, decimal dx, decimal dy)
        {
            MoveShapesBy(shapes, dx, dy);
            OnUpdateDecorator();

            var previous = new { DeltaX = -dx, DeltaY = -dy, Shapes = shapes };
            var next = new { DeltaX = dx, DeltaY = dy, Shapes = shapes };
            Project?.History?.Snapshot(previous, next, (s) => MoveShapesBy(s.Shapes, s.DeltaX, s.DeltaY));
        }

        public void MoveBy(ISet<BaseShapeViewModel> shapes, decimal dx, decimal dy)
        {
            if (shapes is { })
            {
                switch (Project?.Options?.MoveMode)
                {
                    case MoveMode.Point:
                        {
                            var points = new List<PointShapeViewModel>();

                            foreach (var shape in shapes)
                            {
                                if (!shape.State.HasFlag(ShapeStateFlags.Locked))
                                {
                                    shape.GetPoints(points);
                                }
                            }

                            var distinct = points.Distinct().ToList();
                            MoveShapesByWithHistory(distinct, dx, dy);
                        }
                        break;

                    case MoveMode.Shape:
                        {
                            var items = shapes.Where(s => !s.State.HasFlag(ShapeStateFlags.Locked));
                            MoveShapesByWithHistory(items, dx, dy);
                        }
                        break;
                }
            }
        }

        public void MoveItem<T>(LibraryViewModel<T> libraryViewModel, int sourceIndex, int targetIndex)
        {
            if (sourceIndex < targetIndex)
            {
                var item = libraryViewModel.Items[sourceIndex];
                var builder = libraryViewModel.Items.ToBuilder();
                builder.Insert(targetIndex + 1, item);
                builder.RemoveAt(sourceIndex);

                var previous = libraryViewModel.Items;
                var next = builder.ToImmutable();
                Project?.History?.Snapshot(previous, next, (p) => libraryViewModel.Items = p);
                libraryViewModel.Items = next;
            }
            else
            {
                int removeIndex = sourceIndex + 1;
                if (libraryViewModel.Items.Length + 1 > removeIndex)
                {
                    var item = libraryViewModel.Items[sourceIndex];
                    var builder = libraryViewModel.Items.ToBuilder();
                    builder.Insert(targetIndex, item);
                    builder.RemoveAt(removeIndex);

                    var previous = libraryViewModel.Items;
                    var next = builder.ToImmutable();
                    Project?.History?.Snapshot(previous, next, (p) => libraryViewModel.Items = p);
                    libraryViewModel.Items = next;
                }
            }
        }

        public void SwapItem<T>(LibraryViewModel<T> libraryViewModel, int sourceIndex, int targetIndex)
        {
            var item1 = libraryViewModel.Items[sourceIndex];
            var item2 = libraryViewModel.Items[targetIndex];
            var builder = libraryViewModel.Items.ToBuilder();
            builder[targetIndex] = item1;
            builder[sourceIndex] = item2;

            var previous = libraryViewModel.Items;
            var next = builder.ToImmutable();
            Project?.History?.Snapshot(previous, next, (p) => libraryViewModel.Items = p);
            libraryViewModel.Items = next;
        }

        public void InsertItem<T>(LibraryViewModel<T> libraryViewModel, T item, int index)
        {
            var builder = libraryViewModel.Items.ToBuilder();
            builder.Insert(index, item);

            var previous = libraryViewModel.Items;
            var next = builder.ToImmutable();
            Project?.History?.Snapshot(previous, next, (p) => libraryViewModel.Items = p);
            libraryViewModel.Items = next;
        }
    }
}

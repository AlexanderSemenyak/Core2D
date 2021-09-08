﻿#nullable enable
using System;
using System.Linq;
using Core2D.Model;
using Core2D.ViewModels.Containers;

namespace Core2D.ViewModels.Editor.Factories
{
    public class ContainerFactory : IContainerFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ContainerFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private LibraryViewModel DefaultStyleLibrary()
        {
            var factory = _serviceProvider.GetService<IViewModelFactory>();
            var sgd = factory.CreateLibrary("Default");

            var builder = sgd.Items.ToBuilder();

            builder.Add(factory.CreateShapeStyle("Solid"));

            sgd.Items = builder.ToImmutable();
            sgd.Selected = sgd.Items.FirstOrDefault();

            return sgd;
        }

        private TemplateContainerViewModel CreateDefaultTemplate(IContainerFactory containerFactory, ProjectContainerViewModel project, string name)
        {
            var factory = _serviceProvider.GetService<IViewModelFactory>();
            var template = containerFactory.GetTemplate(project, name);

            template.IsGridEnabled = false;
            template.IsBorderEnabled = false;
            template.GridOffsetLeft = 30.0;
            template.GridOffsetTop = 30.0;
            template.GridOffsetRight = -30.0;
            template.GridOffsetBottom = -30.0;
            template.GridCellWidth = 30.0;
            template.GridCellHeight = 30.0;
            template.GridStrokeColor = factory.CreateArgbColor(0xFF, 0xDE, 0xDE, 0xDE);
            template.GridStrokeThickness = 1.0;

            return template;
        }

        TemplateContainerViewModel IContainerFactory.GetTemplate(ProjectContainerViewModel project, string name)
        {
            var factory = _serviceProvider.GetService<IViewModelFactory>();
            var template = factory.CreateTemplateContainer(name);
            template.Background = factory.CreateArgbColor(0xFF, 0xFF, 0xFF, 0xFF);
            return template;
        }

        PageContainerViewModel IContainerFactory.GetPage(ProjectContainerViewModel project, string name)
        {
            var factory = _serviceProvider.GetService<IViewModelFactory>();
            var container = factory.CreatePageContainer(name);
            container.Template = project.CurrentTemplate is { } 
                ? project.CurrentTemplate.CopyShared(null)
                : (this as IContainerFactory).GetTemplate(project, "Default");
            return container;
        }

        DocumentContainerViewModel IContainerFactory.GetDocument(ProjectContainerViewModel project, string name)
        {
            var factory = _serviceProvider.GetService<IViewModelFactory>();
            var document = factory.CreateDocumentContainer(name);
            return document;
        }

        ProjectContainerViewModel IContainerFactory.GetProject()
        {
            var factory = _serviceProvider.GetService<IViewModelFactory>();
            var containerFactory = this as IContainerFactory;
            var project = factory.CreateProjectContainer("Project1");

            // Group Libraries
            var glBuilder = project.GroupLibraries.ToBuilder();
            glBuilder.Add(factory.CreateLibrary("Default"));
            project.GroupLibraries = glBuilder.ToImmutable();

            project.SetCurrentGroupLibrary(project.GroupLibraries.FirstOrDefault());

            // Style Libraries
            var sgBuilder = project.StyleLibraries.ToBuilder();
            sgBuilder.Add(DefaultStyleLibrary());
            project.StyleLibraries = sgBuilder.ToImmutable();

            project.SetCurrentStyleLibrary(project.StyleLibraries.FirstOrDefault());

            // Templates
            var templateBuilder = project.Templates.ToBuilder();
            templateBuilder.Add(CreateDefaultTemplate(this, project, "Default"));
            project.Templates = templateBuilder.ToImmutable();

            project.SetCurrentTemplate(project.Templates.FirstOrDefault(t => t.Name == "Default"));

            // Scripts
            var scriptBuilder = project.Scripts.ToBuilder();
            scriptBuilder.Add(factory.CreateScript());
            project.Scripts = scriptBuilder.ToImmutable();

            project.SetCurrentScript(project.Scripts.FirstOrDefault());

            // Documents and Pages
            var document = containerFactory.GetDocument(project, "Document1");
            var page = containerFactory.GetPage(project, "Page1");

            var pageBuilder = document.Pages.ToBuilder();
            pageBuilder.Add(page);
            document.Pages = pageBuilder.ToImmutable();

            var documentBuilder = project.Documents.ToBuilder();
            documentBuilder.Add(document);
            project.Documents = documentBuilder.ToImmutable();

            project.SetCurrentContainer(page);

            // Databases
            var db = factory.CreateDatabase("Default");
            var columnsBuilder = db.Columns.ToBuilder();
            columnsBuilder.Add(factory.CreateColumn(db, "Column0"));
            columnsBuilder.Add(factory.CreateColumn(db, "Column1"));
            db.Columns = columnsBuilder.ToImmutable();
            project.Databases = project.Databases.Add(db);

            project.SetCurrentDatabase(db);

            return project;
        }
    }
}

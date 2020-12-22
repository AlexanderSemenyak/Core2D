﻿#nullable disable
using Core2D.Model;
using Core2D.ViewModels.Shapes;
using Core2D.ViewModels.Style;
using Xunit;

namespace Core2D.ViewModels.UnitTests.Containers
{
    public class ProjectContainerTests
    {
        private readonly IFactory _factory = new Factory(null);

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void Inherits_From_Selectable()
        {
            var target = _factory.CreateProjectContainer();
            Assert.True(target is ViewModelBase);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void Options_Not_Null()
        {
            var target = _factory.CreateProjectContainer();
            Assert.NotNull(target.Options);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void StyleLibraries_Not_Null()
        {
            var target = _factory.CreateProjectContainer();
            Assert.False(target.StyleLibraries.IsDefault);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void GroupLibraries_Not_Null()
        {
            var target = _factory.CreateProjectContainer();
            Assert.False(target.GroupLibraries.IsDefault);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void Databases_Not_Null()
        {
            var target = _factory.CreateProjectContainer();
            Assert.False(target.Databases.IsDefault);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void Templates_Not_Null()
        {
            var target = _factory.CreateProjectContainer();
            Assert.False(target.Templates.IsDefault);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void Scripts_Not_Null()
        {
            var target = _factory.CreateProjectContainer();
            Assert.False(target.Scripts.IsDefault);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void Documents_Not_Null()
        {
            var target = _factory.CreateProjectContainer();
            Assert.False(target.Documents.IsDefault);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void SetCurrentDocument_Sets_CurrentDocument()
        {
            var target = _factory.CreateProjectContainer();

            var document = _factory.CreateDocumentContainer();
            target.Documents = target.Documents.Add(document);

            target.SetCurrentDocument(document);

            Assert.Equal(document, target.CurrentDocument);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void SetCurrentContainer_Sets_CurrentContainer_And_Selected()
        {
            var target = _factory.CreateProjectContainer();

            var document = _factory.CreateDocumentContainer();
            target.Documents = target.Documents.Add(document);

            var page = _factory.CreatePageContainer();
            document.Pages = document.Pages.Add(page);

            target.SetCurrentContainer(page);

            Assert.Equal(page, target.CurrentContainer);
            Assert.Equal(page, target.Selected);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void SetCurrentTemplate_Sets_CurrentTemplate()
        {
            var target = _factory.CreateProjectContainer();

            var template = _factory.CreateTemplateContainer();
            target.Templates = target.Templates.Add(template);

            target.SetCurrentTemplate(template);

            Assert.Equal(template, target.CurrentTemplate);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void SetCurrentScript_Sets_CurrentScript()
        {
            var target = _factory.CreateProjectContainer();

            var script = _factory.CreateScript();
            target.Scripts = target.Scripts.Add(script);

            target.SetCurrentScript(script);

            Assert.Equal(script, target.CurrentScript);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void SetCurrentDatabase_Sets_CurrentDatabase()
        {
            var target = _factory.CreateProjectContainer();

            var db = _factory.CreateDatabase("Db");
            target.Databases = target.Databases.Add(db);

            target.SetCurrentDatabase(db);

            Assert.Equal(db, target.CurrentDatabase);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void SetCurrentGroupLibrary_Sets_CurrentGroupLibrary()
        {
            var target = _factory.CreateProjectContainer();

            var library = _factory.CreateLibrary<GroupShapeViewModel>("Library1");
            target.GroupLibraries = target.GroupLibraries.Add(library);

            target.SetCurrentGroupLibrary(library);

            Assert.Equal(library, target.CurrentGroupLibrary);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void SetCurrentStyleLibrary_Sets_CurrentStyleLibrary()
        {
            var target = _factory.CreateProjectContainer();

            var library = _factory.CreateLibrary<ShapeStyleViewModel>("Library1");
            target.StyleLibraries = target.StyleLibraries.Add(library);

            target.SetCurrentStyleLibrary(library);

            Assert.Equal(library, target.CurrentStyleLibrary);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void SetSelected_Layer()
        {
            var target = _factory.CreateProjectContainer();

            var document = _factory.CreateDocumentContainer();
            target.Documents = target.Documents.Add(document);

            var page = _factory.CreatePageContainer();
            document.Pages = document.Pages.Add(page);

            var layer = _factory.CreateLayerContainer("Layer1", page);
            page.Layers = page.Layers.Add(layer);
 
            target.SetSelected(layer);

            Assert.Equal(layer, page.CurrentLayer);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void SetSelected_Container()
        {
            var target = _factory.CreateProjectContainer();

            var document = _factory.CreateDocumentContainer();
            target.Documents = target.Documents.Add(document);

            var page = _factory.CreatePageContainer();
            document.Pages = document.Pages.Add(page);

            var layer = _factory.CreateLayerContainer("Layer1", page);
            page.Layers = page.Layers.Add(layer);

            bool raised = false;

            layer.InvalidateLayer += (sender, e) =>
            {
                raised = true;
            };

            target.SetSelected(page);

            Assert.Equal(document, target.CurrentDocument);
            Assert.Equal(page, target.CurrentContainer);
            Assert.True(raised);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void SetSelected_Document()
        {
            var target = _factory.CreateProjectContainer();

            var document = _factory.CreateDocumentContainer();
            target.Documents = target.Documents.Add(document);

            target.Selected = document;

            Assert.Equal(document, target.CurrentDocument);
        }
    }
}

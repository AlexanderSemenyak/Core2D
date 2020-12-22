﻿#nullable disable
using Core2D.Model;
using Core2D.ViewModels.Containers;
using Xunit;

namespace Core2D.ViewModels.UnitTests.Containers
{
    public class LayerContainerTests
    {
        private readonly IFactory _factory = new Factory(null);

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void Inherits_From_ViewModelBase()
        {
            var target = _factory.CreateLayerContainer();
            Assert.True(target is ViewModelBase);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void Shapes_Not_Null()
        {
            var target = _factory.CreateLayerContainer();
            Assert.False(target.Shapes.IsDefault);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void Invalidate_Raises_InvalidateLayer_Event()
        {
            var target = _factory.CreateLayerContainer("Layer1");

            bool raised = false;

            target.InvalidateLayer += (sender, e) =>
            {
                raised = true;
            };

            target.RaiseInvalidateLayer();

            Assert.True(raised);
        }

        [Fact]
        [Trait("Core2D.Containers", "Project")]
        public void Invalidate_Sets_EventArgs()
        {
            var target = _factory.CreateLayerContainer("Layer1");

            InvalidateLayerEventArgs args = null;

            target.InvalidateLayer += (sender, e) =>
            {
                args = e;
            };

            target.RaiseInvalidateLayer();

            Assert.NotNull(args);
            Assert.True(args is InvalidateLayerEventArgs);
        }
    }
}

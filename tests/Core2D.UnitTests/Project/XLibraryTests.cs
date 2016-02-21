﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Core2D.Project;
using Xunit;

namespace Core2D.UnitTests
{
    public class XLibraryTests
    {
        [Fact]
        [Trait("Core2D.Project", "Project")]
        public void Inherits_From_ObservableObject()
        {
            var target = new XLibrary<XTemplate>();
            Assert.True(target is ObservableObject);
        }

        [Fact]
        [Trait("Core2D.Project", "Project")]
        public void Items_Not_Null()
        {
            var target = new XLibrary<XTemplate>();
            Assert.NotNull(target.Items);
        }

        [Fact]
        [Trait("Core2D.Project", "Project")]
        public void Selected_Is_Null()
        {
            var target = new XLibrary<XTemplate>();
            Assert.Null(target.Selected);
        }

        [Fact]
        [Trait("Core2D.Project", "Project")]
        public void SetSelected_Sets_Selected()
        {
            var target = new XLibrary<XTemplate>();

            var item = XTemplate.Create();
            target.Items = target.Items.Add(item);

            target.SetSelected(item);

            Assert.Equal(item, target.Selected);
        }
    }
}

﻿using System;
using Core2D.Renderer;
using Core2D.Shapes;

namespace Core2D.Common.UnitTests
{
    public class TestPointShape : PointShape
    {
        public override Type TargetType => typeof(TestPointShape);
    }
}

﻿#nullable enable
namespace Core2D.Model
{
    public interface IStringExporter
    {
        string ToXamlString();

        string ToSvgString();
    }
}

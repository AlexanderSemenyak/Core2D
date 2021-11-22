﻿#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Core2D.Model;
using Core2D.ViewModels;
using Core2D.ViewModels.Editor;
using Core2D.Views;

namespace Core2D.Editor;

public class AvaloniaImageImporter : IImageImporter
{
    private readonly IServiceProvider? _serviceProvider;

    public AvaloniaImageImporter(IServiceProvider? serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private Window? GetWindow()
    {
        return _serviceProvider?.GetService<Window>();
    }

    public async Task<string> GetImageKeyAsync()
    {
        try
        {
            var dlg = new OpenFileDialog() { Title = "Open" };
            dlg.Filters.Add(new FileDialogFilter() { Name = "All", Extensions = { "*" } });
            var result = await dlg.ShowAsync(GetWindow());
            var path = result?.FirstOrDefault();
            if (path is { })
            {
                return _serviceProvider.GetService<ProjectEditorViewModel>().OnGetImageKey(path);
            }
        }
        catch (Exception ex)
        {
            _serviceProvider.GetService<ILog>()?.LogException(ex);
        }

        return default;
    }
}

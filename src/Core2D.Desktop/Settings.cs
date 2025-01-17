﻿using System.IO;

namespace Core2D.Desktop;

public class Settings
{
    public string? Theme { get; set; } = null;
    public FileInfo[]? Scripts { get; set; }
    public FileInfo? Project { get; set; }
    public bool UseSkia { get; set; }
#if ENABLE_DIRECT2D1
    public bool UseDirect2D1 { get; set; }
#endif
    public bool UseGpu { get; set; } = true;
    public bool AllowEglInitialization { get; set; } = true;
    public bool UseWgl { get; set; }
    public bool UseWindowsUIComposition { get; set; } = true;
    public bool UseDirectX11 { get; set; }
    public bool UseManagedSystemDialogs { get; set; }
    public bool UseHeadless { get; set; }
    public bool UseHeadlessDrawing { get; set; }
    public bool UseHeadlessVnc { get; set; }
    public string? VncHost { get; set; } = null;
    public int VncPort { get; set; } = 5901;
}

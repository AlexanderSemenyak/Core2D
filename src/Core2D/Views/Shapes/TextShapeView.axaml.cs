﻿using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Core2D.ViewModels.Editor;
using Core2D.ViewModels.Shapes;

namespace Core2D.Views.Shapes
{
    public class TextShapeView : UserControl
    {
        public TextShapeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void OnEditTextBinding(object shape)
        {
            if (VisualRoot is TopLevel topLevel
                && topLevel.DataContext is ProjectEditorViewModel editor
                && shape is TextShapeViewModel text)
            {
                var dialog = editor.CreateTextBindingDialog(text);
                if (dialog is { })
                {
                    editor.ShowDialog(dialog);
                }
            }
        }
    }
}

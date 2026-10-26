using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace PSMultiTools;

public partial class PSXColorSelection : Window
{
    public Color SelectedColor { get; private set; }

    public PSXColorSelection()
    {
        InitializeComponent();

        ConfirmButton.Click += (_, _) =>
        {
            SelectedColor = SelectedColorPicker.Color;
            Close(SelectedColor);
        };
    }
}

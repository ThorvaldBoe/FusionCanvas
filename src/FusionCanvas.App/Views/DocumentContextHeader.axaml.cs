using Avalonia.Controls;

namespace FusionCanvas.App.Views;

public partial class DocumentContextHeader : UserControl
{
    public DocumentContextHeader()
    {
        InitializeComponent();
    }

    public bool FocusIdeationButton() => IdeationButton.Focus();
}

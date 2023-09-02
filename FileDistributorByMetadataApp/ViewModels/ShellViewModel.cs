using FileDistributorByMetadataApp.Common;
using FileDistributorByMetadataApp.Interfaces;

namespace FileDistributorByMetadataApp.ViewModels;

public class ShellViewModel : BindableBase, IShell
{
    private object? _content;

    public object? Content
    {
        get => _content;
        private set => _content = value;
    }

    public void SetContent(BindableBase content)
    {
        Content = content;
    }
}
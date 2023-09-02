using FileDistributorByMetadataApp.Interfaces;
using Microsoft.WindowsAPICodePack.Dialogs;


namespace FileDistributorByMetadataApp.Services
{
    public class FolderPathSelector : IFolderPathSelector
    {
        public string GetFolderPath()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            return dialog.ShowDialog() == CommonFileDialogResult.Ok ? dialog.FileName : string.Empty;
        }
    }
}

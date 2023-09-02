using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FileDistributorByMetadataApp.Common;
using FileDistributorByMetadataApp.Common.Async;
using FileDistributorByMetadataApp.Interfaces;

namespace FileDistributorByMetadataApp.ViewModels;

public class FileDistributorViewModel : BindableBase
{
    private readonly IFolderPathSelector _folderPathSelector;
    private readonly IFileDistributionService _fileDistributionService;
    private IAsyncCommand? _processFileCommand;
    private ICommand? _selectInputFolderPathCommand;
    private ICommand? _selectDestinationFolderPathCommand;
    private ICommand? _closeCommand;
    private string? _selectedInputFolderPath;
    private string? _selectedDestinationFolderPath;
    private string? _selectedMetadataLanguage;
    private int _progress;

    public SortedDictionary<string, string> MetadataLanguages { get; } = new()
    {
        { "ua", "Ukrainian" },
        { "en", "English" },
        { "fr", "French"},
        { "de", "German"},
        { "ja", "Japanese"},
        { "es", "Spanish"},
        { "it", "Italian"},
        { "ru", "Russian"}
    };

    public FileDistributorViewModel(IFolderPathSelector folderPathSelector, IFileDistributionService fileDistributionService)
    {
        _folderPathSelector = folderPathSelector ?? throw new ArgumentNullException(nameof(folderPathSelector));
        _fileDistributionService = fileDistributionService ?? throw new ArgumentNullException(nameof(fileDistributionService));
        SelectedMetadataLanguage = "ua";
    }

    public string? SelectedInputFolderPath
    {
        get => _selectedInputFolderPath;
        set
        {
            _selectedInputFolderPath = value;
            OnPropertyChanged();
        }
    }

    public string? SelectedDestinationFolderPath
    {
        get => _selectedDestinationFolderPath;
        set
        {
            _selectedDestinationFolderPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AreFoldersPathsFilled));
        }
    }

    public string? SelectedMetadataLanguage
    {
        get => _selectedMetadataLanguage;
        set
        {
            _selectedMetadataLanguage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AreFoldersPathsFilled));
        }
    }

    public bool AreFoldersPathsFilled => !string.IsNullOrEmpty(SelectedInputFolderPath) && !string.IsNullOrEmpty(SelectedDestinationFolderPath);

    public int Progress
    {
        get => _progress;
        set
        {
            _progress = value;
            OnPropertyChanged();
        }
    }

    #region Select folders paths command

    public ICommand SelectInputFolderPathCommand =>
        _selectInputFolderPathCommand ??= new RelayCommand(SelectInputFolderPathCmdExecute);

    public ICommand SelectDestinationFolderPathCommand =>
        _selectDestinationFolderPathCommand ??= new RelayCommand(SelectDestinationFolderPathCmdExecute);

    public ICommand CloseCommand =>
        _closeCommand ??= new RelayCommand(o => ((Window)o).Close());

    private void SelectInputFolderPathCmdExecute(object param)
    {
        SelectedInputFolderPath = _folderPathSelector.GetFolderPath();
    }

    private void SelectDestinationFolderPathCmdExecute(object param)
    {
        SelectedDestinationFolderPath = _folderPathSelector.GetFolderPath();
    }

    #endregion

    #region Process file distrubution command

    public IAsyncCommand ProcessFileDistributionCommand =>
        _processFileCommand ??= new AsyncCommand(ProcessFileDistribution);

    private async Task ProcessFileDistribution(CancellationToken ct)
    {
        Progress = 0;
        IProgress<int> progress = new Progress<int>(i => Progress = i);
        await _fileDistributionService.DistributeFilesByGpsCoordinatesAndDateAsync(SelectedInputFolderPath!,
            SelectedDestinationFolderPath!, SelectedMetadataLanguage!, progress, ct).ConfigureAwait(false);
    }

    #endregion
}
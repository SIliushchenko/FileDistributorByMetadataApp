using System.Windows.Input;

namespace FileDistributorByMetadataApp.Common.Async;

public interface IAsyncCommand : ICommand
{
    Task ExecuteAsync(object? parameter);
}
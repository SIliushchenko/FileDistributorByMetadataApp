using System.ComponentModel;

namespace FileDistributorByMetadataApp.Common.Async;

public sealed class NotifyTaskCompletion : INotifyPropertyChanged
{
    public NotifyTaskCompletion(Task task)
    {
        Task = task;
        TaskCompletion = WatchTaskAsync(task);
    }
    private async Task WatchTaskAsync(Task task)
    {
        try
        {
            await task;
        }
        catch
        {
            // ignored
        }

        var propertyChanged = PropertyChanged;
        if (propertyChanged == null)
            return;
        propertyChanged(this, new PropertyChangedEventArgs("Status"));
        propertyChanged(this, new PropertyChangedEventArgs("IsCompleted"));
        propertyChanged(this, new PropertyChangedEventArgs("IsNotCompleted"));
        if (task.IsCanceled)
        {
            propertyChanged(this, new PropertyChangedEventArgs("IsCanceled"));
        }
        else if (task.IsFaulted)
        {
            propertyChanged(this, new PropertyChangedEventArgs("IsFaulted"));
            propertyChanged(this, new PropertyChangedEventArgs("Exception"));
            propertyChanged(this, new PropertyChangedEventArgs("InnerException"));
            propertyChanged(this, new PropertyChangedEventArgs("ErrorMessage"));
        }
        else
        {
            propertyChanged(this, new PropertyChangedEventArgs("IsSuccessfullyCompleted"));
            propertyChanged(this, new PropertyChangedEventArgs("Result"));
        }
    }
    public Task Task { get; }

    public Task TaskCompletion { get; }

    public TaskStatus Status => Task.Status;

    public bool IsCompleted => Task.IsCompleted;

    public bool IsNotCompleted => !Task.IsCompleted;

    public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;

    public bool IsCanceled => Task.IsCanceled;

    public bool IsFaulted => Task.IsFaulted;

    public AggregateException? Exception => Task.Exception;

    public Exception? InnerException => Exception?.InnerException;

    public string? ErrorMessage => InnerException?.Message;

    public event PropertyChangedEventHandler? PropertyChanged;
}
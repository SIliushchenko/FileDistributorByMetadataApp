using FileDistributorByMetadataApp.ViewModels;
using System.Windows;
using Autofac;
using FileDistributorByMetadataApp.Interfaces;
using FileDistributorByMetadataApp.Services.Services;
using FileDistributorByMetadataApp.Services;
using NLog;
using System.Threading.Tasks;
using System.Windows.Threading;
using System;
using System.ComponentModel;
using Application = System.Windows.Application;
using IContainer = Autofac.IContainer;
using MessageBox = System.Windows.MessageBox;

namespace FileDistributorByMetadataApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private static IContainer? _container;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public App()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<FileDistributionService>().As<IFileDistributionService>();
            builder.RegisterType<FolderPathSelector>().As<IFolderPathSelector>();
            builder.RegisterType<ShellViewModel>().SingleInstance();
            builder.RegisterType<ShellWindow>().SingleInstance();
            builder.RegisterType<FileDistributorViewModel>().SingleInstance();

            _container = builder.Build();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            RegisterGlobalExceptionHandling();
            var startupForm = _container?.Resolve<ShellWindow>();
            var shellVm = _container?.Resolve<ShellViewModel>();
            var fileDistributorVm = _container?.Resolve<FileDistributorViewModel>();
            shellVm?.SetContent(fileDistributorVm!);
            startupForm!.DataContext = shellVm;

            startupForm.Show();
            startupForm.Closing += MainWindow_Closing;
            base.OnStartup(e);
        }

        private void RegisterGlobalExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException +=
                (_, args) => CurrentDomainOnUnhandledException(args);

            Dispatcher.UnhandledException +=
                (_, args) => DispatcherOnUnhandledException(args);

            Application.Current.DispatcherUnhandledException +=
                (_, args) => CurrentOnDispatcherUnhandledException(args);

            TaskScheduler.UnobservedTaskException +=
                (_, args) => TaskSchedulerOnUnobservedTaskException(args);
        }

        private static void TaskSchedulerOnUnobservedTaskException(UnobservedTaskExceptionEventArgs args)
        {
            Logger.Error(args.Exception, args.Exception.Message);
            args.SetObserved();
        }

        private static void CurrentOnDispatcherUnhandledException(DispatcherUnhandledExceptionEventArgs args)
        {
            Logger.Error(args.Exception, args.Exception.Message);
            args.Handled = true;
        }

        private static void DispatcherOnUnhandledException(DispatcherUnhandledExceptionEventArgs args)
        {
            Logger.Error(args.Exception, args.Exception.Message);
            args.Handled = true;
        }

        private static void CurrentDomainOnUnhandledException(UnhandledExceptionEventArgs args)
        {
            var exception = args.ExceptionObject as Exception;
            var terminatingMessage = args.IsTerminating ? " The application is terminating." : string.Empty;
            var exceptionMessage = exception?.Message ?? "An unmanaged exception occured.";
            var message = string.Concat(exceptionMessage, terminatingMessage);
            Logger.Error(exception, message);
        }

        private static void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to exit?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No)
                e.Cancel = true;
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _container!.DisposeAsync();
            base.OnExit(e);
        }
    }
}

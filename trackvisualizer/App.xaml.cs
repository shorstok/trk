using System;
using System.Windows;
using System.Windows.Threading;
using Autofac;
using Autofac.Builder;
using trackvisualizer.Ioc;
using trackvisualizer.View;

namespace trackvisualizer
{
    /// <summary>
    ///     Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IContainer _container;

        private App()
        {
            Startup += App_Startup;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("Unhandled exception: " + e.Exception);
        }


        private void App_Startup(object sender, StartupEventArgs e)
        {
            MaybeSetTheme();
            BuildContainer();

            var window = _container.Resolve<TrackView>();
            
            MainWindow = window;
            
            window.Show();
        }


        private void BuildContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule<MainModule>();

            _container = builder.Build();
        }

        
        private void MaybeSetTheme()
        {
            try
            {
                var uri = new Uri(
                    "PresentationFramework.Aero;V3.0.0.0;31bf3856ad364e35;component\\themes/aero.normalcolor.xaml",
                    UriKind.Relative);
                Resources.MergedDictionaries.Add(LoadComponent(uri) as ResourceDictionary);
            }
            catch (Exception)
            {
            }
        }
    }
}
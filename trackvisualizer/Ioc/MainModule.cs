using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Autofac.Core.Activators.Reflection;
using trackvisualizer.Config;
using trackvisualizer.Service;
using trackvisualizer.Service.HeightmapProviders;
using trackvisualizer.Service.Middleware;
using trackvisualizer.Service.ReportExporters;
using trackvisualizer.View;
using trackvisualizer.Vm;

namespace trackvisualizer.Ioc
{
    internal class MainModule : Module
    {
        /// <summary>
        /// Selects constructor with most parameters and forbids parameterless constructor - to catch cases where 
        /// parameterless ctor selected when one dep is missing
        /// </summary>
        protected class NotXamlConstructorSelector : IConstructorSelector
        {
            
            public ConstructorParameterBinding SelectConstructorBinding(ConstructorParameterBinding[] constructorBindings,
                IEnumerable<Parameter> parameters)
            {
                var vmCtor = constructorBindings.Where(binding=>binding.CanInstantiate).OrderByDescending(binding => binding.TargetConstructor.GetParameters().Length).FirstOrDefault();

                if(null == vmCtor || vmCtor.TargetConstructor.GetParameters().Length == 0)
                    throw new InvalidOperationException(@"Only default ctor for VM available");

                return vmCtor;
            }
        }
        
        protected override void Load(ContainerBuilder builder)
        {
            //Load config
            builder.RegisterInstance(TrekplannerConfiguration.LoadOrCreate(true)).AsSelf().SingleInstance();

            //Middleware
            builder.RegisterType<KmlLoaderMiddleware>().As<IGeoLoaderMiddleware>().SingleInstance();
            builder.RegisterType<GpxLoaderMiddleware>().As<IGeoLoaderMiddleware>().SingleInstance();
            
            builder.RegisterType<HtmlReportExporter>().As<ITrackReportExporter>().SingleInstance();

            //Services
            builder.RegisterType<UiService>().As<IUiService>().AsSelf().SingleInstance();
            builder.RegisterType<SrtmRepository>().AsSelf().SingleInstance();
            builder.RegisterType<GeoLoaderService>().AsSelf().SingleInstance();
            builder.RegisterType<HeightmapManagerVm>().AsSelf().SingleInstance();
            builder.RegisterType<LocalizationManager>().AsSelf().SingleInstance();
            builder.RegisterType<UiLoggingVm>().As<IUiLoggingService>().AsSelf().SingleInstance();
            
            //Heightmap providers
            builder.RegisterType<SrtmFileDownloadHeightmapProvider>().AsSelf().As<IHeightmapProvider>().SingleInstance();
            
            //Windows
            
            builder.RegisterType<HeightmapDownloaderWindow>().AsSelf().InstancePerDependency();            
            builder.RegisterType<MainWindow>().AsSelf().SingleInstance();
            
            builder.RegisterType<TrackVm>().AsSelf().InstancePerDependency();
            builder.RegisterType<TrackReportVm>().AsSelf().InstancePerDependency();
            builder.RegisterType<TrackReportItemVm>().AsSelf().InstancePerDependency();
            
            builder.RegisterType<TrackManagerVm>().AsSelf().SingleInstance();

            //VMs
            builder.RegisterType<HeightmapDownloaderVm>().AsSelf().
                InstancePerDependency()
                .UsingConstructor(new NotXamlConstructorSelector());     
            
            builder.RegisterType<MainSettingsVm>().AsSelf().
                InstancePerDependency()
                .UsingConstructor(new NotXamlConstructorSelector());
        }
    }
}

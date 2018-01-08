using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;
using Autofac;
using View.Workers;
using View.Services;

using Logix;
using SysConfigManager;

namespace View.Main
{
    class Program
    {
        static void Main(string[] args)
        {
            /* Dependencies */
            string _ConfigPath = AppDomain.CurrentDomain.BaseDirectory + @"\Config";
            SysConfigManager.LaserDeskConfigManager ConfigMgr = new SysConfigManager.LaserDeskConfigManager(_ConfigPath);
            ConfigMgr.Load();
            Controller PLC = new Controller(ConfigMgr.SystemConfig.PLCIPaddress, ConfigMgr.SystemConfig.PLCPath, ConfigMgr.SystemConfig.PLCTimeout);

            var builder = new Autofac.ContainerBuilder();
            builder.RegisterType<MyService>();
            builder.RegisterType<Controller>();
            builder.RegisterType<LaserDeskConfigManager>();
            builder.RegisterType<LaserDeskWorker>()
                .UsingConstructor(typeof(Controller), typeof(SysConfigManager.LaserDeskConfigManager))
                .WithParameter("controller", PLC)
                .WithParameter("config", ConfigMgr);
            builder.RegisterInstance(PLC);
            builder.RegisterInstance(ConfigMgr);

            var container = builder.Build();

            HostFactory.Run(hostConfigurator =>
            {
                hostConfigurator.Service<MyService>(serviceConfigurator =>
                {
                    serviceConfigurator.ConstructUsing(() => container.Resolve<MyService>());
                    serviceConfigurator.WhenStarted(myService => myService.Start());
                    serviceConfigurator.WhenStopped(myService => myService.Stop());
                });

                hostConfigurator.RunAsLocalSystem();

                hostConfigurator.SetDisplayName("MyService");
                hostConfigurator.SetDescription("MyService using Topshelf");
                hostConfigurator.SetServiceName("MyService");
            });
        }
    }
}

using AWS.Logger.Log4net;
using log4net;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
namespace LogTest3
{
    public static class Logger
    {

        private static readonly string LOG_CONFIG_FILE = @"log4net.config";

        private static readonly log4net.ILog _log = GetLogger(typeof(Logger));

        public static ILog GetLogger(Type type)
        {
            return LogManager.GetLogger(type);
        }

        public static void Debug(object message)
        {
            SetLog4NetConfiguration();
            _log.Debug(message);
        }

        private static void SetLog4NetConfiguration()
        {
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead(LOG_CONFIG_FILE));

            var repo = LogManager.CreateRepository(
                Assembly.GetEntryAssembly(), typeof(Hierarchy));

            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);
        }
    }

    public static class NoConfigLogger
    {
        public static void ConfigureLog4net()
        {
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository(Assembly.GetEntryAssembly());
            PatternLayout patternLayout = new PatternLayout
            {
                ConversionPattern = "%-4timestamp [%thread] %-5level %logger %ndc - %message%newline"
            };
            patternLayout.ActivateOptions();

            AWSAppender appender = new AWSAppender
            {
                Name="StartupLogger",
                Layout = patternLayout,
                Threshold = Level.Debug,
                // Set log group and region. Assume credentials will be found using the default profile or IAM credentials.
                LogGroup = "Logging.Startup",
                Region = "us-east-1"
            };

            appender.ActivateOptions();
            hierarchy.Root.AddAppender(appender);
            
            hierarchy.Root.Level = Level.All;
           
            hierarchy.Configured = true;
        }
    }
}


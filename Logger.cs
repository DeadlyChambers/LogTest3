using AWS.Logger.Log4net;
using log4net;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using LogTest3.Appenders;
using LogTest3.Layouts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
namespace LogTest3
{
    //TODO: This extension method is a little over kill
    public static class Logger
    {        
        public static ILog GetLogger(Type type)
        {
            return LogManager.GetLogger(type);
        }
    }

    /// <summary>
    /// TODO: How to Setup Log4net Appenders, Layouts, and Filters without using log4net.config if that
    /// is somethine we want/need
    /// </summary>
    public static class NoConfigLogger
    {
        public static void ConfigureCloudWatchLog4net()
        {
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository(Assembly.GetEntryAssembly());
            //PatternLayout patternLayout = new PatternLayout
            //{
            //    ConversionPattern = "%-4timestamp [%thread] %-5level %logger %ndc - %message%newline"
            //};
            var splunkLayoutCW = new SplunkLayout()
            {
                LoggedProcessId = "LogTest3Rolling.LAB",
                TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ",
                WithTimeStamp = false,

            };
            splunkLayoutCW.ActivateOptions();

            //You should be able create any appender and load it into the LogManager which
            //would allow you to drop the log4net.config
            AWSAppender cWappender = new AWSAppender
            {
                Name = "StartupLogger",
                Layout = splunkLayoutCW,
                BatchPushInterval = new TimeSpan(0, 0, 0, 5, 0),
                Threshold = Level.Debug,
                // Set log group and region. Assume credentials will be found using the default profile or IAM credentials.
                LogGroup = "Logging.Startup",
                Region = "us-east-1"
            };
            var cwFilter = new log4net.Filter.LoggerMatchFilter()
            {
                LoggerToMatch = "LogTest3.CloudWatchFilter",
                AcceptOnMatch = true
            };
            var non = new log4net.Filter.DenyAllFilter();
            non.ActivateOptions();
            cwFilter.ActivateOptions();
            cWappender.AddFilter(cwFilter);
            cWappender.AddFilter(non);
            cWappender.ActivateOptions();
            hierarchy.Root.AddAppender(cWappender);
        }
        public static void ConfigureLog4net()
        {            
            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository(Assembly.GetEntryAssembly());
            var splunkLayoutS3 = new SplunkLayout()
            {
                LoggedProcessId = "LogTest3Rolling.LAB",
                TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ",
                ObjectFormat = "html",
                Header = "<h1>Adding a Header???</h1>",
                Footer = "<h3>Also a footer??? Oh la la</h3>"
            };
            splunkLayoutS3.ActivateOptions();
            var s3appender = new S3Appender()
            {
                Name = "S3NoConfigAppender",
                Layout = splunkLayoutS3,
                Threshold = Level.Debug,
                BufferSize = 5,
                LibraryLogFileName = "_Log_NoConfigError",
                BucketName = "logtest2bucketpoc",
                LogDirectory = "WhatIsThis",
                FilePrefix = "S3Appender_html",
                FileExtension = "html" ,
                
            };
            var htmlFilter = new log4net.Filter.LoggerMatchFilter()
            {
                LoggerToMatch = "LogTest3.HtmlFilter",
                AcceptOnMatch = true
            };
            var non = new log4net.Filter.DenyAllFilter();
                non.ActivateOptions();
            htmlFilter.ActivateOptions();
            s3appender.AddFilter(htmlFilter);
            s3appender.AddFilter(non);

            s3appender.ActivateOptions();
            hierarchy.Root.AddAppender(s3appender);
           

         
        }
    }

    /// <summary>
    /// Simple class to help isolate the Log4net.config LoggerMatch
    /// </summary>
    public class HtmlFilter
    {

    }

    /// <summary>
    /// Simple class to help isolate the Log4net.config LoggerMatch
    /// </summary>
    public class CloudWatchFilter
    {

    }

    /// <summary>
    /// Simple class to help isolate the Log4net.config LoggerMatch
    /// </summary>
    public class JsonFilter
    {

    }
}


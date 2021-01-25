using log4net.Core;
using log4net.Layout;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogTest3.Layouts
{
    /// <summary>

    /// A layout that will be used by any flat file appenders to ensure they are in proper Splunk

    /// format.

    /// </summary>

    public class SplunkLayout : LayoutSkeleton

    {

        /// <summary>

        /// The log process id is set in the Log4Net.config, used to determine

        /// what application is logging

        /// </summary>

        public string LoggedProcessId { get; set; }



        /// <summary>

        /// The timestamp format is being set in the Log4Net.config, used to display

        /// the timestamp in the logs for Splunk

        /// </summary>

        public string TimestampFormat { get; set; }



        /// <summary>

        /// The optional entry assembly name is set in the Log4Net.config, used to determine

        /// what assembly is logging

        /// </summary>

        public string EntryAssemblyName { get; set; }



        /// <summary>

        /// If you need to initialize something for later access in the format method, like setting

        /// up a serializer or some such thing this is where you do that. We are inheriting from an

        /// abstract so this method must be implemented.

        /// </summary>

        public override void ActivateOptions()

        {



        }

        /// <summary>

        /// Here we are going to format the flat files for Splunk. If it is

        /// IO we are going to splunkify the httpRequestLogDto, if it is just

        /// a poco then we will splunkify that.

        /// </summary>

        /// <param name="writer"></param>

        /// <param name="loggingEvent"></param>

        public override void Format(TextWriter writer, LoggingEvent loggingEvent)

        {

            IgnoresException = false;

            var timeZone = TimeZoneInfo.Local.IsDaylightSavingTime(loggingEvent.TimeStamp)

                ? "CDT"

                : "CST";

            try

            {

                var httpRequestLogDto = loggingEvent.ToHttpRequestLogDto(true);

                var splunkString = httpRequestLogDto == null

                    ? new StringBuilder(loggingEvent.ToLogRecord(LoggedProcessId, EntryAssemblyName, true).ToSplunkString())

                    : new StringBuilder(httpRequestLogDto.ToSplunkString());

                //If we have sent a splunk key value pair in, we are specifically trying to disply

                //splunk information in the logs

                if (loggingEvent.MessageObject is LoggingNameValuePair pair)

                {

                    var splunkKvp = pair;

                    if (splunkKvp.Pairs != null)

                    {

                        splunkString.Append($",{splunkKvp.ToSplunkFormat()}");

                    }

                }
                //CloudWatch doesn't need the timestamp
                //writer.WriteLine($"{loggingEvent.TimeStamp.ToString(TimestampFormat)} {timeZone},{splunkString}");
                writer.WriteLine(splunkString);

            }

            catch (Exception e)

            {
                //CloudWatch doesn't need the timestamp
                //writer.WriteLine($"{loggingEvent.TimeStamp.ToString(TimestampFormat)} {timeZone},{e.ToSplunkString()}");
                writer.WriteLine(e.ToSplunkString());

            }

        }

    }






    public static class SplunkUtils

    {
        /// <summary>

        /// This will output an objects properties out as a key value pair in a splunk

        /// specific format.

        /// </summary>

        /// <param name="this"></param>

        /// <returns></returns>

        public static string ToSplunkString(this object @this)

        {

            var logString = new StringBuilder();

            var props = @this.GetType().GetProperties();

            foreach (var item in props)

            {

                logString.Append(SplunkUtils.SplunkifyKeyValue(item.Name, item.GetValue(@this, null)?.ToString()));

            }

            return logString.ToString().TrimEnd(',');

        }

        /// <summary>

        /// This will allow you to make splunk formatted key value pair

        /// </summary>

        /// <param name="key"></param>

        /// <param name="value"></param>

        /// <returns></returns>

        public static string SplunkifyKeyValue(string key, string value)

        {

            //Per splunk specs we cannot use a property named source because it is used by splunk

            var displayName = key.Equals("source", StringComparison.OrdinalIgnoreCase)

                ? "Origin"

                : key;

            //Timestamp is being applied in the layout removing redundancy, reportData is too big, logid/errorlogid are always 0

            if (displayName.Equals("timestamp", StringComparison.OrdinalIgnoreCase)

                 || displayName.Equals("reportData", StringComparison.OrdinalIgnoreCase)

                 || displayName.Equals("logid", StringComparison.OrdinalIgnoreCase)

                 || displayName.Equals("errorlogid", StringComparison.OrdinalIgnoreCase))

                return "";



            if (value != null)

            {

                value = Regex.Replace(value, @"[\u000A\u000B\u000C\u000D\u2028\u2029\u0085]+", String.Empty) //All of the carriage returns, or new lines

                    .Replace("\"", string.Empty).Replace("{", string.Empty).Replace("}", string.Empty).Replace("[", string.Empty).Replace("]", string.Empty); //For objects

            }

            return $" {displayName}=\"{value}\",";

        }

    }



    /////////////////////////////////////////



    public static class LoggingEventMappings

    {

        /// <summary>

        /// For any form of logging we want to turn a logging event into a log record if it is

        /// not IO logging. If it is writing to the db, we might want to ensure the object if in

        /// splunk format is added to the formatted message.

        /// </summary>

        /// <param name="loggingEvent"></param>

        /// <param name="loggedProcessId"></param>

        /// <param name="entryAssemblyName"></param>

        /// <param name="writingFlatFile"></param>

        /// <returns></returns>

        public static LogRecord ToLogRecord(this LoggingEvent loggingEvent, string loggedProcessId, string entryAssemblyName, bool writingFlatFile = false)

        {

            var assemblies = new Lazy<Assembly[]>(() => AppDomain.CurrentDomain.GetAssemblies());

            var entryAssembly = new Lazy<Assembly>(Assembly.GetEntryAssembly);

            var assembly = string.IsNullOrWhiteSpace(entryAssemblyName)

                ? null

                : assemblies.Value.FirstOrDefault(f => f.FullName.StartsWith(entryAssemblyName));



            return new LogRecord

            {

                EventId = null,  // Not currently used.  Future use ??

                Priority = loggingEvent.GetPriority(),

                Severity = loggingEvent.GetSeverity(),

                Title = loggingEvent.GetTitle(),

                Timestamp = loggingEvent.TimeStamp,

                MachineName = Environment.MachineName,

                AppDomainName = (assembly ?? entryAssembly.Value).GetName().Name,

                ProcessId = loggedProcessId,

                ProcessName = (assembly ?? entryAssembly.Value).Location,

                ThreadName = loggingEvent.ThreadName,

                Win32ThreadId = Process.GetCurrentProcess().Id.ToString(),

                Message = CreateRenderedMessage(loggingEvent),

                FormattedMessage = GetFormattedMessage(loggingEvent, writingFlatFile)

            };

        }



        /// <summary>

        /// The formatted Message is being done similar to this in BDS. There are a few other fields that

        /// are added if you wish to build upon the logging information without adding columns.

        /// </summary>

        /// <returns></returns>

        private static string GetFormattedMessage(LoggingEvent loggingEvent, bool writingFlatFile)

        {

            var stringBuilder = new StringBuilder();

            if (!writingFlatFile && loggingEvent.MessageObject is LoggingNameValuePair pair)

            {

                stringBuilder.Append($"||Content,{pair.ToSplunkFormat()}");

            }

            return stringBuilder.ToString();

        }





        /// <summary>

        /// When getting IO communication we want to turn the logging event into an httpRequestLogDto

        /// which will allow us to show properly in the log table. Something to note this should return

        /// null if the MessageObject is not an HttpRequestLogDto.

        /// </summary>

        /// <param name="loggingEvent"></param>

        /// <param name="writingFlatFile"></param>

        /// <returns></returns>

        public static HttpRequestLogDto ToHttpRequestLogDto(this LoggingEvent loggingEvent, bool writingFlatFile = false)

        {

            var populatedHttpRequestLogDto = loggingEvent.MessageObject as HttpRequestLogDto;



            if (populatedHttpRequestLogDto == null)

            {

                return populatedHttpRequestLogDto;

            }



            //Cloning to ensure the values being changed don't cause any side effects

            populatedHttpRequestLogDto = populatedHttpRequestLogDto.Clone();

            populatedHttpRequestLogDto.TimeStamp = loggingEvent.TimeStamp;

            populatedHttpRequestLogDto.MachineName = Environment.MachineName;



            // flat File IO Logging will not log request response

            // this is due to security concerns.

            if (writingFlatFile)

            {

                populatedHttpRequestLogDto.Request = string.Empty;

                populatedHttpRequestLogDto.Response = string.Empty;

            }



            if (!writingFlatFile)

            {

                populatedHttpRequestLogDto.Response = populatedHttpRequestLogDto.Response?.ToJsonString(true);



                // DataMaskingUtility.MaskSensitiveIoData(populatedHttpRequestLogDto);

            }



            return populatedHttpRequestLogDto;

        }



        /// <summary>

        /// Replacing properties that need to be masked when string is in json format.

        /// </summary>

        /// <param name="jsonInput"></param>

        /// <returns></returns>





        /// <summary>

        /// Creates/formats the value that is written to the Message field

        /// </summary>

        /// <param name="loggingEvent"></param>

        /// <returns></returns>

        private static string CreateRenderedMessage(LoggingEvent loggingEvent)

        {

            if (loggingEvent.ExceptionObject == null)

            {

                if (loggingEvent.MessageObject is LoggingNameValuePair pair)

                {

                    return pair.Message;

                }

                return loggingEvent.RenderedMessage;

            }

            // Find the inner most exception message and build out a full stack trace

            var mostInnerException = loggingEvent.ExceptionObject;



            var fullStackTraceBuilder = new StringBuilder();

            fullStackTraceBuilder.AppendLine(mostInnerException.StackTrace);



            while (mostInnerException.InnerException != null)

            {

                mostInnerException = mostInnerException.InnerException;

                fullStackTraceBuilder.AppendLine(mostInnerException.StackTrace);

            }

            return $"{loggingEvent.RenderedMessage}: {mostInnerException.Message} {fullStackTraceBuilder}";

        }


        /// <summary>

        /// Helper method to serialize object into a JSON string

        /// </summary>

        public static string ToJsonString(this object @this, bool checkProperties = false, List<string> removeProperties = null)

        {

            if (checkProperties && @this.GetType().GetProperties().Length == 0)

            {

                return @this.ToString();

            }

            return removeProperties?.Count > 0 ?

                JsonConvert.SerializeObject(@this, new JsonSerializerSettings

                {

                    Converters = new JsonConverterCollection

                    {



                    }

                })

                : JsonConvert.SerializeObject(@this);



        }

    }

    /// </summary>

    public static class LoggingEventExtensions

    {



        /// <summary>

        /// Returns a number priority for a standard LoggingEvent

        /// </summary>

        /// <param name="loggingEvent"></param>

        /// <returns></returns>

        public static int GetPriority(this LoggingEvent loggingEvent)

        {

            // Priority is currently based on Severity/Level.  Arbitrary mapping from Dan, then changed by Shane to be specific

            // to the different possible levels we could be logging for business exceptions https://stackoverflow.com/questions/8926409

            if (loggingEvent.Level == Level.Emergency ||

                loggingEvent.Level == Level.Fatal)

            {

                return 0;

            }

            if (loggingEvent.Level == Level.Critical ||

               loggingEvent.Level == Level.Error)

            {

                return 1;

            }

            if (loggingEvent.Level == Level.Warn ||

                loggingEvent.Level == Level.Severe ||

                loggingEvent.Level == Level.Alert)

            {

                return 2;

            }

            if (loggingEvent.Level == Level.Info ||

                loggingEvent.Level == Level.Notice)

            {

                return 3;

            }



            return 4;

        }





        /// <summary>

        /// Returns a severity description a standard LoggingEvent

        /// </summary>

        /// <param name="loggingEvent"></param>

        /// <returns></returns>

        public static string GetSeverity(this LoggingEvent loggingEvent)

        {

            // Priority is currently the name of the Level

            return loggingEvent.Level.DisplayName;

        }





        /// <summary>

        /// Returns a standard format Title for a LoggingEvent

        /// </summary>

        /// <param name="loggingEvent"></param>

        /// <returns></returns>

        public static string GetTitle(this LoggingEvent loggingEvent)

        {

            // Currently just the type name associated with the logger



            return loggingEvent.LoggerName;

        }

    }

}

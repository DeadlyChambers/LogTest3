using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;


namespace LogTest3.Layouts
{
    /// <summary>
    /// This object should be created in a project that references common, and in the SplunkLayout it
    /// will be parsed and properly logged.
    /// </summary>
    public class LoggingNameValuePair
    {
        public string Message { get; set; }
        public ICollection<KeyValuePair<string, object>> Pairs { get; set; }

        public LoggingNameValuePair(string message, ICollection<KeyValuePair<string, object>> pairs)
        {
            Message = message;
            Pairs = pairs;
        }

        /// <summary>
        /// This will output the Pairs object in a proper Splunk format
        /// </summary>
        /// <returns></returns>
        public string ToSplunkFormat()
        {
            if (Pairs == null)
                return string.Empty;
            var splunkString = new StringBuilder();
            foreach (var kvp in Pairs)
            {
               splunkString.Append(SplunkUtils.SplunkifyKeyValue(kvp.Key, kvp.Value?.ToString()));
            }
            return splunkString.ToString().TrimEnd(',');
        }
    }
}

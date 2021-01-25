using System;
using System.ComponentModel.DataAnnotations;

namespace LogTest3.Layouts
{
    public class EventRecord
    {
        /// <summary>
        /// Auto-generated unique Id for the log entry.
        /// </summary>
        public int EventId { get; set; }
        /// <summary>
        /// Used to correlation records through out the system.
        /// </summary>
        [Required]
        public Guid CorrelationId { get; set; }
        /// <summary>
        /// Event Level such as Info, Debug, etc.
        /// </summary>
        [Required]
        public string EventLevel { get; set; }
        /// <summary>
        /// Event Type such as SmartSearchRequestReceived.
        /// </summary>
        [Required]
        public string EventType { get; set; }
        /// <summary>
        /// JSON representation of an object. Can also be a "free-form" JSON message.
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Name of the assembly making the logging call.
        /// </summary>
        public string EntryAssemblyName { get; set; }
        /// <summary>
        /// Name of the Machine this code is running on.
        /// </summary>
        public string MachineName { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;

namespace LogTest3.Layouts
{

    /// <summary>
    /// An object representation of a Log database record
    /// </summary>
    public class LogRecord
    {

        /// <summary>
        /// Auto-generated unique Id for the log entry
        /// </summary>
        public int LogId { get; set; }


        public int? EventId { get; set; }


        /// <summary>
        /// A number ranking for the log entry
        /// </summary>
        [Required]
        public int Priority { get; set; }


        /// <summary>
        /// Description of the severity (e.g., Information, Debug, Error, etc.)
        /// </summary>
        [Required]
        [StringLength(32)]
        public string Severity { get; set; }



        /// <summary>
        /// The name of the component or process generating the log information (e.g., "SmartMove Payment Transaction Service", "CR response handler")
        /// </summary>
        [Required]
        [StringLength(256)]
        public string Title { get; set; }


        /// <summary>
        /// The time when the event occurred
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The name of the server on which the application is running
        /// </summary>
        [Required]
        [StringLength(32)]
        public string MachineName { get; set; }


        /// <summary>
        /// The name of the application domain (e.g., "TURSS.ResponseWatcher.Service.exe", "TURSS.SmartMove.PaymentTransactionService.exe")
        /// </summary>
        [Required]
        [StringLength(512)]
        public string AppDomainName { get; set; }

        /// <summary>
        /// The process identifier (e.g., "CR", "GWS_BusinessLogic", "SmartMove_PaymentTransactionWindowsService")
        /// </summary>
        [Required]
        [StringLength(256)]
        public string ProcessId { get; set; }


        /// <summary>
        /// The process name (e.g., "E:\TURSS\DataProviders\ResponseWatcher\TURSS.ResponseWatcher.Service.exe", "E:\Tasks\PaymentTransactionService\TURSS.SmartMove.PaymentTransactionService.exe")
        /// </summary>
        [Required]
        [StringLength(512)]
        public string ProcessName { get; set; }


        [StringLength(512)]
        public string ThreadName { get; set; }


        /// <summary>
        /// The managed thread name (e.g., "28668", "39872")
        /// </summary>
        [StringLength(128)]
        public string Win32ThreadId { get; set; }


        /// <summary>
        /// The log message
        /// </summary>
        public string Message { get; set; }


        /// <summary>
        ///    (e.g., "||UserName,SYSTEM||Timestamp,03/11/2017 22:32:44.741")
        /// </summary>
        public string FormattedMessage { get; set; }


    }
}

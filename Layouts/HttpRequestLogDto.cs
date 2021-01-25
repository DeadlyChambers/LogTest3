using System;
using System.ComponentModel.DataAnnotations;

namespace LogTest3.Layouts
{
    /// <summary>
    /// An object representation of a Log database record
    /// </summary>
    public class HttpRequestLogDto
    {
        /// <summary>
        /// The time at which the action occurred
        /// </summary>
        [Required]
        public DateTime TimeStamp { get; set; }
        

        /// <summary>
        /// The source of the log entry
        /// </summary>
        [Required]
        [StringLength(32)]
        public string Source { get; set; }


        /// <summary>
        /// The foundation partner identifier
        /// </summary>
        [Required]
        public long? PartnerId { get; set; }


        /// <summary>
        /// The name of the Api controller
        /// </summary>
        [Required]
        [StringLength(32)]
        public string Controller { get; set; }


        /// <summary>
        /// The name of the specific action method
        /// </summary>
        [Required]
        [StringLength(32)]
        public string Method { get; set; }

        /// <summary>
        /// The controller entity type
        /// </summary>
        [Required]
        [StringLength(32)]
        public string EntityType { get; set; }


        /// <summary>
        /// The Id of the entity
        /// </summary>
        [StringLength(32)]
        public string EntityIdentifier { get; set; }


        /// <summary>
        /// The Http return code
        /// </summary>
        [Required]
        public int ReturnCode { get; set; }


        /// <summary>
        /// The request associated with the call
        /// </summary>
        [Required]
        [StringLength(32)]
        public string Request { get; set; }


        /// <summary>
        /// The returned response
        /// </summary>
        public object Response { get; set; }


        /// <summary>
        /// The amount of time in milliseconds that the call took to return a value
        /// </summary>
        [Required]
        public int ElapsedExecutionTime { get; set; }


        /// <summary>
        /// An identifier associated with the error (if applicable)
        /// </summary>
        [Required]
        public long? ErrorLogId { get; set; }

        
        /// <summary>
        /// An associated visit or session id
        /// </summary>
        [Required]
        public long? VisitId { get; set; }

        /// <summary>
        /// IP address of consumer/customers
        /// </summary>
        public string ClientIp { get; set; }

        /// <summary>
        /// The Headers from the request
        /// </summary>
        public string Header { get; set; }

        /// <summary>
        /// The name of the Machine
        /// </summary>
        public string MachineName { get; set; }
        /// <summary>
        /// An associated CorrelationId
        /// </summary>
        public Guid? CorrelationId { get; set; }
        public HttpRequestLogDto Clone()
        {
            return new HttpRequestLogDto
            {
                Controller = this.Controller,
                ElapsedExecutionTime = this.ElapsedExecutionTime,
                EntityIdentifier = this.EntityIdentifier,
                EntityType = this.EntityType,
                ErrorLogId = this.ErrorLogId,
                Header = this.Header,
                MachineName = this.MachineName,
                Method = this.Method,
                PartnerId = this.PartnerId,
                Request = this.Request,
                Response = this.Response,
                ReturnCode = this.ReturnCode,
                Source = this.Source,
                TimeStamp = this.TimeStamp,
                VisitId = this.VisitId,
                ClientIp= this.ClientIp,
                CorrelationId = this.CorrelationId
            };
        }
    }
}

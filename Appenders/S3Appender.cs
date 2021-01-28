using Amazon.S3;
using Amazon.S3.Model;
using log4net;
using log4net.Core;
using log4net.Util;
using System;
using System.Text;
using System.Threading.Tasks;

namespace LogTest3.Appenders
{
    /// <summary>
    /// An Appender to write directly to S3
    /// </summary>
    public class S3Appender : S3AppenderSkelton
    {
        public S3Appender()
        {
            Evaluator = new MyTrigger();
            
        }

        /// <summary>
        /// The trigger that will ensure the buffer call is made early. If an Error occurs, I want the
        /// buffer to write immediately. Any other calls can wait.
        /// </summary>
        private class MyTrigger : ITriggeringEventEvaluator
        {
            public bool IsTriggeringEvent(LoggingEvent loggingEvent)
            {
                return loggingEvent.Level >= Level.Error;
            }
        }

        /// <summary>
        /// Sends the events.
        /// </summary>
        /// <param name="events">The events that need to be send.</param>
        /// <remarks>
        /// <para>
        /// The subclass must override this method to process the buffered events.
        /// </para>
        /// </remarks>
        protected override void SendBuffer(LoggingEvent[] events)
        {
            var content = new StringBuilder();
            foreach (var loggingevent in events)
            {
                try
                {
                    content.Append(RenderLoggingEvent(loggingevent));
                }catch(Exception e)
                {
                    _logInception.Error("Exception Rendering Event in Logging Appender", e);
                    this.Append(loggingevent);
                }
            }
            new Task(() => UploadEvent(Client, content.ToString())).Start();
        }

        /// <summary>
        /// TODO: Just delete this entire method as it is here to specifically throw an error for testing
        /// </summary>
        /// <param name="loggingEvent"></param>
        protected override void Append(LoggingEvent loggingEvent)
        {
         
            //TODO: Test Code to prove Exception handling in Appender
            if (loggingEvent.ExceptionObject is MissingFieldException)
                throw new ApplicationException("Force Exception from S3MemoryAppender");
            else if (loggingEvent.ExceptionObject is ArithmeticException)
                _logInception.Error("Force Exception form S3MemoryAppender", new ApplicationException("Exception Inception"));
            
            base.Append(loggingEvent);
        }

        /// <summary>
        /// Upload to S3
        /// </summary>
        /// <param name="client"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private async Task UploadEvent(AmazonS3Client client, string content)
        {
            ///TODO: Delete as this is a forced error
            var bucketName = _bucketName;
            if (content.Contains("THROW AN ERROR"))
                bucketName = "THISBUCKETDOESNTEXIST";
            _ = client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = Filename(),
                ContentBody = content            
            });
        }
    }

    /// <summary>
    /// TODO: Delete this, used specifically for testing Logging calls inside of the Appenders
    /// </summary>
    public class ExceptionInception
    {

    }
}

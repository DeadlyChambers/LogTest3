using Amazon.S3;
using Amazon.S3.Model;
using log4net.Appender;
using log4net.Core;
using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogTest3.Appenders
{
    public class S3Appender : BufferingAppenderSkeleton
    {
        private string _bucketName;

        public string BucketName
        {
            get
            {
                if (String.IsNullOrEmpty(_bucketName))
                    throw new ApplicationException("BucketNameNotSpecified");
                return _bucketName;
            }
            set
            {
                _bucketName = value;
            }
        }

        private bool _createBucket = true;

        /// <summary>
        /// If true, checks whether the bucket already exists and if not creates it.
        /// If false, assumes that the bucket is already created and does not check.
        /// Set this to false if the AWS credentials used by the S3Appender do not
        /// have sufficient privileges to call ListBuckets() or PutBucket()
        /// </summary>
        public bool CreateBucket
        {
            get
            {
                return _createBucket;
            }
            set
            {
                _createBucket = value;
            }
        }

        internal AmazonS3Client Client { get; private set; }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            Client = new AmazonS3Client(Utility.GetRegionEndpoint());

            if (CreateBucket)
            {
                InitializeBucket();
            }
        }

        public AmazonS3Client InitializeBucket()
        {
            ListBucketsResponse response = Client.ListBucketsAsync().Result;
            bool found = response.Buckets.Any(bucket => bucket.BucketName == BucketName);

            if (found == false)
            {
                var resp = Client.PutBucketAsync(new PutBucketRequest() { BucketName = BucketName }).Result;
            }
            return Client;
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
            Parallel.ForEach(events, l => UploadEvent(l, Client));
        }

        private void UploadEvent(LoggingEvent loggingEvent, AmazonS3Client client)
        {           
            _ = client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = Filename(Guid.NewGuid().ToString()),
                ContentBody = Utility.GetXmlString(loggingEvent)
            }).Result;

            // log.txt
            // log.1.txt
        }

        private static string Filename(string key)
        {
            return string.Format("s3appender.{0}.log4net.xml", key);
        }
    }


    internal class Utility
    {
        internal static Amazon.RegionEndpoint GetRegionEndpoint()
        {
            var regionEndpoint = ConfigurationManager.AppSettings["Log4net.Appender.Amazon.RegionEndpoint"];
            return regionEndpoint == null ? Amazon.RegionEndpoint.USEast1 : Amazon.RegionEndpoint.GetBySystemName(regionEndpoint);
        }

        internal static string GetXmlString(LoggingEvent loggingEvent)
        {
            var xmlMessage = new XElement(
                "LogEntry",
                new XElement("UserName", loggingEvent.UserName),
                new XElement("TimeStamp",
                             loggingEvent.TimeStamp.ToString(CultureInfo.InvariantCulture)),
                new XElement("ThreadName", loggingEvent.ThreadName),
                new XElement("LoggerName", loggingEvent.LoggerName),
                new XElement("Level", loggingEvent.Level.ToString()),
                new XElement("Identity", loggingEvent.Identity),
                new XElement("Domain", loggingEvent.Domain),
                new XElement("CreatedOn", DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
                new XElement("RenderedMessage", loggingEvent.RenderedMessage)
                );

            string exceptionStr = loggingEvent.GetExceptionString();

            if (!string.IsNullOrEmpty(exceptionStr))
            {
                xmlMessage.Add(new XElement("Exception", exceptionStr));
            }

            return xmlMessage.ToString();
        }
    }
}

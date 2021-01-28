using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using log4net;
using log4net.Appender;
using log4net.Util;
using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace LogTest3.Appenders
{
    public abstract class S3AppenderSkelton : BufferingAppenderSkeleton
    {
        protected string _logDirectory;
        protected string _bucketName;

        /// <summary>
        /// TODO: Should get rid of this, but need to consider what to do when S3 Appenders fail
        /// </summary>
        protected ILog _logInception;
        /// <summary>
        /// This is the prefiz for your Key Object
        /// </summary>
        public string LogDirectory
        {
            get
            {
                return (String.IsNullOrEmpty(_logDirectory)) ? "" : String.Format("{0}/", _logDirectory);
            }
            set
            {
                _logDirectory = value;
            }
        }

        public string LibraryLogFileName { get; set; } = "S3AppenderLog";

        /// <summary>
        /// Name of Bucket in S3
        /// </summary>
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

        /// <summary>
        /// First word in the name of the object in S3
        /// </summary>
        public string FilePrefix { get; set; } = "S3Appender";


        /// <summary>
        /// If true, checks whether the bucket already exists and if not creates it.
        /// If false, assumes that the bucket is already created and does not check.
        /// Set this to false if the AWS credentials used by the S3Appender do not
        /// have sufficient privileges to call ListBuckets() or PutBucket()
        /// </summary>
        public bool CreateBucket { get; set; } = true;

        /// <summary>
        /// The file extension saved to S3
        /// </summary>
        public string FileExtension { get; set; } = "txt";

        /// <summary>
        /// S3 Client used to PUT/GET S3 Objects
        /// </summary>
        internal AmazonS3Client Client { get; private set; }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            Client = new AmazonS3Client(GetRegionEndpoint());

            Client.BeforeRequestEvent += ServiceClientBeforeRequestEvent;
            Client.ExceptionEvent += ServiceClienExceptionEvent;

            if (CreateBucket)
            {
                InitializeBucket();
            }
            
            _logInception = Logger.GetLogger(typeof(ExceptionInception));
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
        /// Set the name of the file to send to S3.
        /// The name is hardcoded right  now
        /// </summary>
        /// <returns></returns>
        protected string Filename()
        {
            return string.Format("{0}{1}_{2}.{3}", LogDirectory, FilePrefix, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff"), FileExtension);
        }

        protected void ServiceClienExceptionEvent(object sender, ExceptionEventArgs e)
        {
            var eventArgs = e as WebServiceExceptionEventArgs;
            if (eventArgs?.Exception != null)
                LogLibraryServiceError(eventArgs?.Exception, eventArgs.Endpoint?.ToString());
            else
                LogLibraryServiceError(new System.Net.WebException(e.GetType().ToString()));
        }

        protected void LogLibraryServiceError(Exception ex, string serviceUrl = null)
        {
            ////Look at implementing CloudWatch or some other infrastructure to Alert when the Exception
            ///Inception occurs
            //LogLibraryAlert?.Invoke(this, new LogLibraryEventArgs(ex) { ServiceUrl = serviceUrl ?? GetServiceUrl() });
            if (!string.IsNullOrEmpty(LibraryLogFileName))
            {
                LogLibraryError(ex, LibraryLogFileName);
            }
        }
        /// <summary>
        /// Write Exception details to the file specified with the filename
        /// </summary>
        public void LogLibraryError(Exception ex, string LibraryLogFileName)
        {
            try
            {
                using (StreamWriter w = File.AppendText(LibraryLogFileName))
                {
                    w.WriteLine("Log Entry : ");
                    w.WriteLine("{0}", DateTime.Now.ToString());
                    w.WriteLine("  :");
                    w.WriteLine("  :{0}", ex.ToString());
                    w.WriteLine("-------------------------------");
                }
                _logInception.Error("Appender Error", ex);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught when writing error log to file" + e.ToString());
            }
        }

        const string UserAgentHeader = "User-Agent";
        /// <summary>
        /// I am not using this, but could be utilized for some AWS Client items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ServiceClientBeforeRequestEvent(object sender, RequestEventArgs e)
        {
            var args = e as WebServiceRequestEventArgs;
            if (args == null || !args.Headers.ContainsKey(UserAgentHeader))
                return;

            args.Headers[UserAgentHeader] = args.Headers[UserAgentHeader] + " AWSLogger/" + this.Name;
        }

        /// <summary>
        /// TODO: You may want to load the Region in the log4net.config instead of hardcoding if
        /// it isn't found inj the appsettings.
        /// </summary>
        /// <returns></returns>
        internal static Amazon.RegionEndpoint GetRegionEndpoint()
        {
            var regionEndpoint = ConfigurationManager.AppSettings["Log4net.Appender.Amazon.RegionEndpoint"];
            return regionEndpoint == null ? Amazon.RegionEndpoint.USEast1 : Amazon.RegionEndpoint.GetBySystemName(regionEndpoint);
        }
    }
}

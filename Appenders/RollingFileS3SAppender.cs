using Amazon.S3;
using Amazon.S3.Model;
using log4net.Appender;
using log4net.Util;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace LogTest3.Appenders
{
    public class RollingFileS3Appender : RollingFileAppender
    {
        private string _bucketName;
        private string _logDirectory;

        /// <summary>
        /// This is the Bucket Name
        /// </summary>
        public string BucketName
        {
            get
            {
                if (String.IsNullOrEmpty(_bucketName))
                    throw new ApplicationException("No Bucket Name");
                return _bucketName;
            }
            set
            {
                _bucketName = value;
            }
        }

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
                _ = Client.PutBucketAsync(new PutBucketRequest() { BucketName = BucketName }).Result;
            }
            return Client;
        }

        /// <summary>
        /// Verify if the file is rolling and send the content to S3.
        /// </summary>
        protected override void AdjustFileBeforeAppend()
        {
            if ((File != null) && ((CountingQuietTextWriter)QuietWriter).Count >= MaxFileSize)
            {
                using (var logFile = new StreamReader(base.File))
                {
                    var content = logFile.ReadToEnd();
                    logFile.Close();
                    new Thread(() => UploadEvent(content)).Start();
                }
            }

            base.AdjustFileBeforeAppend();
        }

        /// <summary>
        /// Upload the log file to S3 Bucket.
        /// </summary>
        /// <param name="content"></param>
        private void UploadEvent(string content)
        {
            string key = Guid.NewGuid().ToString();          
            _ =Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = BucketName,
                Key = Filename(),
                ContentBody = content
            }).Result;
        }

        /// <summary>
        /// Set the name of the file to send to S3.
        /// The name is the machine IP.
        /// </summary>
        /// <returns></returns>
        private string Filename()
        {
            var ip = Dns.GetHostAddresses(Dns.GetHostName()).Select(x => x.ToString()).FirstOrDefault(x => x.Length >= 7 && x.Length <= 15).Replace(".", "-");
            return string.Format("{0}{1}_{2}.txt", LogDirectory, ip, CountObjects(ip));
        }

        private int CountObjects(string ip)
        {
            var prefix = String.Format("{0}{1}", LogDirectory, ip);
            var list = Client.ListObjectsAsync(BucketName, prefix).Result;
            var count = list.S3Objects.Count;
            var emptyFolder = count == 1 && list.S3Objects.FirstOrDefault().Key.Equals(LogDirectory);
            return emptyFolder ? 0 : count;
        }

    }
}

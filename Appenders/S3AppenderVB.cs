using Amazon.S3;
using log4net.Appender;
using log4net.Core;
using log4net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace LogTest3.Appenders
{
    /// <summary>
    /// The S3Appender stores events to a MemoryStream (or FileStream) and periodically
    /// sends them to S3.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The S3Appender stores events to a MemoryStream (or FileStream) and periodically
    /// sends them to S3.
    /// </para>
    /// <para>
    /// Stream is sent to S3 every N milliseconds set by the TimeInterval option.
    /// </para>
    /// <para>
    /// Stream is sent to S3 whenever the MaxStreamSize is exceeded.
    /// </para>
    /// <para>
    /// An overall MaxMemoryFootprint is set to ensure that the appender does not
    /// eat up RAM. When MaxMemoryFootprint is exceeded by MemoryStreams, the appender
    /// writes messages to FileStreams.
    /// </para> 
    /// </remarks>
    /// <author>Scott L. Mitchell</author>
    /// <author>Stephane G. Legay</author>
    public class S3AppenderVB : AppenderSkeleton
	{

		#region Public Instance Constructors

		public S3AppenderVB() : this(1)
		{
			//TODO: Determine if creating multiple worker threads is worthwhile performance-wise
		}

		public S3AppenderVB(int workerCount) : base()
		{
			m_stream = new MemoryStream();
			m_writer = new StreamWriter(m_stream);

			setNextTimeThreshold();

			//create a worker thread to monitor the filesToSend queue
			workers = new Thread[workerCount];

			// Create and start a separate thread for each worker
			for (int i = 0; i < workerCount; i++)
			{
				Debug("AmazonS3Appender: Creating Worker [" + i + "]");
				(workers[i] = new Thread(ConsumeFileAndSend)).Start();
			}
		}

		private void setNextTimeThreshold()
		{
			m_timeThreshold = DateTime.UtcNow.Add(new TimeSpan(0, 0, 0, 0, m_timeInterval));
		}

		private void Debug(string logStatement)
        {
			LogLog.Debug(typeof(S3AppenderVB), logStatement);
        }


		#endregion

		#region Override implementation of AppenderSkeleton

		override protected bool RequiresLayout
		{
			get
			{
				return true;
			}
		}

		override protected void Append(LoggingEvent loggingEvent)
		{
			RenderLoggingEvent(m_writer, loggingEvent);

			if (m_stream != null && (m_stream.Length > m_maxStreamSize || DateTime.UtcNow > m_timeThreshold))
			{
				if (DateTime.UtcNow > m_timeThreshold)
				{
					setNextTimeThreshold();
				}
				var streamToSend = m_stream;
				Stream newStream;

				if (m_currentMemoryFootprint > m_maxMemoryFootprint)
				{
					newStream = new FileStream(Path.GetTempFileName(), FileMode.OpenOrCreate);
				}
				else
				{
					newStream = new MemoryStream();
				}
				var newWriter = new StreamWriter(newStream);

				lock (this)
				{
					m_writer.Flush();
					m_writer = newWriter;
					m_stream = newStream;
				}
				EnqueueStreamToSend(streamToSend);
			}
			else
			{
				m_writer.Flush();
			}
		}

		protected override void OnClose()
		{
			Debug("AmazonS3Appender: About to close this appender");

			// Enqueue one null task per worker to make each exit.
			foreach (Thread worker in workers)
			{
				EnqueueStreamToSend(null);
			}

			// Wait for all workers to be done
			foreach (Thread worker in workers)
			{
				worker.Join();
			}

			// Clean up and streams that were requeued
			foreach (Stream stream in streamsToSend)
			{
				stream.Close();
			}

			Debug("AmazonS3Appender: Appender closed");
		}

		#endregion Override implementation of AppenderSkeleton

		#region Core AmazonS3Appender Methods
				
		protected void EnqueueStreamToSend(Stream aStream)
		{
			lock (locker)
			{
				streamsToSend.Enqueue(aStream);

				if (aStream is MemoryStream)
				{
					m_currentMemoryFootprint += aStream.Length;
				}

				Monitor.PulseAll(locker);
			}

		}

		protected void ConsumeFileAndSend()
		{
			Debug("AmazonS3Appender: Begin Consume and Send");
			while (true)
			{
				Debug("AmazonS3Appender: Begin While True");
				Stream streamToSend = null;

				lock (locker)
				{
					Debug("AmazonS3Appender: Worker obtained a lock");
					while (streamsToSend.Count == 0)
					{
						Debug("AmazonS3Appender: No more work ... so wait");
						Monitor.Wait(locker);
					}

					streamToSend = streamsToSend.Dequeue();
					Debug("AmazonS3Appender: Dequeued a stream");
				}

				if (streamToSend == null)
				{
					Debug("AmazonS3Appender: Stream was null so exiting thread");
					return;
				}
				else
				{
					try
					{
						string s3Key = S3FileName +
							"-" + DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss-fff", System.Globalization.CultureInfo.InvariantCulture) +
							"-" + String.Format("{0:0000}", new Random().Next(9999).ToString() +
							"." + S3FileExtension);

						Debug("AmazonS3Appender: Getting S3 Client");
						var s3 = new AmazonS3Client();
						streamToSend.Position = 0;

						Debug("AmazonS3Appender: About to add object [" + s3Key + "]");

						var response = s3.PutObjectAsync(
							new Amazon.S3.Model.PutObjectRequest { 
								InputStream = streamToSend, 
								BucketName = this.S3BucketName, 
								Key = s3Key }).Result;					
						Debug("AmazonS3Appender: Added object [" + s3Key + "]");

						if (streamToSend != null && streamToSend is MemoryStream)
						{
							Debug("AmazonS3Appender: Decrement footprint because a memory stream was sent");
							m_currentMemoryFootprint -= streamToSend.Length;
							streamToSend.Dispose();
						}
						else if (streamToSend != null && streamToSend is FileStream)
						{
							((FileStream)streamToSend).Close();

							try
							{
								Debug("AmazonS3Appender: Delete file because a file stream was sent");
								File.Delete(((FileStream)streamToSend).Name);
							}
							catch (Exception fileDeleteException)
							{
								ErrorHandler.Error("AmazonS3Appender: Could not delete [" + ((FileStream)streamToSend).Name + "]", fileDeleteException, ErrorCode.GenericFailure);
							}

						}
						else
						{
							throw new InvalidDataException("AmazonS3Appender: streamToSend was neither a MemoryStream nor a FileStream which is wrong");
						}

					}
					catch (Exception e)
					{
						ErrorHandler.Error("AmazonS3Appender: Cannot Send stream to S3 because error:", e);
						Debug("AmazonS3Appender: Try to Re-Enqueue the stream");
						EnqueueStreamToSend(streamToSend);
					}
				}
			}
		}

		#endregion

		#region Public Instance Properties

		public string S3FileName
		{
			get
			{
				return m_S3FileName;
			}
			set
			{
				m_S3FileName = value;
			}
		}

		public string S3FileExtension
		{
			get
			{
				return m_S3FileExtension;
			}
			set
			{
				m_S3FileExtension = value;
			}
		}

		public string S3BucketName
		{
			get
			{
				return m_S3BucketName;
			}
			set
			{
				m_S3BucketName = value;
			}
		}

		public string S3AccessKeyID
		{
			get
			{
				return m_S3AccessKeyID;
			}
			set
			{
				m_S3AccessKeyID = value;
			}
		}

		public string S3SecretAccessKey
		{
			get
			{
				return m_S3SecretAccessKey;
			}
			set
			{
				m_S3SecretAccessKey = value;
			}
		}

		public string MaxStreamSize
		{
			get
			{
				return m_maxStreamSize.ToString(NumberFormatInfo.InvariantInfo);
			}
			set
			{
				m_maxStreamSize = OptionConverter.ToFileSize(value, m_maxStreamSize + 1);
			}
		}
		public string MaxMemoryFootprint
		{
			get
			{
				return m_maxMemoryFootprint.ToString(NumberFormatInfo.InvariantInfo);
			}
			set
			{
				m_maxMemoryFootprint = OptionConverter.ToFileSize(value, m_maxMemoryFootprint + 1);
			}
		}

		public int TimeInterval
		{
			get
			{
				return m_timeInterval;
			}
			set
			{
				m_timeInterval = value;
			}
		}

		#endregion

		#region Private Instance Fields

		private string m_S3FileName;
		private string m_S3FileExtension;
		private string m_S3BucketName;
		private string m_S3AccessKeyID;
		private string m_S3SecretAccessKey;

		private long m_maxMemoryFootprint = 10 * 1024 * 1024; //default is 10MB
		private long m_maxStreamSize = 1 * 1024 * 1024; //default is 1MB
		private int m_timeInterval = 60000; //default is 60000 milliseconds or one minute

		private DateTime m_timeThreshold;
		private long m_currentMemoryFootprint = 0;

		private StreamWriter m_writer;
		private Stream m_stream;

		private object locker = new object();
		private Queue<Stream> streamsToSend = new Queue<Stream>();
		private Thread[] workers;


		#endregion

	}
}

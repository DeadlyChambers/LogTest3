# Appenders

There are 3 appenders that are working properly, S3Appender, RollingFileS3Appender, and AWSSplunked. I am running the AWS Toolkit in Visual 
Studio, so it takes care of the authorization pieces. That enables me to write to buckets and CloudWatch via local.

## S3Appender

You will need to setup an S3 bucket in us-east-1, and then update the log4net.config



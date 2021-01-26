# Appenders

There are 3 appenders that are working properly, S3Appender, RollingFileS3Appender, and AWSSplunked. I am running the AWS Toolkit in Visual 
Studio, so it takes care of the authorization pieces. That enables me to write to buckets and CloudWatch via local.

## S3Appender

You will need to setup an S3 bucket in us-east-1, and then update the log4net.config

## RollingFileS3Appender

You will need to add a folder the same S3 bucket in us-east-1, and then update the log4net.config accordingly.

When running this, you will want to do a get on the Values endpoint with a number over 500 to ensure the RollingAppender reaches
it's limit quicker  http://localhost:65508/values/600

## AWSSplunked

This appender usese CloudWatch. So you will need to setup a Log Group in CloudWatch us-east-1, and then update the log4net.config

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using AmazonWebApp.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AmazonRegion = global::Amazon.RegionEndpoint;
using System.Drawing;
using System.Drawing.Imaging;

namespace AmazonWebApp.Amazon
{
    public class AmazonClient
    {
        private readonly string _accessKeyId;
        private readonly string _secretAccessKey;

        private readonly string _bucketName;
        private readonly string _bucketUrl;
        private readonly string _queueUrl;

        public AmazonClient(IConfigurationRoot config)
        {
            _accessKeyId = config.GetValue<string>("AWS_ACCESS_KEY_ID");
            _secretAccessKey = config.GetValue<string>("AWS_SECRET_ACCESS_KEY");

            _bucketName = config.GetValue<string>("AWS_BUCKET_NAME");
            _bucketUrl = config.GetValue<string>("AWS_BUCKET_URL");
            _queueUrl = config.GetValue<string>("AWS_SQS_QUEUE_URL");
        }

        public UploadFileViewModel BuildUploadModel(string redirectUrl)
        {
            var policy = CalculatePolicy(redirectUrl);
            var signature = CalculateSignature(policy);

            return new UploadFileViewModel()
            {
                AccessKey = _accessKeyId,
                RedirectUrl = redirectUrl,
                UploadUrl = _bucketUrl,
                Policy = policy,
                Signature = signature
            };
        }

        public IEnumerable<PictureViewModel> ListPictures()
        {
            var pictureList = new List<PictureViewModel>();

            var request = new ListObjectsRequest
            {
                BucketName = _bucketName
            };

            using (var client = new AmazonS3Client(_accessKeyId, _secretAccessKey, AmazonRegion.USEast1))
            do
            {
                ListObjectsResponse response = client.ListObjectsAsync(request).Result;

                foreach (S3Object entry in response.S3Objects)
                {
                    pictureList.Add(new PictureViewModel()
                    {
                        Name = entry.Key,
                        ModifiedDate = entry.LastModified,
                        Size = entry.Size
                    });
                }

                if (response.IsTruncated)
                {
                    request.Marker = response.NextMarker;
                }
                else
                {
                    request = null;
                }
            } while (request != null);

            return pictureList;
        }

        public void RequestPictureTransformations(PicturesTransformationModel transformations)
        {
            using (var client = new AmazonSQSClient(_accessKeyId, _secretAccessKey, AmazonRegion.USWest2))
            {
                var batchRequest = new SendMessageBatchRequest()
                {
                    QueueUrl = _queueUrl,
                    Entries = new List<SendMessageBatchRequestEntry>()
                };

                foreach (var file in transformations.FileNames)
                {
                    batchRequest.Entries.Add(new SendMessageBatchRequestEntry()
                    {
                        Id = Guid.NewGuid().ToString(),
                        MessageBody = $"File '{file}' transformation [{transformations.Transformation.ToString()}] request",
                        MessageAttributes = new Dictionary<string, MessageAttributeValue>
                        {
                            { "fileName", new MessageAttributeValue() { StringValue = file, DataType = "String" } },
                            { "transformation", new MessageAttributeValue() { StringValue = transformations.Transformation.ToString(), DataType = "String" } },
                            { "date", new MessageAttributeValue() { StringValue = DateTime.Now.ToString("HH:mm:ss dd.MM.yyyy"), DataType = "String" } }
                        }
                    });
                }

                client.SendMessageBatchAsync(batchRequest).Wait();
            }
        }

        public byte[] GetPicture(string fileName)
        {
            byte[] responseBody;

            using (IAmazonS3 client = new AmazonS3Client(_accessKeyId, _secretAccessKey, AmazonRegion.USWest2))
            {
                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileName
                };

                using (GetObjectResponse response = client.GetObjectAsync(request).Result)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        response.ResponseStream.CopyTo(memoryStream);
                        responseBody = memoryStream.ToArray();
                    }
                }
            }
            return responseBody;
        }

        public void TransformFiles()
        {
            using (var s3client = new AmazonS3Client(_accessKeyId, _secretAccessKey, AmazonRegion.USWest2))
            using (var client = new AmazonSQSClient(_accessKeyId, _secretAccessKey, AmazonRegion.USWest2))
            {

                ReceiveMessageResponse response;
                do
                {
                    var recieveMessageRequest = new ReceiveMessageRequest();
                    recieveMessageRequest.QueueUrl = _queueUrl;
                    recieveMessageRequest.MessageAttributeNames = new List<string> { "fileName", "transformation", "date" };
                    recieveMessageRequest.MaxNumberOfMessages = 10;

                    response = client.ReceiveMessageAsync(recieveMessageRequest).Result;
                    foreach (var message in response.Messages)
                    {
                        var fileName = message.MessageAttributes["fileName"].StringValue;
                        var transformation = Enum.Parse(typeof(Transformation), message.MessageAttributes["transformation"].StringValue);

                        var picture = s3client.GetObjectAsync(_bucketName, fileName).Result;

                        using (var inStream = new MemoryStream())
                        {
                            picture.ResponseStream.CopyTo(inStream);
                            using (var outStream = new MemoryStream())
                            {
                                Image image = Image.FromStream(inStream);
                                image.RotateFlip(RotateFlipType.Rotate90FlipNone);

                                image.Save(outStream, ImageFormat.Jpeg);
                                
                                s3client.PutObjectAsync(new PutObjectRequest()
                                {
                                    BucketName = _bucketName,
                                    Key = fileName,
                                    InputStream = outStream
                                }).Wait();
                            }
                        }

                        client.DeleteMessageAsync(_queueUrl, message.ReceiptHandle);
                    }

                } while (response.Messages?.Count == 10);
            }
        }

        private string CalculateSignature(string policy)
        {
            HMAC hmacsha256 = new HMACSHA1(System.Text.Encoding.UTF8.GetBytes(_secretAccessKey));
            byte[] hashmessage = hmacsha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(policy));
            string signature = Convert.ToBase64String(hashmessage);

            return signature;
        }

        private string CalculatePolicy(string successUrl)
        {
            string policyJson =
                           @"{	
                                'expiration' : '2030-12-01T12:00:00.000Z',
	                            'conditions': [ 
    				                {'bucket': '" + _bucketName + @"'}, 
                                    {'content-type': 'image/jpeg'},
                                    {'success_action_redirect':'" + successUrl + @"'},
                                    {'acl': 'public-read'},
                                    ['starts-with', '$key', ''],
    				                ['content-length-range', 0, 1048576]
    				            ]
                            }";

            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(policyJson.Replace("\n", "").Replace("\r", ""))); ;
        }
    }

}

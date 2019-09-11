using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace S3Client
{
    public static class S3Helper
    {
        /// <summary>
        /// 移动文件
        /// </summary>
        /// <param name="S3Manager"></param>
        /// <param name="bucketName1"></param>
        /// <param name="bucketName2"></param>
        /// <param name="fileName1"></param>
        /// <param name="fileName2"></param>
        /// <returns></returns>
        public static ActionResult Move(IAmazonS3 S3Manager, string bucketName1, string fileName1, string bucketName2, string fileName2)
        {

            ActionResult ar = new ActionResult()
            {
                IsSuccess = false,
                Msg="Empty"
            };

            if (S3Manager != null)
            {
               

                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = bucketName1;
                request.Prefix = fileName1;

                ListObjectsResponse response = S3Manager.ListObjects(request);
                if (response.S3Objects.Count == 1)
                {
                    CopyObjectRequest copyObjectRequest = new CopyObjectRequest();
                    copyObjectRequest.SourceBucket = bucketName1;
                    copyObjectRequest.SourceKey = fileName1;
                    copyObjectRequest.DestinationBucket = bucketName2;
                    copyObjectRequest.DestinationKey = fileName2;
                    CopyObjectResponse res=  S3Manager.CopyObject(copyObjectRequest);
                    if(res.HttpStatusCode== HttpStatusCode.OK)
                    {
                        
                           S3Manager.DeleteObject(bucketName1, fileName1);
                       
                            ar.IsSuccess = true;
                            ar.Msg = "移动成功！";
                        
                    }
                    else
                    {
                        ar.IsSuccess = false;
                        ar.Msg = "复制失败！";
                    }
                }
                else
                {
                    ar.IsSuccess = false;
                    ar.Msg = "文件不存在！";
                }
              
            }



         
            return ar;
        }

        public static ActionResult Delete(IAmazonS3 S3Manager, string bucketName, string fileName)
        {

            ActionResult ar = new ActionResult()
            {
                IsSuccess = false,
                Msg = "Empty"
            };

            if (S3Manager != null)
            {


                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = bucketName;
                request.Prefix = fileName;

                ListObjectsResponse response = S3Manager.ListObjects(request);
                if (response.S3Objects.Count == 1)
                {
                    

                        S3Manager.DeleteObject(bucketName, fileName);

                        ar.IsSuccess = true;
                        ar.Msg = "删除成功！";

                   
                }
                else
                {
                    ar.IsSuccess = false;
                    ar.Msg = "文件不存在！";
                }

            }




            return ar;
        }

        public static ActionResult Upload(IAmazonS3 S3Manager, string bucketName, string fileName, string filePath,bool overlay = false)
        {
            PutObjectRequest request = new PutObjectRequest()
            {

                BucketName = bucketName,
                Key = fileName,
                FilePath=filePath
            };

            if (!overlay)
            {
                {
                    ListObjectsRequest req = new ListObjectsRequest();
                    req.BucketName = bucketName;
                    req.Prefix = fileName;

                    ListObjectsResponse res = S3Manager.ListObjects(req);
                    if (res.S3Objects.Count == 1)
                    {
                        return new ActionResult { IsSuccess = false, Msg = fileName + "已存在！" };
                    }
                }
            }
            PutObjectResponse response = S3Manager.PutObject(request);
            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                return new ActionResult { IsSuccess = true, Msg = "上传成功！" };
            }
            else
            {
                return new ActionResult { IsSuccess = false, Msg = fileName+"上传失败！" };
            }
        }

        public static bool Download(IAmazonS3 S3Manager, string bucketName, string fileName, string filePath)
        {
            GetObjectRequest request = new GetObjectRequest();
            request.BucketName = bucketName;
            request.Key = fileName;
            try
            {
                using (GetObjectResponse response = S3Manager.GetObject(request))

                {
                    //文件
                    response.WriteResponseStreamToFile(filePath);
                    return true;

                }

            }
            catch (AmazonClientException ex)
            {
                if (!ex.Message.Contains("Expected hash not equal to calculated hash"))
                {
                    return false;
                }
            }
            return false;
        }
        public static string RemoveWindowsFileNameSpicalChar(string str, char repChar = '-')
        {

            return str.Replace('\\', repChar).Replace('/', repChar).Replace(':', repChar).Replace('*', repChar).Replace('?', repChar).Replace('"', repChar).Replace('<', repChar).Replace('>', repChar).Replace('|', repChar);
        }

        public static string GetDataTime(long unixTimeStamp)
        {
            // long unixTimeStamp = 1478162177;
            //unixTimeStamp         1360395673.4587420 
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 当地时区
            DateTime dt = startTime.AddSeconds((double)unixTimeStamp / 10000000);
            // return dt.ToString("yyyy/MM/dd HH:mm:ss:ffff");
            return dt.ToString("yyyy/MM/dd HH:mm:ss");
        }

        public static string GetFileSize(long Size)
        {
            string m_strSize = "";
            long FactSize = 0;
            FactSize = Size;
            if (FactSize < 1024.00)
                m_strSize = DoubleToString(FactSize) + " B";
            else if (FactSize >= 1024.00 && FactSize < 1048576)
                m_strSize = DoubleToString(FactSize / 1024.00) + " KB";
            else if (FactSize >= 1048576 && FactSize < 1073741824)
                m_strSize = DoubleToString(FactSize / 1024.00 / 1024.00) + " MB";
            else if (FactSize >= 1073741824) m_strSize = DoubleToString(FactSize / 1024.00 / 1024.00 / 1024.00) + " GB";
            return m_strSize;

        }

        private static string DoubleToString(double data)
        {
            string s = data.ToString("F2");
            if (s.EndsWith(".00"))
            {
                s = s.Substring(0, s.Length - 3);
            }
            return s;
        }

        public static string GetFileType(string fileName)
        {
            if (fileName.Contains("."))
            {
                string[] strs = fileName.Split('.');
                if (strs.Length == 2)
                {
                    return strs[1] + "文件";
                }
                else
                {
                    return "其他文件";
                }
            }
            else
            {
                return "其他文件";
            }
        }
    }
}

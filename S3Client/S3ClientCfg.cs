using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace S3Client
{
   public class S3ClientCfg
    {
        public string Ak { get; set; }
        public string Sk { get; set; }
        public string Endpoint { get; set; }

        public string BucketName { get; set; }

        public int? DeleteAfterDays { get; set; }
    }
}

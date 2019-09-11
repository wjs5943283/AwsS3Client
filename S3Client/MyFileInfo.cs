using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace S3Client
{
  public  class MyFileInfo
    {

        public int Num { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public string StorageType { get; set; }

        public string FileSize { get; set; }

        public string CreateDate { get; set; }

        public string EndUser { get; set; }


    }
}

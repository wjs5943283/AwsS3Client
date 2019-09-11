using Amazon.S3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.Threading;
using Amazon.S3.Model;
using System.Net;
using Path = System.IO.Path;
using Amazon.Runtime;

namespace S3Client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen; //居中
            InitializeComponent();
        }

        private IAmazonS3 _client=null;
        private S3ClientCfg S3Cfg { get; set; }
      
     
        private string marker;
        private string fileSaveDir;
      
       
        private List<MyFileInfo> myFileInfoList;
        private string[] fileUploadFiles;
   
        private StringBuilder uploadResult;
     
        private string startWith;
        private bool progressbarNeedStop;
        private int num = 1;
        /// <summary>
        /// 连接获得 空间名
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnConnet_Click(object sender, RoutedEventArgs e)
        {
            ConnectServer();
        }

        private delegate void SetProgressBarHandle(int value);
        private void SetProgressBar(int val)
        {
            if (this.Dispatcher.Thread != System.Threading.Thread.CurrentThread)
            {
                this.Dispatcher.Invoke(new SetProgressBarHandle(this.SetProgressBar), val);
            }
            else
            {
                pb1.Value = val;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            pb1.Visibility = Visibility.Hidden;
            pb1.Maximum = 100;
            pb1.Value = 1;

           
          

            //从配置文件中载入Ak和Sk（S3ClientCfg.Json）
            if (File.Exists("S3ClientCfg.Json"))
            {
                string json = File.ReadAllText("S3ClientCfg.Json");
                S3Cfg = JsonConvert.DeserializeObject<S3ClientCfg>(json);
                if (S3Cfg != null)
                {
                    TxtAK.Text = S3Cfg.Ak;
                    TxtSk.Text = S3Cfg.Sk;
                    if (!string.IsNullOrWhiteSpace(TxtAK.Text.Trim()))
                    {
                        string ak = TxtAK.Text.Trim();
                        if (ak.Length >= 40 && !ak.Contains("*"))
                        {
                            TxtAK.Text = ak.Substring(0, 4) + "********************************" + ak.Substring(ak.Length - 5, 4);
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(TxtSk.Text.Trim()))
                    {
                        string sk = TxtSk.Text.Trim();
                        if (sk.Length >= 40 && !sk.Contains("*"))
                        {
                            TxtSk.Text = sk.Substring(0, 4) + "********************************" + sk.Substring(sk.Length - 5, 4);
                        }
                    }



                    TxtEndpoint.Text = S3Cfg.Endpoint;
                   
                }
            }
            else
            {
                S3Cfg = new S3ClientCfg { DeleteAfterDays = 365 };
                TxtAK.Text = "";
                TxtSk.Text = "";
                TxtEndpoint.Text = "";
              

            }
            if (!string.IsNullOrWhiteSpace(TxtAK.Text) && !string.IsNullOrWhiteSpace(TxtSk.Text)&& string.IsNullOrWhiteSpace(TxtEndpoint.Text))
            {
                ConnectServer();
            }


            marker = "";
        }

        private void ConnectServer()
        {

            if (string.IsNullOrWhiteSpace(TxtAK.Text) || string.IsNullOrWhiteSpace(TxtSk.Text) || string.IsNullOrWhiteSpace(TxtEndpoint.Text))
            {
                MessageBox.Show("AccessKey 、SecretKey 、Endpoint不能为空！");
                return;
            }

            if (SyncTargetBucketsComboBox.Items.Count > 0)
            {
                SyncTargetBucketsComboBox.ItemsSource = null;
                SyncTargetBucketsComboBox.Items.Clear();
            }

            if (!TxtAK.Text.Contains("*") && !TxtSk.Text.Contains("*"))
            {
                S3Cfg.Ak = TxtAK.Text.Trim();
                S3Cfg.Sk = TxtSk.Text.Trim();
            }

            S3Cfg.Endpoint = TxtEndpoint.Text.Trim();
            Amazon.S3.AmazonS3Config cofg = new AmazonS3Config();
            cofg.ServiceURL = S3Cfg.Endpoint;


            _client = new AmazonS3Client(S3Cfg.Ak, S3Cfg.Sk, cofg);




            this.SyncTargetBucketsComboBox.ItemsSource = null;
            BtnConnet.IsEnabled = false;


            //使用线程池
            ThreadPool.QueueUserWorkItem((state) =>
            {

                reloadBuckets();

            });

            LoadProgressBar();

            Thread.Sleep(10);
        }


        private void reloadBuckets()
        {
            if (_client == null)
            {
                return;
            }
            ListBucketsResponse response = _client.ListBuckets();
            if (response.Buckets.Count>0)
            {
                //todo:保存ak&sk

                if (File.Exists("S3ClientCfg.Json"))
                {
                    File.Delete("S3ClientCfg.Json");

                }



                string json = JsonConvert.SerializeObject(S3Cfg);
                File.WriteAllText("S3ClientCfg.Json", json);


               

                Dispatcher.Invoke(new Action(() =>
                {
                    List<string> bucketNames = new List<string>();

                    foreach (S3Bucket bucket in response.Buckets)
                    {
                        bucketNames.Add(bucket.BucketName);
                    }


                    this.SyncTargetBucketsComboBox.ItemsSource = bucketNames;
                    BtnConnet.IsEnabled = true;
                    pb1.Visibility = Visibility.Hidden;
                    if (!string.IsNullOrWhiteSpace(TxtAK.Text.Trim()))
                    {
                        string ak = TxtAK.Text.Trim();
                        if (ak.Length >= 40 && !ak.Contains("*"))
                        {
                            TxtAK.Text = ak.Substring(0, 4) + "********************************" + ak.Substring(ak.Length - 5, 4);
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(TxtSk.Text.Trim()))
                    {
                        string sk = TxtSk.Text.Trim();
                        if (sk.Length >= 40 && !sk.Contains("*"))
                        {
                            TxtSk.Text = sk.Substring(0, 4) + "********************************" + sk.Substring(sk.Length - 5, 4);
                        }
                    }
                    MessageBox.Show("连接成功！");
                }));
            }
            else
            {
                Dispatcher.Invoke(new Action(() =>
                {

                    BtnConnet.IsEnabled = true;
                    pb1.Visibility = Visibility.Hidden;

                    MessageBox.Show("连接失败！");
                }));
            }
            progressbarNeedStop = true;
        }

        /// <summary>
        /// 加载进度条
        /// </summary>
        private void LoadProgressBar()
        {
            pb1.Visibility = Visibility.Visible;
            progressbarNeedStop = false;
            ThreadPool.QueueUserWorkItem((state) =>
            {
                int i = 1;
                while (true)
                {

                    i++;
                    if (i == 100)
                    {
                        i = 1;
                    }
                    SetProgressBar(i);
                    Thread.Sleep(200);
                    if (progressbarNeedStop)
                    {
                        return;

                    }
                }

            });

        }

        private void btnGetNowVersion_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://raw.githubusercontent.com/wjs5943283/QiNiuBucketClient/master/S3Client.zip");

        }

        private void btnOpenSource_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/wjs5943283/QiNiuBucketClient");
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void btnBatchDownload_Click(object sender, RoutedEventArgs e)
        {
            DownLoad();
        }
        private void DownLoad()
        {
            //1.获得表中选中的数据
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }
            //2.选择下载保存的路径

            var sfd = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = @"选择保存位置",
                ShowNewFolderButton = true
            };


            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }


            fileSaveDir = sfd.SelectedPath;


            List<MyFileInfo> list = new List<MyFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                MyFileInfo info = (MyFileInfo)item;
                if (info != null)
                {
                    list.Add(info);
                }
            }

            if (list.Count > 0)
            {
                //执行批量下载方法
                //使用线程池
                btnBatchDownload.IsEnabled = false;

                ThreadPool.QueueUserWorkItem(state =>
                {
                    batchDownLoad(list);
                });
                LoadProgressBar();
                Thread.Sleep(10);
            }
            pb1.Visibility = Visibility.Hidden;

            btnBatchDownload.IsEnabled = true;
        }

        /// <summary>
        /// 批量下载
        /// </summary>
        /// <param name="qiNiuFileInfolist"></param>
        private void batchDownLoad(IEnumerable<MyFileInfo> list)
        {

            if (string.IsNullOrWhiteSpace(S3Cfg.BucketName))
            {
                return;
            }
              
                var rresult = new StringBuilder();

                foreach (var info in list)
                {
                   

                    string saveFile = Path.Combine(fileSaveDir, info.FileName.Replace('/', '-'));
                    if (File.Exists(saveFile))
                    {
                        saveFile = Path.Combine(fileSaveDir,
                            Path.GetFileNameWithoutExtension(info.FileName.Replace('/', '-')) + Guid.NewGuid() +
                            Path.GetExtension(info.FileName));
                    }

                GetObjectRequest request = new GetObjectRequest();
                request.BucketName = S3Cfg.BucketName;
                request.Key = info.FileName;
             
                try
                {
                    using (GetObjectResponse response = _client.GetObject(request))

                    {
                        //文件
                       response.WriteResponseStreamToFile(saveFile);

                    }

                }
                catch (AmazonClientException ex)
                {
                    if (!ex.Message.Contains("Expected hash not equal to calculated hash"))
                    {
                        rresult.AppendLine(info.FileName + ":下载失败！");
                    }
                }

                

            }
            MessageBox.Show(string.IsNullOrWhiteSpace(rresult.ToString()) ? "下载结束！" : rresult.ToString());
                progressbarNeedStop = true;

          

        }


        private void btnBatchDel_Click(object sender, RoutedEventArgs e)
        {
            Delete(); 
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new System.Windows.Forms.OpenFileDialog
            {
                Multiselect = true,
                Title = @"选择上传文件"
            };

            if (ofd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            uploadResult = new StringBuilder();

            fileUploadFiles = ofd.FileNames;

            if (fileUploadFiles.Length <= 0) return;


            bool? result;

          
            if (fileUploadFiles.Length > 100)
            {
                MessageBox.Show("每次上传文件不得大于100个");
                return;
            }
            foreach (string file in fileUploadFiles)
            {
                var fileInfo = new System.IO.FileInfo(file);

                if (fileInfo.Length > 1024 * 1024 * 100)
                {
                    MessageBox.Show("单个文件大小不得大于100M");
                    return;
                }
            }

            btnUpload.Content = "正在上传......";
            btnUpload.IsEnabled = false;
            result = true;

           
                LoadProgressBar();
                Dispatcher.Invoke(new Action(() =>
                {


                    foreach (string file in fileUploadFiles)
                    {
                        var uploadRes = S3Helper.Upload(_client, S3Cfg.BucketName, Path.GetFileName(file), file, cbOverlay.IsChecked == true);
                        result = result & uploadRes.IsSuccess;
                        if (!uploadRes.IsSuccess)
                        {
                            uploadResult.Append(uploadRes.Msg);
                        }
                    }
                    pb1.Visibility = Visibility.Hidden;
                }));
            if (result==true)
            {
                MessageBox.Show("上传成功!");
            }
            else
            {
                MessageBox.Show(uploadResult.ToString());
            }
            btnUpload.Content = "上传";
            btnUpload.IsEnabled = true;
            Search();

        }

        private void MiRefresh_Click(object sender, RoutedEventArgs e)
        {
            Search();
        }

        private void MiPreview_Click(object sender, RoutedEventArgs e)
        {
            Preview();
        }
        private void Preview()
        {
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }

            List<MyFileInfo> list = new List<MyFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                MyFileInfo info = (MyFileInfo)item;
                if (info != null)
                {
                    list.Add(info);
                }
            }
            if (list.Count > 0)
            {
            
                 
                   
                    string tempfile = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), S3Helper.RemoveWindowsFileNameSpicalChar(list[0].FileName));

                    
                    System.Threading.ThreadPool.QueueUserWorkItem((state) =>
                    {

                      bool res=  S3Helper.Download(_client, S3Cfg.BucketName, list[0].FileName, tempfile);
                       
                        Dispatcher.Invoke(new Action(() =>
                        {
                            if(res) System.Diagnostics.Process.Start(tempfile);
                        }));
                    });

                
              

            }
        }
        private void MiDownload_Click(object sender, RoutedEventArgs e)
        {
            DownLoad();
        }

        private void MiReName_Click(object sender, RoutedEventArgs e)
        {
            ReName();
        }
        private void ReName()
        {
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }

            List<MyFileInfo> list = new List<MyFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                MyFileInfo info = (MyFileInfo)item;
                if (info != null)
                {
                    list.Add(info);
                }
            }
            if (list.Count > 0)
            {

                RenameWindow rw = new RenameWindow()
                {
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    FileName = list[0].FileName,
                    S3Manager = _client,
                    Bucket = S3Cfg.BucketName
                };

                rw.ShowDialog();
                Search();
            }
        }


        private void Search()
        {
            if (btnSearch.IsEnabled == false)
            {
                return;
            }

            btnSearch.IsEnabled = false;



            if (string.IsNullOrWhiteSpace(marker))
            {
                num = 1;
                myFileInfoList = new List<MyFileInfo>();

            }

            if(!string.IsNullOrWhiteSpace(SyncTargetBucketsComboBox.Text))
            S3Cfg.BucketName = SyncTargetBucketsComboBox.Text;
            startWith = txtStartWith.Text.Trim();
            ThreadPool.QueueUserWorkItem((state) =>
            {
                //ListResult listResult = bucketManager.ListFiles(bucket, startWith, marker, 5000, "");
                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = S3Cfg.BucketName;
                request.Marker = marker;
                request.Prefix = startWith;
                request.MaxKeys = 1000;
               // request.Delimiter = null;
                ListObjectsResponse response = _client.ListObjects(request);


                Dispatcher.Invoke(new Action(() =>
                {
                    if (response.HttpStatusCode == HttpStatusCode.OK && response.S3Objects.Count > 0)
                    {
                        marker = response.NextMarker;
                        foreach (S3Object item in response.S3Objects)
                        {
                            // item.EndUser
                            var f = new MyFileInfo
                            {

                                FileName = item.Key,
                                FileType = S3Helper.GetFileType(item.Key),
                                StorageType = item.StorageClass.Value,
                                FileSize = S3Helper.GetFileSize(item.Size),
                                EndUser = item.Owner.DisplayName,
                                CreateDate = item.LastModified.ToString("yyyy/MM/dd HH:mm:ss")
                            };
                            myFileInfoList.Add(f);

                        }

                        if (myFileInfoList.Count > 0)
                        {
                            //dgResult.ItemsSource = !string.IsNullOrWhiteSpace(txtEndWith.Text)
                            //    ? qiNiuFileInfoList.Where(f => f.FileName.EndsWith(txtEndWith.Text.Trim()))
                            //    : qiNiuFileInfoList;
                            var list = myFileInfoList;


                            if (!string.IsNullOrWhiteSpace(txtEndWith.Text))
                            {
                                list = myFileInfoList.Where(f => f.FileName.EndsWith(txtEndWith.Text.Trim())).ToList();

                            }
                            if (list.Count > 0)
                            {
                                // dgResult.ItemsSource = list.OrderBy(t => t.CreateDate).ToList();
                                num = 1;
                                list = list.OrderByDescending(t => t.CreateDate).ToList();
                                foreach (var s in list)
                                {
                                    s.Num = num++;
                                }
                                dgResult.ItemsSource = list;
                            }
                            else
                            {
                                dgResult.ItemsSource = new List<MyFileInfo>();
                            }
                            //  dgResult.ItemsSource = list;

                        }
                        else
                        {

                            marker = string.Empty;
                            dgResult.ItemsSource = new List<MyFileInfo>();
                        }

                        btnSearch.IsEnabled = true;
                    }
                }));
            });











        }

        private void Delete()
        {
            //1.获得表中选中的数据
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }

            List<MyFileInfo> list = new List<MyFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                MyFileInfo info = (MyFileInfo)item;
                if (info != null)
                {
                    list.Add(info);
                }
            }
            if (list.Count > 0)
            {
                string msg = string.Join(",\r\n", list.Select(q => q.FileName));
                MessageBoxResult confirmToDel = MessageBox.Show("确认要删除所选行吗？\r\n" + msg, "提示", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirmToDel != MessageBoxResult.Yes)
                {
                    return;
                }


                //执行批量删除
                List<string> ops = new List<string>();
                foreach (var key in list)
                {
                   S3Helper.Delete(_client,S3Cfg.BucketName,key.FileName);
                   
                }


                Search();
                Thread.Sleep(10);
            }
        }

        private void MiDelete_Click(object sender, RoutedEventArgs e)
        {
            Delete();
        }

        private void MISelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }
            dgResult.SelectAll();
        }

        private void MICopyFileName_Click(object sender, RoutedEventArgs e)
        {
            if (dgResult.ItemsSource == null && dgResult.SelectedItems.Count <= 0)
            {
                return;

            }

            List<MyFileInfo> list = new List<MyFileInfo>();
            foreach (var item in dgResult.SelectedItems)
            {
                MyFileInfo info = (MyFileInfo)item;
                if (info != null)
                {
                    list.Add(info);
                }
            }
            if (list.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var qiNiuFileInfo in list)
                {
                    sb.AppendLine(qiNiuFileInfo.FileName);
                }
                if (!string.IsNullOrWhiteSpace(sb.ToString()))
                {
                    Clipboard.SetText(sb.ToString());
                }
            }
        }



       

       

        /// <summary>
        /// 选择bucket
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SyncTargetBucketsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            num = 1;
            marker = "";
            btnSearch.Content = "查询";
            btnSearch.IsEnabled = true;
            btnUpload.IsEnabled = true;
            btnBatchDel.IsEnabled = true;
            btnBatchDownload.IsEnabled = true;
            if (!string.IsNullOrWhiteSpace(SyncTargetBucketsComboBox.Text))
                S3Cfg.BucketName = SyncTargetBucketsComboBox.Text;
            myFileInfoList = new List<MyFileInfo>();

        }
    }
}

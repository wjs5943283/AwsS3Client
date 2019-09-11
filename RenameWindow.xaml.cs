using Amazon.S3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;



namespace S3Client
{
    /// <summary>
    /// RenameWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RenameWindow : Window
    {
        public RenameWindow()
        {
            InitializeComponent();
        }

        public string FileName { get; set; }
        public IAmazonS3 S3Manager { get; set; }

        public string Bucket;

        private void btnRename_Click(object sender, RoutedEventArgs e)
        {
            if (S3Manager != null && !string.IsNullOrWhiteSpace(Bucket) && !string.IsNullOrWhiteSpace(FileName) &&
                !string.IsNullOrWhiteSpace(txtRename.Text.Trim()))
            {
               var res= S3Helper.Move(S3Manager, Bucket, FileName, Bucket, txtRename.Text.Trim());
                if (!res.IsSuccess)
                {
                    MessageBox.Show(res.Msg);
                }
            }
        this.Close();
          
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtRename.Text = FileName;
        }
    }
}

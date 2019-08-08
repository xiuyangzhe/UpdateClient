using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using UpdateClient.ServiceUpdate;

namespace UpdateClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                //检测有无更新文件，需要更新显示详情，不更新退出更新程序启动应用主程序
                var checkcount = CheckUpdate();
                if (checkcount > 0)//显示详情
                {

                    KillProcess(appname);

                    InitializeComponent();

                    foreach (var file in updatefiles)
                    {
                        var label = new Label();
                        label.Content = System.IO.Path.Combine(file.FilePath, file.FileName);

                        mainpanel.Children.Add(label);
                    }

                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    this.Topmost = true;
                }
                else//退出
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                var result = MessageBox.Show(ex.Message);
                if (result == MessageBoxResult.OK)
                {
                    this.Close();
                }
            }
        }

        private List<FileInfomation> updatefiles = new List<FileInfomation>();//需要更新的文件集合
        private string appname = ConfigurationManager.AppSettings["AppName"];//启动主程序名称

        /// <summary>
        /// 在线检测服务器与客户端文件对比
        /// </summary>
        /// <returns>需要更新的文件个数</returns>
        private int CheckUpdate()
        {
            var updateclient = new UpdateServiceClient();
            var localfiles = GetAllFiles();
            var result = updateclient.Update(ref localfiles);
            if (result)
            {
                updatefiles = localfiles;
                return localfiles.Count;
            }
            else
                return 0;
        }

        /// <summary>
        /// 确认按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void confirm_btn_Click(object sender, RoutedEventArgs e)
        {
            var UpdateFilesDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UpdateFiles");
            Directory.Exists(UpdateFilesDir);
            {
                Directory.CreateDirectory(UpdateFilesDir);
            }

            foreach (var file in updatefiles)
            {
                var updatepath = System.IO.Path.Combine(UpdateFilesDir, file.FilePath, file.FileName);
                var apppath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file.FilePath, file.FileName);

                var lastupdatefolderindex = updatepath.LastIndexOf("\\");
                var lastappfolderindex = apppath.LastIndexOf("\\");
                var updatefolder = updatepath.Remove(lastupdatefolderindex, updatepath.Length - lastupdatefolderindex);
                var appfolder = apppath.Remove(lastappfolderindex, apppath.Length - lastappfolderindex);
                Directory.CreateDirectory(updatefolder);
                Directory.CreateDirectory(appfolder);

                File.WriteAllBytes(updatepath, file.Filebody);

                if (File.Exists(apppath))
                    File.Delete(apppath);
                File.Move(updatepath, apppath);
            }

            Process.Start(appname);

            this.Close();
        }


        /// <summary>
        /// 获取所有文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<FileInfomation> GetAllFiles()
        {

            var files = new List<FileInfomation>();
            // 获取程序的基目录。
            var basepath = System.AppDomain.CurrentDomain.BaseDirectory;
            var xmldoc = new XmlDocument();
            xmldoc.Load(basepath + "UpdateFiles.xml");
            var nodes = xmldoc.SelectNodes("root/UpdateFiles/File");
            foreach (XmlElement node in nodes)
            {
                var fileinfo = new FileInfomation();
                fileinfo.FilePath = node.GetAttribute("Path");
                fileinfo.Version = node.GetAttribute("Version");
                fileinfo.FileName = node.InnerText;
                files.Add(fileinfo);
            }

            return files;
        }

        /// <summary>
        /// 关闭进程
        /// </summary>
        /// <param name="processName"></param>
        public static void KillProcess(string processName)
        {
            Process[] ps = Process.GetProcesses();
            foreach (Process item in ps)
            {
                if (item.ProcessName == processName)
                {
                    item.Kill();
                }
            }
        }

    }
}

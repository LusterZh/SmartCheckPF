using Ezhu.AutoUpdater.utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Ezhu.AutoUpdater.UI
{
    public partial class DownFileProcess : WindowBase
    {
        private string updateFileDir;//更新文件存放的文件夹
        private string callExeName;
        private string appDir;
        private string appName;
        private string appVersion;
        private string desc;
        public static string IP = IniUtils.IniReadValue("Station Configuration", "IP", @"C:\config\config.ini");

        public DownFileProcess(string callExeName, string updateFileDir, string appDir, string appName, string appVersion, string desc)
        {
            InitializeComponent();
            Topmost = true;
            this.Loaded += (sl, el) =>
            {
                YesButton.Content = "现在更新";
                //NoButton.Content = "暂不更新";

                this.YesButton.Click += (sender, e) =>
                {
                    this.YesButton.IsEnabled = false;
                    Process[] processes = Process.GetProcessesByName(this.callExeName);

                    if (processes.Length > 0)
                    {
                        foreach (var p in processes)
                        {
                            p.Kill();
                        }
                    }

                    Process[] pr = Process.GetProcessesByName("ZbarDecode");
                    if (pr.Length > 0)
                    {
                        foreach (var p in pr)
                        {
                            p.Kill();
                        }
                    }

                    Process[] pr1 = Process.GetProcessesByName("python");
                    if (pr1.Length > 0)
                    {
                        foreach (var p in pr1)
                        {
                            p.Kill();
                        }
                    }

                    DownloadUpdateFile();
                };

                //this.NoButton.Click += (sender, e) =>
                //{
                //    this.Close();
                //};

                this.txtProcess.Text = this.appName + "发现新的版本(" + this.appVersion + "),是否现在更新?";
                txtDes.Text = this.desc;
            };
            this.callExeName = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(callExeName));
            this.updateFileDir = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(updateFileDir));
            this.appDir = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(appDir));
            this.appName = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(appName));
            this.appVersion = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(appVersion));

            string sDesc = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(desc));
            if (sDesc.ToLower().Equals("null"))
            {
                this.desc = "";
            }
            else
            {
                this.desc = "更新内容如下:\r\n" + sDesc;
            }
        }

        public void DownloadUpdateFile()
        {
            string url = Constants.RemoteUrl + "/update.zip";
            var client = new System.Net.WebClient();
            client.DownloadProgressChanged += (sender, e) =>
            {
                UpdateProcess(e.BytesReceived, e.TotalBytesToReceive);
            };
            client.DownloadDataCompleted += (sender, e) =>
            {
                string zipFilePath = System.IO.Path.Combine(updateFileDir, "update.zip");
                byte[] data = e.Result;
                BinaryWriter writer = new BinaryWriter(new FileStream(zipFilePath, FileMode.OpenOrCreate));
                writer.Write(data);
                writer.Flush();
                writer.Close();

                System.Threading.ThreadPool.QueueUserWorkItem((s) =>
                {
                    Action f = () =>
                    {
                       txtProcess.Text = "开始更新程序...";
                    };
                    this.Dispatcher.Invoke(f);

                    string tempDir = System.IO.Path.Combine(updateFileDir, "temp");
                    if (!Directory.Exists(tempDir))
                    {
                        Directory.CreateDirectory(tempDir);
                    }
                    UnZipFile(zipFilePath, tempDir);

                    //写入文件路径
                    //IniUtils.IniReadValue("Station Configuration", "FilePath", @"C:\config\config.ini");
                    IniUtils.IniWriteValue("Station Configuration", "FilePath", tempDir, @"C:\config\config.ini");

                    ////移动文件
                    string tempUpdate = System.IO.Path.Combine(tempDir, "update");
                    ///
                    if (Directory.Exists(tempDir))
                    {
                        if(Directory.Exists(tempUpdate))
                        {
                            CopyDir(tempUpdate, appDir);
                        }
                        else
                        {
                            CopyDir(tempDir, appDir);
                        }
                    }

                    f = () =>
                    {
                        txtProcess.Text = "更新完成!";

                        try
                        {
                            //清空缓存文件夹
                            //string rootUpdateDir = updateFileDir.Substring(0, updateFileDir.LastIndexOf(System.IO.Path.DirectorySeparatorChar));
                            //IniUtils.IniWriteValue("Station Configuration", "FilePath2", rootUpdateDir, @"C:\config\config.ini");
                            //foreach (string p in System.IO.Directory.EnumerateDirectories(rootUpdateDir))
                            //{
                            //    if (!p.ToLower().Equals(updateFileDir.ToLower()))
                            //    {
                            //        System.IO.Directory.Delete(p, true);
                            //    }

                            //}
                            //清空缓存文件夹
                            string TempPath = System.IO.Path.Combine(appDir.Substring(0, appDir.LastIndexOf(System.IO.Path.DirectorySeparatorChar)), "Update");
                            //IniUtils.IniWriteValue("Station Configuration", "FilePath1", TempPath, @"C:\config\config.ini");
                            DeleteDir(TempPath);
                        }
                        catch (Exception ex)
                        {
                            //MessageBox.Show(ex.Message);
                        }

                    };
                    this.Dispatcher.Invoke(f);

                    try
                    {
                        f = () =>
                        {
                            AlertWin alert = new AlertWin("更新完成,是否现在启动软件?") { WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this };
                            alert.Title = "更新完成";
                            alert.Loaded += (ss, ee) =>
                            {
                                alert.YesButton.Width = 40;
                                alert.NoButton.Width = 40;
                            };
                            alert.Width=300;
                            alert.Height = 200;
                            alert.ShowDialog();
                            if (alert.YesBtnSelected)
                            {
                                //启动软件
                                string exePath = System.IO.Path.Combine(appDir, callExeName + ".exe");
                                var info = new System.Diagnostics.ProcessStartInfo(exePath);
                                info.UseShellExecute = true;
                                info.WorkingDirectory = appDir;// exePath.Substring(0, exePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar));
                                System.Diagnostics.Process.Start(info);
                            }
                            else
                            {
 
                            }
                            //清空缓存文件夹
                            string TempPath = System.IO.Path.Combine(appDir.Substring(0, appDir.LastIndexOf(System.IO.Path.DirectorySeparatorChar)), "Update");
                            //IniUtils.IniWriteValue("Station Configuration", "FilePath1", TempPath, @"C:\config\config.ini");
                            DeleteDir(TempPath);
                            this.Close();
                        };
                        this.Dispatcher.Invoke(f);
                    }
                    catch (Exception ex)
                    {
                        //MessageBox.Show(ex.Message);
                    }
                });

            };
            client.DownloadDataAsync(new Uri(url));
        }

        private static void UnZipFile(string zipFilePath, string targetDir)
        {
            ICCEmbedded.SharpZipLib.Zip.FastZipEvents evt = new ICCEmbedded.SharpZipLib.Zip.FastZipEvents();
            ICCEmbedded.SharpZipLib.Zip.FastZip fz = new ICCEmbedded.SharpZipLib.Zip.FastZip(evt);
            fz.ExtractZip(zipFilePath, targetDir, "");
        }

        public void UpdateProcess(long current, long total)
        {
            string status = (int)((float)current * 100 / (float)total) + "%";
            this.txtProcess.Text = status;
            rectProcess.Width = ((float)current / (float)total) * bProcess.ActualWidth;
        }

        public void CopyDirectory(string sourceDirName, string destDirName)
        {
            try
            {
                if (!Directory.Exists(destDirName))
                {
                    Directory.CreateDirectory(destDirName);
                    File.SetAttributes(destDirName, File.GetAttributes(sourceDirName));
                }
                if (destDirName[destDirName.Length - 1] != Path.DirectorySeparatorChar)
                    destDirName = destDirName + Path.DirectorySeparatorChar;
                string[] files = Directory.GetFiles(sourceDirName);
                foreach (string file in files)
                {
                    File.Copy(file, destDirName + Path.GetFileName(file), true);
                    File.SetAttributes(destDirName + Path.GetFileName(file), FileAttributes.Normal);
                }
                string[] dirs = Directory.GetDirectories(sourceDirName);
                foreach (string dir in dirs)
                {
                    CopyDirectory(dir, destDirName + Path.GetFileName(dir));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("复制文件错误");
            }
        }

        public void CopyDir(string srcPath, string aimPath)
        {
            try
            {
                // 检查目标目录是否以目录分割字符结束如果不是则添加
                if (aimPath[aimPath.Length - 1] != System.IO.Path.DirectorySeparatorChar)
                {
                    aimPath += System.IO.Path.DirectorySeparatorChar;
                }
                // 判断目标目录是否存在如果不存在则新建
                if (!System.IO.Directory.Exists(aimPath))
                {
                    System.IO.Directory.CreateDirectory(aimPath);
                }
                // 得到源目录的文件列表，该里面是包含文件以及目录路径的一个数组
                // 如果你指向copy目标文件下面的文件而不包含目录请使用下面的方法
                // string[] fileList = Directory.GetFiles（srcPath）；
                string[] fileList = System.IO.Directory.GetFileSystemEntries(srcPath);
                // 遍历所有的文件和目录
                foreach (string file in fileList)
                {
                    // 先当作目录处理如果存在这个目录就递归Copy该目录下面的文件
                    if (System.IO.Directory.Exists(file))
                    {
                        CopyDir(file, aimPath + System.IO.Path.GetFileName(file));
                    }
                    // 否则直接Copy文件
                    else
                    {
                        System.IO.File.Copy(file, aimPath + System.IO.Path.GetFileName(file), true);
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        //删除文件夹
        public bool DeleteDir(string file)
        {

            try
            {
                //去除文件夹和子文件的只读属性
                //去除文件夹的只读属性
                System.IO.DirectoryInfo fileInfo = new DirectoryInfo(file);
                fileInfo.Attributes = FileAttributes.Normal & FileAttributes.Directory;

                //去除文件的只读属性
                System.IO.File.SetAttributes(file, System.IO.FileAttributes.Normal);

                //判断文件夹是否还存在
                if (Directory.Exists(file))
                {

                    foreach (string f in Directory.GetFileSystemEntries(file))
                    {

                        if (File.Exists(f))
                        {
                            //如果有子文件删除文件
                            File.Delete(f);
                            Console.WriteLine(f);
                        }
                        else
                        {
                            //循环递归删除子文件夹
                            DeleteDir(f);
                        }

                    }

                    //删除空文件夹

                    Directory.Delete(file);

                }
                return true;

            }
            catch (Exception ex) // 异常处理
            {
                return false;
            }

        }
    }



}


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace samrtCheckPF.utils
{
    class FileUtils
    {
        /// <summary>
        /// 将文件拷贝到某个文件夹下
        /// </summary>
        /// <param name="srcFile"></param> 源文件
        /// <param name="destFolder"></param> 目标文件夹
        public static void copyFiles(string srcFile, string destFolder)
        {
            FileInfo file = new FileInfo(srcFile);
            if (!Directory.Exists(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }
            string destFilePath = destFolder + file.Name;
            if (File.Exists(destFilePath))
            {
                File.Delete(destFilePath);
            }
            file.CopyTo(Path.Combine(destFolder, file.Name));
        }

        /// <summary>
        /// 将文件拷贝到某个文件夹下
        /// </summary>
        /// <param name="srcFile"></param> 源文件
        /// <param name="destFolder"></param> 目标文件夹
        public static void copyGoldSampleFile(string srcFile, string destFolder)
        {
            FileInfo file = new FileInfo(srcFile);
            if (!Directory.Exists(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }
            string destFilePath = destFolder + "Reg_golden.jpg";
            if (File.Exists(destFilePath))
            {
                File.Delete(destFilePath);
            }
            file.CopyTo(Path.Combine(destFolder, "Reg_golden.jpg"));
        }


        /// <summary>
        /// 判断文件夹里的文件数是否为空
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns> 返回文件夹里的文件数
        public static int isEmptyFolder(string folder)
        {
            return Directory.GetFiles(folder).Length;
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="fileName"></param>
        public static void deleteFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }

        public static string[] getFileList(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Directory.GetFiles(folder);
        }
        public static string getFileNameByPath(string path)
        {
            if (File.Exists(path))
            {
                FileInfo file = new FileInfo(path);
                return file.Name;
            }
            return null;
        }

        public static void DelectDir(string srcPath)
        {
            if (!Directory.Exists(srcPath))
            {
                return;
            }

            try
            {
                ////定义一个DirectoryInfo对象  
                //DirectoryInfo di = new DirectoryInfo(srcPath);
                //List<FileInfo> ls = new List<FileInfo>();
                ////通过GetFiles方法,获取di目录中的所有文件
                //foreach (FileInfo fi in di.GetFiles())
                //{
                //    ls.Add(fi);
                //}
                //ls.OrderBy(p => p.CreationTime);//时间从大到小排列
                //int deleteIndex = ls.Count / 2;//删除一半
                //for (int i = deleteIndex; i < ls.Count; i++)
                //{
                //    File.Delete(ls[i].FullName);
                //}

                DirectoryInfo dir = new DirectoryInfo(srcPath);
                FileSystemInfo[] fileinfo = dir.GetFileSystemInfos();  //返回目录中所有文件和子目录

                foreach (FileSystemInfo i in fileinfo)
                {
                    if (i is DirectoryInfo)            //判断是否文件夹
                    {
                        DirectoryInfo subdir = new DirectoryInfo(i.FullName);
                        subdir.Delete(true);          //删除子目录和文件
                    }
                    else
                    {
                        File.Delete(i.FullName);      //删除指定文件
                    }
                }
            }
            catch (Exception e)
            {
                //throw;
            }
        }

        public static void deleteFolder(string folder)
        {

            try
            {
                //去除文件夹和子文件的只读属性
                //去除文件夹的只读属性
                //System.IO.DirectoryInfo fileInfo = new DirectoryInfo(folder);
                //fileInfo.Attributes = FileAttributes.Normal & FileAttributes.Directory;

                //去除文件的只读属性
                //System.IO.File.SetAttributes(folder, System.IO.FileAttributes.Normal);

                //判断文件夹是否还存在
                if (Directory.Exists(folder))
                {
                    foreach (string f in Directory.GetFileSystemEntries(folder))
                    {
                        if (File.Exists(f))
                        {
                            //如果有子文件删除文件
                            File.Delete(f);
                        }
                        else
                        {
                            //循环递归删除子文件夹
                            deleteFolder(f);
                        }
                    }
                    //删除空文件夹
                    Directory.Delete(folder);
                }

            }
            catch (Exception ex) // 异常处理
            {
                LogUtils.e("FileUtils deleteFolder" +folder+" exception:"+ ex.Message.ToString());// 异常信息
            }

        }

        public static bool renameFile(string imgPath,string newPath)
        {
            try
            {
                File.Move(imgPath, newPath);
            }
            catch (Exception e)
            {
                LogUtils.d("renameFile error: " + e.Message);
                return false;
            }
            
            return true;
        }
    }


    public class CStopWatch
    {
        double m_dStartTime = 0.0;    ///< 开始时间
        double m_dStopTime = 0.0;     ///< 停止时间 

        public CStopWatch()
        {
            Start();
        }

        /// <summary>
        /// 开始计数
        /// </summary>
        public void Start()
        {
            m_dStartTime = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// 停止计数
        /// </summary>
        /// <returns>时间差单位ms</returns>
        public double Stop()
        {
            m_dStopTime = Stopwatch.GetTimestamp();
            double theElapsedTime = ElapsedTime();

            m_dStartTime = m_dStopTime;
            return theElapsedTime;
        }


        /// <summary>
        /// 获取时间差
        /// </summary>
        /// <returns>时间差单位ms</returns>
        public double ElapsedTime()
        {
            m_dStopTime = Stopwatch.GetTimestamp();
            double dTimeElapsed = (m_dStopTime - m_dStartTime) * 1000.0;

            return dTimeElapsed / Stopwatch.Frequency;
        }


        /// <summary>
        /// 获取时间差
        /// </summary>
        /// <returns>时间差单位ms</returns>
        public double ElapsedTimeSecond()
        {
            m_dStopTime = Stopwatch.GetTimestamp();
            double dTimeElapsed = (m_dStopTime - m_dStartTime) * 1.0;

            return dTimeElapsed / Stopwatch.Frequency;
        }
    }
}

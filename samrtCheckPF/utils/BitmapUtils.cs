using samrtCheckPF.json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace samrtCheckPF.utils
{
    class BitmapUtils
    {
        //裁剪Bitmap

        public static Bitmap crop(string imgPath,string saveFolder, string filename, MyRect rect)
        {
            try
            {
                Bitmap oriBmp = new Bitmap(imgPath);
                Rectangle cropRect = new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
                Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

                using (Graphics g = Graphics.FromImage(target))
                {
                    g.DrawImage(oriBmp, new Rectangle(0, 0, target.Width, target.Height),
                          cropRect,
                          GraphicsUnit.Pixel);
                }
                if (!Directory.Exists(saveFolder))
                {
                    Directory.CreateDirectory(saveFolder);
                }
                target.Save(saveFolder + filename);
                return target;
            }
            catch(Exception ex)
            {
                return null;
            }
          
        }

        //
        //public void saveBitmapFile(Bitmap bitmap,string path,string name)

        //{
        //    File file = new File("/mnt/sdcard/pic/01.jpg");//将要保存图片的路径
        //    try

        //    {
        //        BufferedOutputStream bos = new BufferedOutputStream(new FileOutputStream(file));
        //        bitmap.compress(Bitmap.CompressFormat.JPEG, 100, bos);
        //        bos.flush();
        //        bos.close();
        //    }

        //    catch (IOException e)

        //    {
        //        e.printStackTrace();
        //    }
        //}
    }
}

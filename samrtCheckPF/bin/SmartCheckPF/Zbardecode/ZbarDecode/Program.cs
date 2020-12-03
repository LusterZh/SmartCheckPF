
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZBar;


namespace ZbarDecode
{
    class Program
    {
        public static TCPClient m_pTCPClient = new TCPClient();

        static void Main(string[] args)
        {
            if (m_pTCPClient.ConnectSever("127.0.0.1", "20000"))
                m_pTCPClient.ReceiveMessage += new DelegateReceiveMessage(_receiveMsg);
        }


        //收到信息
        public static void _receiveMsg(string Message, int length)
        {
            if (length != 0)
            {
                Console.WriteLine(Message);
                if (Message =="error")
                {
                    m_pTCPClient.SendMessage("CodeError");
                }
                else
                {
                    try
                    {
                        System.Drawing.Image pImg = System.Drawing.Image.FromFile(AppDomain.CurrentDomain.BaseDirectory + "cutimage.jpg");
                        Bitmap bmp = MakeGrayscale3((Bitmap)pImg);
                        if(bmp.Width >= 400 || bmp.Height>=400)
                        {
                            OpenCVToResize(bmp, 1);
                        }
                        else
                        {
                            OpenCVToResize(bmp, 2);
                        }                        
                        pImg.Dispose();
                    }
                    catch
                    {
                        m_pTCPClient.SendMessage("CodeError");
                    }
                }
            }
            else
            {
                m_pTCPClient.ReceiveMessage -= new DelegateReceiveMessage(_receiveMsg);
            }
        }

        public static void  OpenCVToResize(Bitmap ImageOriginal, int ZoomNumber)
        {
            string result = string.Empty;

            if (ZoomNumber <= 0)
            {
                return;
            }

            System.Drawing.Bitmap ImageBaseOriginal = new System.Drawing.Bitmap(ImageOriginal);
            //Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(ImageBaseOriginal);

            //OpenCvSharp.Size size = new OpenCvSharp.Size(ImageBaseOriginal.Width * ZoomNumber, ImageBaseOriginal.Height * ZoomNumber);
            //Mat SizeMat = new Mat();
            //Cv2.Resize(mat, SizeMat, size);
            //Mat mask = SizeMat.CvtColor(ColorConversionCodes.BGR2GRAY);
            //InputArray kernel1 = InputArray.Create<float>(new float[3, 3] { { 0, -1, 0 }, { -1, 5, -1 }, { 0, -1, 0 } });
            //InputArray kernel2 = InputArray.Create<float>(new float[3, 3] { { -1, -1, -1 }, { -1, 9, -1 }, { -1, -1, -1 } });
            //InputArray kernel3 = InputArray.Create<float>(new float[3, 3] { { 1, -2, 1 }, { -2, 5, -2 }, { 1, -2, 1 } });
            //List<InputArray> list = new List<InputArray>();
            //list.Add(kernel1);
            //list.Add(kernel2);
            //list.Add(kernel3);

            //List<int> brightiness = new List<int>();
            //int dsj = 0, a = 10, b = 20, c = 30;
            //brightiness.Add(dsj);
            //brightiness.Add(a);
            //brightiness.Add(b);
            //brightiness.Add(c);
            //Mat FilterMat = new Mat();

            ////增强对比度
            //Mat ContrastMat = new Mat();

            //foreach (int x in brightiness)
            //{
            //    mask.ConvertTo(ContrastMat, -1, 1, x);

            //    foreach (InputArray inputa in list)
            //    {
            //        //滤波
            //        Cv2.Filter2D(ContrastMat, FilterMat, -1, inputa, new OpenCvSharp.Point(-1, -1), 0);

            //        ImageBaseOriginal = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(FilterMat);

            //        using (ZBar.ImageScanner scanner = new ZBar.ImageScanner())
            //        {
            //            scanner.SetConfiguration(ZBar.SymbolType.None, ZBar.Config.Enable, 0);
            //            scanner.SetConfiguration(ZBar.SymbolType.QRCODE, ZBar.Config.Enable, 1);
            //            scanner.SetConfiguration(ZBar.SymbolType.CODE128, ZBar.Config.Enable, 1);

            //            List<ZBar.Symbol> symbols = new List<ZBar.Symbol>();
            //            symbols = scanner.Scan((System.Drawing.Image)ImageBaseOriginal);
            //            if (symbols != null && symbols.Count > 0)
            //            {

            //                symbols.ForEach(s => result += s.Data);
            //                Console.WriteLine(result);
            //                break;
            //            }
            //        }
            //    }
            //    if (result != string.Empty)
            //    {
            //        break;
            //    }
            //}
            //mat.Dispose();
            //SizeMat.Dispose();
            //mask.Dispose();
            //if (result != string.Empty)
            //{
            //    m_pTCPClient.SendMessage(result);
            //    return;
            //}

            //自适应阈值检测
            if (result == string.Empty)
            {
                Mat mat1 = OpenCvSharp.Extensions.BitmapConverter.ToMat(ImageBaseOriginal);
                //转成灰度图
                Mat src_gray = mat1.CvtColor(ColorConversionCodes.BGR2GRAY);

                OpenCvSharp.Size size = new OpenCvSharp.Size(ImageBaseOriginal.Width * ZoomNumber, ImageBaseOriginal.Height * ZoomNumber);
                Mat SizeMat1 = new Mat();
                Cv2.Resize(src_gray, SizeMat1, size);

                //OpenCvSharp.Size size1 = new OpenCvSharp.Size(400, 400);
                //Mat SizeMat1 = new Mat();
                //Cv2.Resize(src_gray, SizeMat1, size1);
                List<int> BlockSizeList = new List<int>();

                BlockSizeList.Add(9);
                BlockSizeList.Add(11);
                BlockSizeList.Add(13);
                BlockSizeList.Add(15);
                BlockSizeList.Add(17);
                BlockSizeList.Add(21);
                BlockSizeList.Add(23);
                BlockSizeList.Add(25);
                BlockSizeList.Add(27);
                BlockSizeList.Add(31);
                BlockSizeList.Add(33);
                BlockSizeList.Add(35);
                BlockSizeList.Add(37);
                BlockSizeList.Add(39);
                BlockSizeList.Add(41);
                //BlockSizeList.Add(43);
                //BlockSizeList.Add(45);
                //BlockSizeList.Add(47);
                //BlockSizeList.Add(49);
                //BlockSizeList.Add(51);
                //BlockSizeList.Add(53);

                foreach (var blocksize in BlockSizeList)
                {
                    Mat SizeMatThreshold = new Mat();
                    Cv2.AdaptiveThreshold(SizeMat1, SizeMatThreshold, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, blocksize, 2);
                    System.Drawing.Bitmap bitmap12 = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(SizeMatThreshold); // mat 转 bitmap
                    Mat SizeMatThreshold1 = new Mat();
                    Cv2.AdaptiveThreshold(SizeMat1, SizeMatThreshold1, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, blocksize, 2);
                    System.Drawing.Bitmap bitmap13 = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(SizeMatThreshold1); // mat 转 bitmap
                    using (ZBar.ImageScanner scanner = new ZBar.ImageScanner())
                    {
                        scanner.SetConfiguration(ZBar.SymbolType.None, ZBar.Config.Enable, 0);
                        scanner.SetConfiguration(ZBar.SymbolType.QRCODE, ZBar.Config.Enable, 1);

                        List<ZBar.Symbol> symbols = new List<ZBar.Symbol>();
                        symbols = scanner.Scan((System.Drawing.Image)bitmap12);
                        if (symbols != null && symbols.Count > 0)
                        {
                            symbols.ForEach(s => result += s.Data);
                            Console.WriteLine(result);
                            break;
                        }
                        List<ZBar.Symbol> symbols1 = new List<ZBar.Symbol>();
                        symbols1 = scanner.Scan((System.Drawing.Image)bitmap13);
                        if (symbols1 != null && symbols1.Count > 0)
                        {
                            symbols1.ForEach(s => result += s.Data);
                            Console.WriteLine(result);
                            break;
                        }
                    }
                }
            }

            if (result != string.Empty)
            {
                m_pTCPClient.SendMessage(result);
                return;
            }

            using (ZBar.ImageScanner scanner = new ZBar.ImageScanner())
            {
                scanner.SetConfiguration(ZBar.SymbolType.None, ZBar.Config.Enable, 0);
                scanner.SetConfiguration(ZBar.SymbolType.QRCODE, ZBar.Config.Enable, 1);

                List<ZBar.Symbol> symbols = new List<ZBar.Symbol>();

                symbols = scanner.Scan((System.Drawing.Image)ImageOriginal);

                if (symbols != null && symbols.Count > 0)
                {
                    symbols.ForEach(s => result += s.Data);
                    m_pTCPClient.SendMessage(result);
                    return;
                }
            }

            if (result == string.Empty)
            {
                m_pTCPClient.SendMessage("CodeError");
            }
         
            return;
        }


        //public static void ReadQRcode(Bitmap ImageOriginal, int ZoomNumber)
        //{
        //    string result = string.Empty;

        //    System.Drawing.Bitmap ImageBaseOriginal = new System.Drawing.Bitmap(ImageOriginal);
        //    Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(ImageBaseOriginal);

        //    OpenCvSharp.Size size = new OpenCvSharp.Size(ImageBaseOriginal.Width * ZoomNumber, ImageBaseOriginal.Height * ZoomNumber);
        //    Mat SizeMat = new Mat();
        //    Cv2.Resize(mat, SizeMat, size);

        //    List<int> Blocksize = new List<int>();
        //    for(int i=9;i<32;i+=2)
        //    {
        //        Blocksize.Add(i);
        //    }

        //    Mat SizeMatThreshold = new Mat();
        //    Cv2.AdaptiveThreshold(SizeMat, SizeMatThreshold, 255, AdaptiveThresholdTypes.MeanC, ThresholdTypes.Binary, 17, 2);
        //    Cv2.ImShow("SizeMatThreshold", SizeMatThreshold);
        //    Bitmap bitmap12 = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(SizeMatThreshold); // mat 转 bitmap



        //    if (result != string.Empty)
        //    {
        //        m_pTCPClient.SendMessage(result);
        //        return;
        //    }


        //    using (ZBar.ImageScanner scanner = new ZBar.ImageScanner())
        //    {
        //        scanner.SetConfiguration(ZBar.SymbolType.None, ZBar.Config.Enable, 0);
        //        scanner.SetConfiguration(ZBar.SymbolType.QRCODE, ZBar.Config.Enable, 1);

        //        List<ZBar.Symbol> symbols = new List<ZBar.Symbol>();

        //        symbols = scanner.Scan((System.Drawing.Image)ImageOriginal);

        //        if (symbols != null && symbols.Count > 0)
        //        {
        //            symbols.ForEach(s => result += s.Data);
        //            m_pTCPClient.SendMessage(result);
        //            return;
        //        }
        //    }

        //    if (result == string.Empty)
        //    {
        //        m_pTCPClient.SendMessage("CodeError");
        //    }

        //    return;
        //}

        /// <summary>
        /// 处理图片灰度
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static Bitmap MakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            System.Drawing.Imaging.ColorMatrix colorMatrix = new System.Drawing.Imaging.ColorMatrix(
               new float[][]
              {
                 new float[] {.3f, .3f, .3f, 0, 0},
                 new float[] {.59f, .59f, .59f, 0, 0},
                 new float[] {.11f, .11f, .11f, 0, 0},
                 new float[] {0, 0, 0, 1, 0},
                 new float[] {0, 0, 0, 0, 1}
              });

            //create some image attributes
            System.Drawing.Imaging.ImageAttributes attributes = new System.Drawing.Imaging.ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }
    }

}

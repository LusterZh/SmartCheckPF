using samrtCheckPF.alg;
using samrtCheckPF.common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace samrtCheckPF
{
    /// <summary>
    /// ShowImageWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ShowImageWindow : Window
    {
        ScaleTransform st;
        TranslateTransform tt;
        TransformGroup group;
        bool isDrag = false;
        Point startPoint;
        ArrayList largeRectangleList = new ArrayList();
        double _Width;
        double _Height;
        List<LammyRect> m_rect;
        bool _isDJStation = false;

        public double ImageWidth
        {
            get { return _Width; }
            set { _Width = value; }
        }

        public double ImageHeight
        {
            get { return _Height; }
            set { _Height = value; }
        }

        public bool IsDJStation
        {
            get { return _isDJStation; }
            set { _isDJStation = value; }
        }

        public ShowImageWindow(string imagePath, List<LammyRect> Showrect, double width, double height, bool TF,bool isDJStation)
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(Grd_Loaded);

            // 设置全屏
            this.WindowState = System.Windows.WindowState.Normal;
            //this.WindowStyle = System.Windows.WindowStyle.None;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
           // this.Topmost = true;

            this.Left = 0.0;
            this.Top = 0.0;
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;

            iv_big.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            iv_big.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
            // iv_big.Source = getBitmapImage(imagePath);


            BitmapImage bi = getBitmapImage(imagePath);
            //2.bitmapimage旋转
            TransformedBitmap tb = new TransformedBitmap();
            tb.BeginInit();
            tb.Source = bi;
            RotateTransform transform = new RotateTransform(180);
            tb.Transform = transform;
            tb.EndInit();

            iv_big.Source = tb;


            ImageWidth = width;
            ImageHeight = height;
            m_rect = Showrect;
            IsDJStation = isDJStation;
            DrawLargeRect(m_rect, ImageWidth, ImageHeight, IsDJStation);

            if (TF)
            {
                // 设置窗体3秒后自动关闭
                StartCloseTimer();
            }
            ShowDialog();

        }

        public ShowImageWindow(BitmapImage Temp, List<LammyRect> Showrect, double width, double height, bool TF, bool isDJStation)
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(Grd_Loaded);

            // 设置全屏
            this.WindowState = System.Windows.WindowState.Normal;
            //this.WindowStyle = System.Windows.WindowStyle.None;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            // this.Topmost = true;

            this.Left = 0.0;
            this.Top = 0.0;
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;

            iv_big.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            iv_big.Height = System.Windows.SystemParameters.PrimaryScreenHeight;

            //2.bitmapimage旋转
            TransformedBitmap tb = new TransformedBitmap();
            tb.BeginInit();
            tb.Source = Temp;
            RotateTransform transform = new RotateTransform(180);
            tb.Transform = transform;
            tb.EndInit();

            iv_big.Source = tb;

            ImageWidth = width;
            ImageHeight = height;
            m_rect = Showrect;
            IsDJStation = isDJStation;
            DrawLargeRect(m_rect, ImageWidth, ImageHeight,isDJStation);

            if (TF)
            {
                // 设置窗体3秒后自动关闭
                StartCloseTimer();
            }
            ShowDialog();
        }

        private BitmapImage getBitmapImage(string imagePath)
        {
            BinaryReader binReader = new BinaryReader(File.Open(imagePath, FileMode.Open));
            FileInfo fileInfo = new FileInfo(imagePath);
            byte[] bytes = binReader.ReadBytes((int)fileInfo.Length);
            binReader.Close();

            // Init bitmap
            BitmapImage CurBitmap = new BitmapImage();
            CurBitmap = new BitmapImage();
            CurBitmap.BeginInit();
            CurBitmap.StreamSource = new MemoryStream(bytes);
            CurBitmap.EndInit();
            return CurBitmap;
        }

        private void StartCloseTimer()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(2.5); // 3秒
            // 为了方便测试，可以把这个秒数写到App.config配置文件中
          //  double t = double.Parse(ConfigurationManager.AppSettings["LOGO_WINDOW_AUTO_CLOSE_TIMER"]);
            timer.Tick += TimerTick; // 注册计时器到点后触发的回调
            timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            timer.Stop();
            timer.Tick -= TimerTick; // 取消注册
            this.Close();
        }

        private void Grd_Loaded(object sender, RoutedEventArgs e)
        {
            group = (TransformGroup)grdMap.RenderTransform;
            st = group.Children[0] as ScaleTransform;
            tt = group.Children[3] as TranslateTransform;
        }

        private void grdMap_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var point = e.GetPosition(grdRelative); // 实际点击的点
            var actualPoint = group.Inverse.Transform(point); // 想要缩放的点
            slider.Value = slider.Value + (double)e.Delta / 1000;
            tt.X = -((actualPoint.X * (slider.Value - 1))) + point.X - actualPoint.X;
            tt.Y = -((actualPoint.Y * (slider.Value - 1))) + point.Y - actualPoint.Y;
            DrawLargeRect(m_rect, ImageWidth, ImageHeight,IsDJStation);
        }

        private void grdMap_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isDrag = true;
            startPoint = e.GetPosition(grdRelative);
        }

        private void grdMap_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isDrag = false;
        }

        private void grdMap_MouseLeave(object sender, MouseEventArgs e)
        {
            isDrag = false;
        }

        private void grdMap_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrag)
            {
                Point p = e.GetPosition(grdRelative);
                Point topPoint = grdMap.TranslatePoint(new Point(0, 0), grdRelative);
                Point bottomPoint = grdMap.TranslatePoint(new Point(grdMap.ActualWidth, grdMap.ActualHeight), grdRelative);

                double moveX = p.X - startPoint.X;
                double moveY = p.Y - startPoint.Y;

                //向上向下移动条件判断（会有一点点的小偏移，如果想更精确的控制，那么分向上和向下两种情况，并判断边距）
                if ((moveY < 0 && bottomPoint.Y > grdRelative.ActualHeight) || (moveY > 0 && topPoint.Y < 0))
                {
                    tt.Y += (p.Y - startPoint.Y);
                    startPoint.Y = p.Y;
                }

                //向左向右移动条件判断
                if ((moveX < 0 && bottomPoint.X > grdRelative.ActualWidth) || (moveX > 0 && topPoint.X < 0))
                {
                    tt.X += (p.X - startPoint.X);
                    startPoint.X = p.X;
                }
                DrawLargeRect(m_rect, ImageWidth, ImageHeight, IsDJStation);
            }
        }

        private void DrawLargeRect(List<LammyRect> rects,double width, double Height,bool IsDJStation)
        {
            foreach (UIElement rectangle in largeRectangleList)
            {
               this.grdMap.Children.Remove(rectangle);            
            }
            largeRectangleList.Clear();
            double ratio = iv_big.Width / width;
            double ratio1 = iv_big.Height / Height;

            int size = rects.Count;
            for (int i = 0; i < size; i++)
            {
                LammyRect r = rects[i];
                Rectangle curRectangle = r.curRectangle;
                Rectangle largeRectangle = new System.Windows.Shapes.Rectangle();
                if (r.result == "PASS")
                {
                  largeRectangle.Stroke = Brushes.Transparent;
                }
                else if (r.result == "FAIL")
                {
                    largeRectangle.Stroke = Brushes.Red;
                }
                else
                {
                    largeRectangle.Stroke = Brushes.Transparent;
                }
                largeRectangle.StrokeThickness = 3;
                largeRectangle.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                largeRectangle.VerticalAlignment = VerticalAlignment.Top;
                //if (!IsDJStation)
                //{
                //    double Left = curRectangle.Margin.Left - GlobalValue.ALG_RESULT_CalibrationRes.global_roi_rect[0];
                //    if (Left < 0)
                //    {
                //        Left = 0;
                //    }
                //    double TOP = curRectangle.Margin.Top - GlobalValue.ALG_RESULT_CalibrationRes.global_roi_rect[1];
                //    if (TOP < 0)
                //    {
                //        TOP = 0;
                //    }
                //    largeRectangle.Margin = new Thickness(Left * ratio, TOP * ratio1, 0, 0);
                //}
                //else
                //{
                //    largeRectangle.Margin = new Thickness(curRectangle.Margin.Left * ratio, curRectangle.Margin.Top * ratio1, 0, 0);
                //}

                largeRectangle.Margin = new Thickness(curRectangle.Margin.Left * ratio, curRectangle.Margin.Top * ratio1, 0, 0);
                largeRectangle.Width = curRectangle.Width * ratio;
                largeRectangle.Height = curRectangle.Height * ratio1;

                this.grdMap.Children.Add(largeRectangle); //屏蔽掉，不画大框
                largeRectangleList.Add(largeRectangle);
                Panel.SetZIndex(largeRectangle, 5);
            }
        }


        private static System.Drawing.Bitmap BitmapImage2Bitmap(BitmapImage bitmapImage)
        {
            // BitmapImage bitmapImage = new BitmapImage(new Uri("../Images/test.png", UriKind.Relative));

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new System.Drawing.Bitmap(bitmap);
            }
        }


        private static BitmapImage BitmapToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg); // 坑点：格式选Bmp时，不带透明度

                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                // According to MSDN, "The default OnDemand cache option retains access to the stream until the image is needed."
                // Force the bitmap to load right now so we can dispose the stream.
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
        }
    }
}

using System;
using System.Collections.Generic;
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
using smartCheckPF;

namespace smartCheckPF
{
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : Window
    {


        public string _UserName;
        public string _UserPassword;
        public bool _IsValid = false;

        public LoginWindow()
        {
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            // 设置全屏
           this.WindowState = System.Windows.WindowState.Normal;
          //  this.WindowStyle = System.Windows.WindowStyle.None;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;

            InitializeComponent();
            username.Focus();
        }

        //用户名
        public string UserName
        {
            get { return _UserName; }
            set { _UserName = value; }
        }

        //密码
        public string UserPassword
        {
            get { return _UserPassword; }
            set { _UserPassword = value; }
        }

        //是否获取用户名和密码
        public bool IsValid
        {
            get { return _IsValid; }
            set { _IsValid = value; }
        }

        private void Btn_ok_Click(object sender, RoutedEventArgs e)
        {
            UserName = username.Text;
            UserPassword = password.Password;
            if(UserName =="" || UserPassword =="")
            {
                MessageBox.Show("用户名或密码不能为空");
                return;
            }
            IsValid = true;
            this.Close();
        }

        private void Btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            IsValid = false;
            this.Close();
        }

        private void Username_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                password.Focus();
            }
        }

        private void Password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btn_ok.Focus();
            }
        }
    }
}

using samrtCheckPF.common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace samrtCheckPF
{
    public class LammyRect : INotifyPropertyChanged
    {
        Rectangle _curRectangle;

        private double _index;
        private double _left;
        private double _top;
        private double _width;
        private double _height;
        private string _function_name;
        private int _function_ID;
        private string _result;


        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }


        public Rectangle curRectangle
        {
            get { return _curRectangle; }
            set { _curRectangle = value;
                _left = curRectangle.Margin.Left;
                _top = curRectangle.Margin.Top;
                _width = curRectangle.Width;
                _height = curRectangle.Height;
            }
        }
        public double left
        {
            get { return curRectangle.Margin.Left; }
            set { _left = value; }
        }

        public double index
        {
            get { return _index; }
            set { _index = value; }
        }
        public double top
        {
            get { return curRectangle.Margin.Top; }
            set { _top = value; }

        }
        public double width
        {
            get { return curRectangle.Width; }
            set { _width = value; }
        }

        public double height
        {
            get { return curRectangle.Height; }
            set { _height = value; }

        }
        public string function_name
        {
            get { return _function_name; }
            set { _function_name = value; }
        }

        public int function_ID
        {
            get { return _function_ID; }
            set { _function_ID = value; }
        }

        public string result
        {
            get { return _result; }
            set { _result = value;
                if (value == GlobalValue.ALG_RESULT_TRUE)
                {
                    curRectangle.Stroke = GlobalValue.BRUSH_PASS_COLOR;
                }
                else if (value == GlobalValue.ALG_RESULT_FALSE)
                {
                    curRectangle.Stroke = GlobalValue.BRUSH_FAIL_COLOR;
                }
                else
                {
                    curRectangle.Stroke = GlobalValue.BRUSH_INIT_COLOR;
                }
              OnPropertyChanged(new PropertyChangedEventArgs("result"));

            }

        }

        public LammyRect(int index, int left, int top, int width, int height, int function_ID, string function_name,string result)//构造函数
        {
            _index = index;
            _left = left;
            _top = top;
            _width = width;
            _height = height;
            _function_name = function_name;
            _result = result;
            _function_ID = function_ID;

            curRectangle = new System.Windows.Shapes.Rectangle();
            if (result == GlobalValue.ALG_RESULT_TRUE)
            {              
                curRectangle.Stroke = GlobalValue.BRUSH_PASS_COLOR;
            }
            else if (result == GlobalValue.ALG_RESULT_FALSE)
            {
                curRectangle.Stroke = GlobalValue.BRUSH_FAIL_COLOR;
            }
            else
            {
                curRectangle.Stroke = GlobalValue.BRUSH_INIT_COLOR;
            }

            curRectangle.HorizontalAlignment = HorizontalAlignment.Left;
            curRectangle.VerticalAlignment = VerticalAlignment.Top;
            curRectangle.Margin = new Thickness(left, top, 0, 0);
            curRectangle.Height = height;
            curRectangle.Width = width;
            
         
        }

    }

  

}



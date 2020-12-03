using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Shapes;
using samrtCheckPF.common;

namespace samrtCheckPF
{
    class DjRect : INotifyPropertyChanged
    {
        private double _index;
        private string _function_name;
        private string _result;
        private List<LammyRect> _rects;


        public DjRect(double _index, string _function_name, string _result, List<LammyRect> _rects)
        {
            this._index = _index;
            this._function_name = _function_name;
            this._result = _result;
            this._rects = _rects;
        }

        public double index
        {
            get { return _index; }
            set { _index = value; }
        }

        public string function_name
        {
            get { return _function_name; }
            set { _function_name = value; }
        }

        public string result
        {
            get { return _result; }
            set {
                _result = value;
                OnPropertyChanged(new PropertyChangedEventArgs("result"));
            }
        }

        public List<LammyRect> rects
        {
            get { return _rects; }
            set { _rects = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

    }
}

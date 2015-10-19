using System;
using System.Windows.Forms;

namespace PowerPlanChanger
{
    public class DataMenuItem<T> : MenuItem
    {
        private Func<T, string> _dataSource;

        public T Data { get; set; }

        public Func<T, string> DataSource
        {
            get { return _dataSource; }
            set
            {
                _dataSource = value;
                Text = value(Data);
            }
        }

        public DataMenuItem(T data, Func<T, string> dataSource)
        {
            Data = data;
            DataSource = dataSource;
        }
    }
}

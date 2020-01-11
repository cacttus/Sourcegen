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

namespace sourcegen
{
    public partial class TextEditor : Window
    {
        private Control _control = null;
        public Control Control
        {
            get
            {
                return _control;
            }
            set
            {
                _control = value;
                if (TextBoxControl != null)
                {
                    _textBox.Text = TextBoxControl.Text;
                }
            }
        }
        public TextBox TextBoxControl
        {
            get{
                return (_control as TextBox);
            }
        }
        public TextEditor()
        {
            InitializeComponent();
        }

        private void _btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (TextBoxControl != null)
            {
                TextBoxControl.Text = _textBox.Text;
            }
            Close();
        }

        private void _btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

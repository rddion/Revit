using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Wpf.ViewModel
{
    class Condition : INotifyPropertyChanged
    {
        private string name;
        private string text;
        private object selectedValue;
        private object selectedItem;
        private Brush background;
        private Guid ruleId;

        public event PropertyChangedEventHandler PropertyChanged;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
            }
        }

        public object SelectedValue
        {
            get { return selectedValue; }
            set
            {
                selectedValue = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedValue)));
            }
        }

        public object SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
            }
        }

        public Brush Background
        {
            get { return background; }
            set
            {
                background = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Background)));
            }
        }

        public Guid RuleId
        {
            get { return ruleId; }
            set
            {
                ruleId = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RuleId)));
            }
        }

        public Condition(string controlName, Guid ruleId)
        {
            Name = controlName;
            RuleId = ruleId;
            Background = Brushes.White;
        }

        public Condition(Control control)
        {
            Name = control.Name;
            Background = control.Background;
            try
            {
                Text = ((TextBox)control).Text;
            }
            catch { }

            try
            {
                SelectedValue = ((Selector)control).SelectedValue;
                SelectedItem = ((Selector)control).SelectedItem;
            }
            catch { }
        }
    }
}

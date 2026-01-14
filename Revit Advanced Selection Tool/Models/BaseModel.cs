using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RevitAdvancedSelectionTool.Models
{
    public abstract class BaseModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly Dictionary<string, string> _errors = new Dictionary<string, string>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public string Error => string.Join(Environment.NewLine, _errors.Values);

        public string this[string columnName]
        {
            get => _errors.ContainsKey(columnName) ? _errors[columnName] : null;
            set
            {
                if (_errors.ContainsKey(columnName))
                {
                    if (string.IsNullOrEmpty(value))
                        _errors.Remove(columnName);
                    else
                        _errors[columnName] = value;
                }
                else if (!string.IsNullOrEmpty(value))
                {
                    _errors[columnName] = value;
                }
            }
        }
    }
}
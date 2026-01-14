using System.ComponentModel;

namespace RevitAdvancedSelectionTool.Interfaces
{
    public interface INotifyPropertyChanged
    {
        event PropertyChangedEventHandler PropertyChanged;
    }
}
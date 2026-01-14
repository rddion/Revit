using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Converters;
using Wpf.View.ViewServices;
using Wpf.ViewModel;

namespace Wpf
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        internal ViewModel.ViewModel ViewModel { get; set; }

        RuleManager ruleManager;
        public MainWindow()
        {
            InitializeComponent();
            ViewModel.ViewModel viewModel = new ViewModel.ViewModel();
            ViewModel = viewModel;
            DataContext = viewModel;
            ruleManager = new RuleManager(this);
            ViewModel.PreviouslySelectedCategories.CollectionChanged += PreviouslySelectedCategories;
        }

        private void Add_Rule(object sender, RoutedEventArgs e)
        {
            ruleManager.AddRule();
        }

        private void Categories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.SelectionCategoriesChanged(lView.SelectedItems);
        }

        private void SelectAllCategories(object sender, RoutedEventArgs e)
        {
            lView.SelectAll();
        }

        private void CancelSelection(object sender, RoutedEventArgs e)
        {
            lView.SelectedItems.Clear();
        }

        private void PreviouslySelectedCategories(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                for (int i = 0; i < ViewModel.PreviouslySelectedCategories.Count; i++)
                {
                    lView.SelectedItems.Add(ViewModel.PreviouslySelectedCategories[i]);
                }
            }
            catch { }
        }
    }

}

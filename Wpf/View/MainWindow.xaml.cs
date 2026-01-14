using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RevitAdvancedSelectionTool.Services;
using RevitAdvancedSelectionTool.Models;
using Wpf.ViewModel;

namespace Wpf
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel ViewModel { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // В реальной реализации здесь будет dependency injection
            // Пока создаем сервисы напрямую
            var revitService = new RevitService();
            var categoryService = new CategoryService();
            var filterService = new FilterService();

            ViewModel = new MainWindowViewModel(revitService, categoryService, filterService);
            DataContext = ViewModel;
        }

        private void Categories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView listView)
            {
                var selectedCategories = listView.SelectedItems.Cast<Category>().ToList();
                ViewModel.SelectionCategoriesChanged(selectedCategories);
            }
        }

        private void SelectAllCategories(object sender, RoutedEventArgs e)
        {
            lView.SelectAll();
        }

        private void CancelSelection(object sender, RoutedEventArgs e)
        {
            lView.SelectedItems.Clear();
        }

        private void Add_Rule(object sender, RoutedEventArgs e)
        {
            // В MVVM это должно быть через команду, но для демонстрации оставим
            // В реальной реализации нужно создать RuleBuilderViewModel
            var rule = new FilterRule
            {
                ParameterName = "Имя параметра",
                Operator = RuleOperator.Equals,
                Value = "Значение"
            };
            ViewModel.AddRule(rule);
        }
    }
}

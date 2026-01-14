using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using RevitAdvancedSelectionTool.Services;
using RevitAdvancedSelectionTool.Models;
using Autodesk.Revit.DB;

namespace Wpf.ViewModel
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly IRevitService _revitService;
        private readonly ICategoryService _categoryService;
        private readonly IFilterService _filterService;

        // Properties
        private ObservableCollection<Category> _categories;
        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private ObservableCollection<Category> _selectedCategories;
        public ObservableCollection<Category> SelectedCategories
        {
            get => _selectedCategories;
            set => SetProperty(ref _selectedCategories, value);
        }

        private ObservableCollection<FilterRule> _rules;
        public ObservableCollection<FilterRule> Rules
        {
            get => _rules;
            set => SetProperty(ref _rules, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        // Commands
        public ICommand LoadCategoriesCommand { get; }
        public ICommand ApplyCategoriesCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand InvertSelectionCommand { get; }

        public MainWindowViewModel(IRevitService revitService, ICategoryService categoryService, IFilterService filterService)
        {
            _revitService = revitService;
            _categoryService = categoryService;
            _filterService = filterService;

            // Subscribe to service events
            _revitService.StatusChanged += OnStatusChanged;

            // Initialize collections
            Categories = new ObservableCollection<Category>();
            SelectedCategories = new ObservableCollection<Category>();
            Rules = new ObservableCollection<FilterRule>();

            // Initialize commands
            LoadCategoriesCommand = new RelayCommand(async () => await LoadCategoriesAsync());
            ApplyCategoriesCommand = new RelayCommand(async () => await ApplyCategoriesAsync(), () => SelectedCategories.Any());
            SearchCommand = new RelayCommand(async () => await SearchAsync(), () => SelectedCategories.Any() && Rules.Any());
            InvertSelectionCommand = new RelayCommand(async () => await InvertSelectionAsync());
        }

        private async Task LoadCategoriesAsync()
        {
            IsBusy = true;
            try
            {
                var categories = await _revitService.GetCategoriesAsync();
                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ApplyCategoriesAsync()
        {
            IsBusy = true;
            try
            {
                var categoryNames = SelectedCategories.Select(c => c.Name).ToList();
                var parameters = await _revitService.GetCommonParametersAsync(categoryNames);
                StatusMessage = $"Выбрано {SelectedCategories.Count} категорий, найдено {parameters.Count} параметров";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SearchAsync()
        {
            IsBusy = true;
            try
            {
                var categoryNames = SelectedCategories.Select(c => c.Name).ToList();
                var rulesList = Rules.ToList();
                var result = await _revitService.SearchElementsAsync(categoryNames, rulesList);
                StatusMessage = result.StatusMessage;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task InvertSelectionAsync()
        {
            IsBusy = true;
            try
            {
                // В реальной реализации нужно получить текущую выборку
                var elementIds = new List<Autodesk.Revit.DB.ElementId>();
                await _revitService.InvertSelectionAsync(elementIds);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnStatusChanged(object sender, string message)
        {
            StatusMessage = message;
        }

        // Methods for UI interaction
        public void SelectionCategoriesChanged(IEnumerable<Category> selection)
        {
            SelectedCategories.Clear();
            foreach (var category in selection)
            {
                SelectedCategories.Add(category);
            }
        }

        public void AddRule(FilterRule rule)
        {
            Rules.Add(rule);
        }
    }

    // Simple RelayCommand implementation
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
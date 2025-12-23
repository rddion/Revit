# Проектирование MVVM архитектуры для "Revit Advanced Selection Tool"

## Цели новой архитектуры

1. **Разделение ответственностей** - четкое разделение между View, ViewModel и Model
2. **Улучшение тестируемости** - возможность юнит-тестирования бизнес-логики
3. **Повышение расширяемости** - легкость добавления новых функций
4. **Улучшение сопровождения** - понятная и модульная структура
5. **Соблюдение принципов SOLID** - особенно принципа единственной ответственности

## Общая архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                     MVVM Architecture                        │
├─────────────────────────────────────────────────────────────┤
│  View Layer (WPF)                                           │
│  ├── Views/                                                 │
│  │   ├── MainWindow.xaml                                   │
│  │   ├── MainWindow.xaml.cs (minimal code-behind)         │
│  │   ├── UserControls/                                     │
│  │   │   ├── CategorySelectionView.xaml                   │
│  │   │   ├── RuleBuilderView.xaml                         │
│  │   │   └── SearchResultsView.xaml                       │
│  │   └── Converters/                                       │
│  │       ├── BooleanToVisibilityConverter.cs              │
│  │       └── EnumToBooleanConverter.cs                    │
├─────────────────────────────────────────────────────────────┤
│  ViewModel Layer                                            │
│  ├── ViewModels/                                           │
│  │   ├── MainWindowViewModel.cs                           │
│  │   ├── CategorySelectionViewModel.cs                    │
│  │   ├── RuleBuilderViewModel.cs                          │
│  │   └── SearchResultsViewModel.cs                        │
│  ├── Commands/                                             │
│  │   ├── RelayCommand.cs                                  │
│  │   └── AsyncCommand.cs                                  │
│  └── Behaviors/                                            │
│      ├── SelectionChangedBehavior.cs                       │
│      └── TextChangedBehavior.cs                            │
├─────────────────────────────────────────────────────────────┤
│  Model Layer                                               │
│  ├── Models/                                               │
│  │   ├── Category.cs                                      │
│  │   ├── FilterRule.cs                                    │
│  │   ├── ParameterInfo.cs                                 │
│  │   ├── SearchResult.cs                                  │
│  │   └── RevitElement.cs                                  │
│  ├── Services/                                             │
│  │   ├── IRevitService.cs                                 │
│  │   ├── RevitService.cs                                  │
│  │   ├── ICategoryService.cs                              │
│  │   ├── CategoryService.cs                               │
│  │   ├── IFilterService.cs                                │
│  │   └── FilterService.cs                                 │
│  └── Interfaces/                                           │
│      ├── INotifyPropertyChanged.cs                        │
│      └── IDataErrorInfo.cs                                │
└─────────────────────────────────────────────────────────────┘
```

## Детальное проектирование слоев

### 1. Model Layer

#### Модели данных (Models)

```csharp
// Category.cs
public class Category : BaseModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public bool IsSelected { get; set; }
}

// FilterRule.cs
public class FilterRule : BaseModel
{
    public string ParameterName { get; set; }
    public RuleOperator Operator { get; set; }
    public string Value { get; set; }
    public LogicalOperator LogicalOperator { get; set; } // И, ИЛИ
}

// ParameterInfo.cs
public class ParameterInfo : BaseModel
{
    public string Name { get; set; }
    public StorageType StorageType { get; set; }
    public string DisplayName => $"{Name} ({StorageType})";
}

// SearchResult.cs
public class SearchResult : BaseModel
{
    public List<RevitElement> FoundElements { get; set; }
    public int TotalCount { get; set; }
    public string StatusMessage { get; set; }
}

// RevitElement.cs
public class RevitElement : BaseModel
{
    public ElementId Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}
```

#### Сервисы (Services)

```csharp
// IRevitService.cs
public interface IRevitService
{
    Task<List<Category>> GetCategoriesAsync();
    Task<List<ParameterInfo>> GetCommonParametersAsync(List<string> categoryNames);
    Task<SearchResult> SearchElementsAsync(List<string> categories, List<FilterRule> rules);
    Task InvertSelectionAsync(List<ElementId> elementIds);
    event EventHandler<string> StatusChanged;
}

// ICategoryService.cs
public interface ICategoryService
{
    Task<List<Category>> LoadCategoriesAsync();
    Task<List<Category>> FilterCategoriesAsync(string searchText);
    Task SaveCategorySelectionAsync(List<Category> selectedCategories);
}

// IFilterService.cs
public interface IFilterService
{
    Task<List<ParameterInfo>> GetAvailableParametersAsync(List<string> categoryNames);
    Task<bool> ValidateRuleAsync(FilterRule rule);
    Task<SearchResult> ApplyFilterAsync(List<string> categories, List<FilterRule> rules);
}
```

### 2. ViewModel Layer

#### Основные ViewModels

```csharp
// MainWindowViewModel.cs
public class MainWindowViewModel : BaseViewModel
{
    private readonly IRevitService _revitService;
    private readonly ICategoryService _categoryService;
    private readonly IFilterService _filterService;
    
    public CategorySelectionViewModel CategorySelection { get; }
    public RuleBuilderViewModel RuleBuilder { get; }
    public SearchResultsViewModel SearchResults { get; }
    
    // Commands
    public ICommand ApplyCategoriesCommand { get; }
    public ICommand SearchElementsCommand { get; }
    public ICommand InvertSelectionCommand { get; }
    public ICommand RefreshCategoriesCommand { get; }
    
    // Properties
    public string StatusMessage { get; set; }
    public bool IsBusy { get; set; }
    public bool CanSearch { get; set; }
    
    public MainWindowViewModel(IRevitService revitService, 
                              ICategoryService categoryService,
                              IFilterService filterService)
    {
        _revitService = revitService;
        _categoryService = categoryService;
        _filterService = filterService;
        
        // Initialize child ViewModels
        CategorySelection = new CategorySelectionViewModel(_categoryService);
        RuleBuilder = new RuleBuilderViewModel(_filterService);
        SearchResults = new SearchResultsViewModel(_revitService);
        
        // Initialize commands
        ApplyCategoriesCommand = new RelayCommand(async () => await ApplyCategoriesAsync());
        SearchElementsCommand = new RelayCommand(async () => await SearchElementsAsync(), CanSearchExecute);
        InvertSelectionCommand = new RelayCommand(async () => await InvertSelectionAsync());
        RefreshCategoriesCommand = new RelayCommand(async () => await RefreshCategoriesAsync());
        
        // Subscribe to child ViewModel events
        CategorySelection.CategoriesChanged += OnCategoriesChanged;
        RuleBuilder.RulesChanged += OnRulesChanged;
    }
    
    private async Task ApplyCategoriesAsync()
    {
        IsBusy = true;
        try
        {
            var selectedCategories = CategorySelection.SelectedCategories.Select(c => c.Name).ToList();
            await RuleBuilder.LoadParametersAsync(selectedCategories);
            StatusMessage = $"Выбрано {selectedCategories.Count} категорий";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private async Task SearchElementsAsync()
    {
        IsBusy = true;
        try
        {
            var categories = CategorySelection.SelectedCategories.Select(c => c.Name).ToList();
            var rules = RuleBuilder.Rules.ToList();
            var result = await _revitService.SearchElementsAsync(categories, rules);
            SearchResults.UpdateResults(result);
            StatusMessage = $"Найдено {result.TotalCount} элементов";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка поиска: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private bool CanSearchExecute() => CategorySelection.HasSelection && RuleBuilder.HasValidRules;
    
    private void OnCategoriesChanged(object sender, EventArgs e)
    {
        RaisePropertyChanged(nameof(CanSearch));
    }
    
    private void OnRulesChanged(object sender, EventArgs e)
    {
        RaisePropertyChanged(nameof(CanSearch));
    }
}
```

#### Командный слой

```csharp
// RelayCommand.cs
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
    
    public event EventHandler CanExecuteChanged;
}

// AsyncCommand.cs
public class AsyncCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool> _canExecute;
    private bool _isExecuting;
    
    public AsyncCommand(Func<Task> execute, Func<bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }
    
    public bool CanExecute(object parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);
    
    public async void Execute(object parameter)
    {
        if (CanExecute(parameter))
        {
            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }
    }
    
    public event EventHandler CanExecuteChanged;
    
    private void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

### 3. View Layer

#### Основные изменения в XAML

```xml
<!-- MainWindow.xaml -->
<Window x:Class="Wpf.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:local="clr-namespace:Wpf.Views"
        xmlns:vm="clr-namespace:Wpf.ViewModels"
        xmlns:uc="clr-namespace:Wpf.Views.UserControls"
        Title="Revit Advanced Selection Tool" Height="570" Width="800">
    
    <Window.DataContext>
        <vm:MainWindowViewModel/>
    </Window.DataContext>
    
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="200"/>
            <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        
        <!-- Category Selection -->
        <uc:CategorySelectionView DataContext="{Binding CategorySelection}" 
                                  Grid.Column="0"/>
        
        <!-- Rule Builder and Results -->
        <Grid Grid.Column="1">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            
            <uc:RuleBuilderView DataContext="{Binding RuleBuilder}" 
                               Grid.Row="0"/>
            
            <uc:SearchResultsView DataContext="{Binding SearchResults}" 
                                 Grid.Row="1"/>
            
            <!-- Commands -->
            <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="Применить" 
                       Command="{Binding ApplyCategoriesCommand}"
                       Margin="5"/>
                <Button Content="Найти и выбрать" 
                       Command="{Binding SearchElementsCommand}"
                       IsEnabled="{Binding CanSearch}"
                       Margin="5"/>
                <Button Content="Инвертировать выбор" 
                       Command="{Binding InvertSelectionCommand}"
                       Margin="5"/>
                <Button Content="Обновить список" 
                       Command="{Binding RefreshCategoriesCommand}"
                       Margin="5"/>
            </StackPanel>
        </Grid>
    </Grid>
</Window>
```

#### User Controls

```xml
<!-- CategorySelectionView.xaml -->
<UserControl x:Class="Wpf.Views.UserControls.CategorySelectionView">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <TextBox Grid.Row="0" 
                Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                Placeholder="Поиск по категориям"/>
        
        <ListView Grid.Row="1" 
                 ItemsSource="{Binding Categories}"
                 SelectedItems="{Binding SelectedCategories}"
                 SelectionMode="Extended">
            <ListView.View>
                <GridView>
                    <GridViewColumn Header="Категории" 
                                   DisplayMemberBinding="{Binding Name}"/>
                </GridView>
            </ListView.View>
        </ListView>
        
        <TextBlock Grid.Row="2" 
                  Text="{Binding SelectionCountText}"/>
    </Grid>
</UserControl>
```

### 4. Базовая инфраструктура

```csharp
// BaseModel.cs
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

// BaseViewModel.cs
public abstract class BaseViewModel : BaseModel
{
    protected readonly ILog Logger;
    
    protected BaseViewModel()
    {
        Logger = LogManager.GetLogger(GetType());
    }
    
    protected async Task ExecuteAsync(Func<Task> action, string errorMessage = "Произошла ошибка")
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, errorMessage);
            // Handle error (show message, etc.)
        }
    }
}
```

## Преимущества новой архитектуры

### 1. Разделение ответственностей
- **View** отвечает только за отображение данных
- **ViewModel** содержит логику представления
- **Model** представляет бизнес-данные и логику

### 2. Тестируемость
- **Юнит-тесты** для ViewModels без UI
- **Интеграционные тесты** для сервисов
- **Mock-объекты** для изоляции зависимостей

### 3. Расширяемость
- **Модульная структура** позволяет легко добавлять новые функции
- **Интерфейсы** обеспечивают гибкость реализации
- **Dependency Injection** упрощает конфигурацию

### 4. Сопровождение
- **Четкая структура** облегчает понимание кода
- **Меньшие классы** проще понимать и изменять
- **Принципы SOLID** соблюдаются автоматически

### 5. Производительность
- **Async/await** для асинхронных операций
- **Lazy loading** для больших коллекций
- **Data virtualization** для больших списков

## Следующие шаги

1. **Создание базовых классов** (BaseModel, BaseViewModel)
2. **Реализация моделей данных**
3. **Создание сервисного слоя**
4. **Разработка ViewModels**
5. **Рефакторинг Views**
6. **Настройка Dependency Injection**
7. **Тестирование и отладка**
# План реализации MVVM архитектуры для Revit Advanced Selection Tool

## Этап 1: Создание базовой инфраструктуры

### 1.1 Структура проектов

```
Revit Advanced Selection Tool/
├── Revit Advanced Selection Tool.sln
├── Revit Advanced Selection Tool/
│   ├── Models/
│   ├── Services/
│   ├── Interfaces/
│   └── Properties/
├── Wpf/
│   ├── ViewModels/
│   ├── Views/
│   ├── Views/UserControls/
│   ├── Commands/
│   ├── Converters/
│   ├── Behaviors/
│   └── Properties/
├── Contracts/
│   └── IRevitPluginContract.cs
└── Tests/
    ├── UnitTests/
    └── IntegrationTests/
```

### 1.2 Создание базовых интерфейсов и классов

#### INotifyPropertyChanged базовый интерфейс
```csharp
// Contracts/INotifyPropertyChanged.cs
public interface INotifyPropertyChanged
{
    event PropertyChangedEventHandler PropertyChanged;
}
```

#### BaseModel базовый класс
```csharp
// Models/BaseModel.cs
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
```

#### BaseViewModel базовый класс
```csharp
// ViewModels/BaseViewModel.cs
public abstract class BaseViewModel : BaseModel
{
    protected readonly ILog Logger;
    
    protected BaseViewModel(ILog logger = null)
    {
        Logger = logger ?? LogManager.GetLogger(GetType());
    }
    
    protected async Task ExecuteAsync(Func<Task> action, string errorMessage = "Произошла ошибка")
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Logger?.Error(ex, errorMessage);
            // TODO: Handle error (show message, etc.)
        }
    }
    
    protected void ExecuteOnUIThread(Action action)
    {
        if (SynchronizationContext.Current == SynchronizationContext.Current)
        {
            action();
        }
        else
        {
            SynchronizationContext.Current.Post(_ => action(), null);
        }
    }
}
```

## Этап 2: Создание моделей данных

### 2.1 Модель Category
```csharp
// Models/Category.cs
public class Category : BaseModel
{
    private int _id;
    private string _name;
    private string _type;
    private bool _isSelected;
    
    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }
    
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    public string Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }
    
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public override string ToString() => Name;
}
```

### 2.2 Модель FilterRule
```csharp
// Models/FilterRule.cs
public enum RuleOperator
{
    Equals,
    NotEquals,
    Contains,
    StartsWith,
    GreaterThan,
    LessThan
}

public enum LogicalOperator
{
    And,
    Or
}

public class FilterRule : BaseModel
{
    private string _parameterName;
    private RuleOperator _operator;
    private string _value;
    private LogicalOperator _logicalOperator;
    
    public string ParameterName
    {
        get => _parameterName;
        set => SetProperty(ref _parameterName, value);
    }
    
    public RuleOperator Operator
    {
        get => _operator;
        set => SetProperty(ref _operator, value);
    }
    
    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
    
    public LogicalOperator LogicalOperator
    {
        get => _logicalOperator;
        set => SetProperty(ref _logicalOperator, value);
    }
    
    public string DisplayText => $"{ParameterName} {GetOperatorDisplay()} {Value}";
    
    private string GetOperatorDisplay()
    {
        return Operator switch
        {
            RuleOperator.Equals => "=",
            RuleOperator.NotEquals => "≠",
            RuleOperator.Contains => "содержит",
            RuleOperator.StartsWith => "начинается с",
            RuleOperator.GreaterThan => ">",
            RuleOperator.LessThan => "<",
            _ => Operator.ToString()
        };
    }
}
```

## Этап 3: Создание командного слоя

### 3.1 RelayCommand
```csharp
// Commands/RelayCommand.cs
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
    
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

### 3.2 AsyncCommand
```csharp
// Commands/AsyncCommand.cs
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
    
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

## Этап 4: Создание сервисного слоя

### 4.1 Интерфейсы сервисов

#### IRevitService
```csharp
// Services/IRevitService.cs
public interface IRevitService
{
    Task<List<Category>> GetCategoriesAsync();
    Task<List<ParameterInfo>> GetCommonParametersAsync(List<string> categoryNames);
    Task<SearchResult> SearchElementsAsync(List<string> categories, List<FilterRule> rules);
    Task InvertSelectionAsync(List<ElementId> elementIds);
    event EventHandler<string> StatusChanged;
}
```

#### ICategoryService
```csharp
// Services/ICategoryService.cs
public interface ICategoryService
{
    Task<List<Category>> LoadCategoriesAsync();
    Task<List<Category>> FilterCategoriesAsync(string searchText);
    Task SaveCategorySelectionAsync(List<Category> selectedCategories);
}
```

#### IFilterService
```csharp
// Services/IFilterService.cs
public interface IFilterService
{
    Task<List<ParameterInfo>> GetAvailableParametersAsync(List<string> categoryNames);
    Task<bool> ValidateRuleAsync(FilterRule rule);
    Task<SearchResult> ApplyFilterAsync(List<string> categories, List<FilterRule> rules);
}
```

## Этап 5: Создание ViewModels

### 5.1 CategorySelectionViewModel
```csharp
// ViewModels/CategorySelectionViewModel.cs
public class CategorySelectionViewModel : BaseViewModel
{
    private readonly ICategoryService _categoryService;
    
    private ObservableCollection<Category> _categories;
    private ObservableCollection<Category> _selectedCategories;
    private string _searchText;
    
    public ObservableCollection<Category> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }
    
    public ObservableCollection<Category> SelectedCategories
    {
        get => _selectedCategories;
        set => SetProperty(ref _selectedCategories, value);
    }
    
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterCategoriesAsync();
            }
        }
    }
    
    public string SelectionCountText => $"Выбрано {SelectedCategories.Count} категорий";
    
    public bool HasSelection => SelectedCategories?.Count > 0;
    
    public ICommand SelectAllCommand { get; }
    public ICommand ClearSelectionCommand { get; }
    public ICommand RefreshCategoriesCommand { get; }
    
    public event EventHandler CategoriesChanged;
    
    public CategorySelectionViewModel(ICategoryService categoryService, ILog logger = null) : base(logger)
    {
        _categoryService = categoryService;
        
        Categories = new ObservableCollection<Category>();
        SelectedCategories = new ObservableCollection<Category>();
        
        SelectAllCommand = new RelayCommand(SelectAll);
        ClearSelectionCommand = new RelayCommand(ClearSelection);
        RefreshCategoriesCommand = new AsyncCommand(LoadCategoriesAsync);
        
        // Подписка на изменения выбора
        SelectedCategories.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(SelectionCountText));
            OnPropertyChanged(nameof(HasSelection));
            CategoriesChanged?.Invoke(this, EventArgs.Empty);
        };
        
        // Загружаем категории при инициализации
        LoadCategoriesAsync();
    }
    
    private async Task LoadCategoriesAsync()
    {
        await ExecuteAsync(async () =>
        {
            var categories = await _categoryService.LoadCategoriesAsync();
            
            ExecuteOnUIThread(() =>
            {
                Categories.Clear();
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }
            });
        }, "Ошибка при загрузке категорий");
    }
    
    private async void FilterCategoriesAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadCategoriesAsync();
            return;
        }
        
        await ExecuteAsync(async () =>
        {
            var filteredCategories = await _categoryService.FilterCategoriesAsync(SearchText);
            
            ExecuteOnUIThread(() =>
            {
                Categories.Clear();
                foreach (var category in filteredCategories)
                {
                    Categories.Add(category);
                }
            });
        }, "Ошибка при фильтрации категорий");
    }
    
    private void SelectAll()
    {
        SelectedCategories.Clear();
        foreach (var category in Categories)
        {
            SelectedCategories.Add(category);
        }
    }
    
    private void ClearSelection()
    {
        SelectedCategories.Clear();
    }
}
```

## Этап 6: Рефакторинг Views

### 6.1 CategorySelectionView XAML
```xml
<!-- Views/UserControls/CategorySelectionView.xaml -->
<UserControl x:Class="Wpf.Views.UserControls.CategorySelectionView">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Search Box -->
        <TextBox Grid.Row="0" 
                Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                Height="30" Margin="5"
                FontSize="12"
                ToolTip="Поиск по категориям"/>
        
        <!-- Categories List -->
        <ListView Grid.Row="1" 
                 ItemsSource="{Binding Categories}"
                 SelectedItems="{Binding SelectedCategories}"
                 SelectionMode="Extended"
                 Margin="5"
                 FontSize="16"
                 FontFamily="ISOCPEUR">
            <ListView.View>
                <GridView>
                    <GridViewColumn Header="Категории" 
                                   DisplayMemberBinding="{Binding Name}"
                                   Width="180"/>
                </GridView>
            </ListView.View>
        </ListView>
        
        <!-- Selection Count -->
        <TextBlock Grid.Row="2" 
                  Text="{Binding SelectionCountText}"
                  Margin="5"
                  FontSize="12"
                  HorizontalAlignment="Center"/>
        
        <!-- Control Buttons -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,25,0,5">
            <Button Content="Выбрать все" 
                   Command="{Binding SelectAllCommand}"
                   Height="30" Width="130" Margin="5"
                   FontSize="16" FontFamily="ISOCPEUR"/>
            <Button Content="Снять выбор" 
                   Command="{Binding ClearSelectionCommand}"
                   Height="30" Width="130" Margin="5"
                   FontSize="16" FontFamily="ISOCPEUR"/>
        </StackPanel>
    </Grid>
</UserControl>
```

### 6.2 Minimal Code-Behind
```csharp
// Views/MainWindow.xaml.cs
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

## Этап 7: Dependency Injection

### 7.1 IoC контейнер
```csharp
// IoC/ServiceContainer.cs
public static class ServiceContainer
{
    private static readonly IContainer _container;
    
    static ServiceContainer()
    {
        var builder = new ContainerBuilder();
        
        // Register services
        builder.RegisterType<RevitService>().As<IRevitService>().SingleInstance();
        builder.RegisterType<CategoryService>().As<ICategoryService>().SingleInstance();
        builder.RegisterType<FilterService>().As<IFilterService>().SingleInstance();
        
        // Register ViewModels
        builder.RegisterType<MainWindowViewModel>();
        builder.RegisterType<CategorySelectionViewModel>();
        builder.RegisterType<RuleBuilderViewModel>();
        builder.RegisterType<SearchResultsViewModel>();
        
        // Register logging
        builder.RegisterInstance(LogManager.GetLogger("App")).As<ILog>();
        
        _container = builder.Build();
    }
    
    public static T Resolve<T>()
    {
        return _container.Resolve<T>();
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

## Следующие шаги

1. **Создание базовых классов** (BaseModel, BaseViewModel)
2. **Реализация моделей данных**
3. **Создание сервисного слоя**
4. **Разработка ViewModels**
5. **Рефакторинг Views**
6. **Настройка Dependency Injection**
7. **Тестирование и отладка**
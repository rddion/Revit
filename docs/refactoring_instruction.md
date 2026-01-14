# Инструкция по рефакторингу проекта "Revit Advanced Selection Tool"

## Обзор рефакторинга

Проект рефакторится с монолитной архитектуры на **MVVM (Model-View-ViewModel)** паттерн с разделением на слои. Основные изменения:

- **View**: WPF интерфейс (MainWindow.xaml)
- **ViewModel**: Логика представления (MainWindowViewModel.cs)
- **Model**: Модели данных и бизнес-логика
- **Services**: Сервисный слой для работы с Revit API
- **Commands**: Команды для интеграции с Revit

## Целевая структура проекта

```
Revit Advanced Selection Tool/
├── Models/                          # Модели данных
│   ├── Category.cs                  # Модель категории элементов
│   ├── FilterRule.cs                # Модель правила фильтрации
│   ├── ParameterInfo.cs             # Информация о параметре
│   ├── SearchResult.cs              # Результат поиска
│   └── RevitElement.cs              # Модель элемента Revit
├── Interfaces/                      # Интерфейсы
│   ├── INotifyPropertyChanged.cs    # Интерфейс для уведомлений
│   └── IDataErrorInfo.cs            # Интерфейс для валидации
├── Services/                        # Сервисный слой
│   ├── IRevitService.cs             # Интерфейс сервиса Revit
│   ├── RevitService.cs              # Реализация сервиса Revit
│   ├── ICategoryService.cs          # Интерфейс сервиса категорий
│   ├── CategoryService.cs           # Реализация сервиса категорий
│   ├── IFilterService.cs            # Интерфейс сервиса фильтров
│   └── FilterService.cs             # Реализация сервиса фильтров
├── Commands/                        # Команды Revit
│   ├── TroyankaCommand.cs           # Основная команда
│   ├── ButtonApplication.cs         # Приложение кнопки
│   └── CmdFindCommonParameters.cs   # Команда поиска параметров
├── Core/                            # Ядро приложения
│   └── RevitRuleFilter.cs           # Логика фильтрации
└── ViewModels/                      # ViewModels
    └── MainWindowViewModel.cs       # ViewModel главного окна

Wpf/
├── MainWindow.xaml                  # View - пользовательский интерфейс
├── MainWindow.xaml.cs               # Code-behind (минимальный)
├── App.xaml                         # Приложение WPF
└── ViewModels/                      # ViewModels для WPF
    └── MainWindowViewModel.cs       # ViewModel главного окна
```

## Детальное описание компонентов

### 1. Models (Модели данных)

Модели должны реализовывать `INotifyPropertyChanged` для привязки данных.

#### Category.cs
```csharp
public class Category : INotifyPropertyChanged
{
    private string _name;
    private bool _isSelected;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

#### FilterRule.cs
```csharp
public class FilterRule : INotifyPropertyChanged, IDataErrorInfo
{
    private string _parameterName;
    private string _operator;
    private string _value;
    private LogicalOperator _logicalOperator;

    public string ParameterName { get => _parameterName; set { _parameterName = value; OnPropertyChanged(); } }
    public string Operator { get => _operator; set { _operator = value; OnPropertyChanged(); } }
    public string Value { get => _value; set { _value = value; OnPropertyChanged(); } }
    public LogicalOperator LogicalOperator { get => _logicalOperator; set { _logicalOperator = value; OnPropertyChanged(); } }

    // IDataErrorInfo implementation
    public string Error => null;
    public string this[string columnName] => ValidateProperty(columnName);

    private string ValidateProperty(string propertyName)
    {
        // Валидация полей
    }

    public event PropertyChangedEventHandler PropertyChanged;
    // OnPropertyChanged implementation
}
```

### 2. Services (Сервисный слой)

Сервисы должны быть инъектированы через DI и тестируемыми.

#### IRevitService.cs
```csharp
public interface IRevitService
{
    Task<IEnumerable<Category>> GetCategoriesAsync();
    Task<IEnumerable<RevitElement>> GetElementsAsync(IEnumerable<Category> categories);
    Task<IEnumerable<ParameterInfo>> GetCommonParametersAsync(IEnumerable<RevitElement> elements);
    Task SelectElementsAsync(IEnumerable<RevitElement> elements);
    Task InvertSelectionAsync();
}
```

#### RevitService.cs
```csharp
public class RevitService : IRevitService
{
    private readonly UIApplication _uiApp;

    public RevitService(UIApplication uiApp)
    {
        _uiApp = uiApp;
    }

    public async Task<IEnumerable<Category>> GetCategoriesAsync()
    {
        return await Task.Run(() =>
        {
            // Логика получения категорий через Revit API
        });
    }

    // Реализация других методов
}
```

### 3. ViewModels

ViewModel должен содержать всю логику представления и взаимодействовать с сервисами.

#### MainWindowViewModel.cs
```csharp
public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IRevitService _revitService;
    private readonly ICategoryService _categoryService;
    private readonly IFilterService _filterService;

    public MainWindowViewModel(IRevitService revitService, ICategoryService categoryService, IFilterService filterService)
    {
        _revitService = revitService;
        _categoryService = categoryService;
        _filterService = filterService;

        LoadCategoriesCommand = new RelayCommand(async () => await LoadCategoriesAsync());
        ApplyFilterCommand = new RelayCommand(async () => await ApplyFilterAsync());
        // Другие команды
    }

    // Свойства для привязки
    public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();
    public ObservableCollection<FilterRule> FilterRules { get; } = new ObservableCollection<FilterRule>();
    public ObservableCollection<ParameterInfo> Parameters { get; } = new ObservableCollection<ParameterInfo>();

    // Команды
    public ICommand LoadCategoriesCommand { get; }
    public ICommand ApplyFilterCommand { get; }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _revitService.GetCategoriesAsync();
        Categories.Clear();
        foreach (var category in categories)
        {
            Categories.Add(category);
        }
    }

    private async Task ApplyFilterAsync()
    {
        var selectedCategories = Categories.Where(c => c.IsSelected);
        var elements = await _revitService.GetElementsAsync(selectedCategories);
        var filteredElements = await _filterService.ApplyFilterAsync(elements, FilterRules);
        await _revitService.SelectElementsAsync(filteredElements);
    }

    // INotifyPropertyChanged implementation
}
```

### 4. View (Представление)

#### MainWindow.xaml
```xaml
<Window x:Class="Wpf.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Title="Revit Advanced Selection Tool" Height="600" Width="800"
        DataContext="{Binding MainWindowViewModel}">

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <!-- Левая панель - Категории -->
        <StackPanel Grid.Column="0" Margin="10">
            <TextBlock Text="Категории элементов:" FontWeight="Bold" />
            <ListView ItemsSource="{Binding Categories}" SelectionMode="Multiple">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <CheckBox Content="{Binding Name}" IsChecked="{Binding IsSelected}" />
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </StackPanel>

        <!-- Правая панель - Фильтры -->
        <StackPanel Grid.Column="1" Margin="10">
            <TextBlock Text="Правила фильтрации:" FontWeight="Bold" />
            <ListView ItemsSource="{Binding FilterRules}">
                <ListView.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal">
                            <ComboBox ItemsSource="{Binding AvailableParameters}" SelectedValue="{Binding ParameterName}" Width="150" />
                            <ComboBox ItemsSource="{Binding AvailableOperators}" SelectedValue="{Binding Operator}" Width="100" />
                            <TextBox Text="{Binding Value}" Width="100" />
                            <ComboBox ItemsSource="{Binding AvailableLogicalOperators}" SelectedValue="{Binding LogicalOperator}" Width="100" />
                        </StackPanel>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>

            <StackPanel Orientation="Horizontal" Margin="0,10,0,0">
                <Button Content="Добавить правило" Command="{Binding AddFilterRuleCommand}" />
                <Button Content="Удалить правило" Command="{Binding RemoveFilterRuleCommand}" />
            </StackPanel>

            <StackPanel Orientation="Horizontal" Margin="0,10,0,0">
                <Button Content="Применить фильтр" Command="{Binding ApplyFilterCommand}" />
                <Button Content="Инвертировать выбор" Command="{Binding InvertSelectionCommand}" />
            </StackPanel>
        </StackPanel>
    </Grid>
</Window>
```

#### MainWindow.xaml.cs
```csharp
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Инициализация ViewModel через DI или фабрику
        DataContext = new MainWindowViewModel(/* сервисы */);
    }
}
```

### 5. Commands (Команды Revit)

Команды должны использовать сервисный слой вместо прямого доступа к UI.

#### TroyankaCommand.cs
```csharp
public class TroyankaCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            var uiApp = commandData.Application;
            var revitService = new RevitService(uiApp);

            // Показать WPF окно
            var mainWindow = new MainWindow();
            var viewModel = new MainWindowViewModel(revitService, /* другие сервисы */);
            mainWindow.DataContext = viewModel;
            mainWindow.ShowDialog();

            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
```

## Принципы рефакторинга

1. **Разделение ответственностей**: Каждый класс имеет одну ответственность
2. **Инверсия зависимостей**: Зависимости инъектируются через конструкторы
3. **Привязка данных**: Все взаимодействия через Data Binding
4. **Асинхронность**: Длительные операции выполняются асинхронно
5. **Тестируемость**: Код должен быть легко тестируемым
6. **Валидация**: Данные валидируются на уровне моделей

## Порядок выполнения рефакторинга

1. Создать модели данных и интерфейсы
2. Реализовать сервисный слой
3. Создать ViewModel с командами
4. Обновить View для использования Data Binding
5. Рефакторить команды Revit для использования сервисов
6. Удалить старый код (Class1.cs, Class2.cs, etc.)
7. Протестировать функциональность

После рефакторинга код станет более поддерживаемым, тестируемым и расширяемым.
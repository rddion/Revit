# Анализ текущей архитектуры проекта "Revit Advanced Selection Tool"

## Обзор проекта

Проект представляет собой инструмент для продвинутого выбора элементов в Autodesk Revit, состоящий из двух основных компонентов:
- **Revit Advanced Selection Tool** - библиотека для интеграции с Revit API
- **Wpf** - WPF приложение с пользовательским интерфейсом

## Текущая архитектура

### Структура проекта
```
Revit Advanced Selection Tool/
├── Class1.cs           - Основная логика интеграции с Revit
├── Class2.cs           - Создание UI кнопки в Revit
├── Class3.cs           - Вспомогательные классы для работы с параметрами
├── Class4.cs           - Логика фильтрации элементов
└── Properties/

Wpf/
├── MainWindow.xaml     - UI интерфейс
├── MainWindow.xaml.cs  - Code-behind с бизнес-логикой
├── App.xaml           - Приложение
└── Properties/
```

### Функциональные возможности
1. **Выбор категорий элементов Revit** - отображение и выбор из списка доступных категорий
2. **Конструктор правил** - создание условий фильтрации на основе параметров элементов
3. **Поиск и выбор** - применение правил для выделения элементов в Revit
4. **Инвертирование выбора** - обратная логика выбора элементов

## Проблемы текущей архитектуры

### 1. Нарушение MVVM паттерна
- **Вся бизнес-логика находится в code-behind** (`MainWindow.xaml.cs`)
- **Прямые обращения к UI элементам** из программного кода
- **Отсутствие разделения ответственности** между слоями

### 2. Сложность сопровождения
- **Монолитный класс `MainWindow.xaml.cs`** (567 строк кода)
- **Статические поля** для передачи данных между компонентами
- **Смешение UI логики с бизнес-логикой**

### 3. Проблемы с тестируемостью
- **Невозможность юнит-тестирования** бизнес-логики
- **Зависимость от UI компонентов** в логических классах
- **Сложность мокирования** для тестов

### 4. Проблемы с расширяемостью
- **Жесткая связь** между WPF UI и Revit API
- **Сложность добавления новых функций**
- **Трудности с изменением интерфейса**

### 5. Проблемы с данными
- **Использование статических коллекций** для хранения состояния
- **Отсутствие валидации данных** на уровне модели
- **Непоследовательная обработка ошибок**

## Детальный анализ проблем в коде

### Проблемы в MainWindow.xaml.cs

#### 1. Нарушение принципа единственной ответственности
```csharp
// В одном классе смешаны:
// - Управление UI элементами
// - Бизнес-логика
// - Обработка данных
// - Взаимодействие с Revit API
public partial class MainWindow : Window
{
    List<string> list = new List<string>();
    public static ObservableCollection<string> strings = new ObservableCollection<string>();
    Dictionary<int, UIElement> conditionElements = new Dictionary<int, UIElement>();
    // ... более 20 полей класса
}
```

#### 2. Прямые манипуляции с UI
```csharp
private void Button_Click(object sender, RoutedEventArgs e)
{
    // Прямое изменение UI элементов
    if(search.Text.Length > 0)
    {
        search.Text = "";
    }
    // Смешение UI и бизнес-логики
    exitSelect.Clear();
    foreach(string category in selectCategories)
    {
        exitSelect.Add(category);
    }
    lView.ItemsSource = selectCategories;
}
```

#### 3. Статические поля для передачи данных
```csharp
public static ObservableCollection<string> strings = new ObservableCollection<string>();
public static string[,] uslovia =new string[0,3];
public static string[] unions = new string[0];
public static IList selectCategories = new List<string>();
public static List<string> exitSelect= new List<string>();
```

### Проблемы в Class1.cs

#### 1. Смешение ответственностей
```csharp
public class ll
{
    private readonly Wpf.MainWindow _mainWindow; // Зависимость от UI
    // Логика работы с Revit API перемешана с UI логикой
}
```

#### 2. Прямые обращения к UI из бизнес-логики
```csharp
_mainWindow.exitParameters.Clear();
foreach (var p in commonParams)
{
    _mainWindow.exitParameters.Add($"{p.Name}");
    _mainWindow.storageTypesOfParameters.Add(p.StorageType.ToString());
}
```

## Выводы

Текущая архитектура **не соответствует современным принципам разработки** и требует кардинального рефакторинга для:

1. **Повышения качества кода**
2. **Улучшения тестируемости**
3. **Облегчения сопровождения**
4. **Повышения расширяемости**
5. **Соблюдения принципов SOLID**

Следующим шагом будет проектирование новой MVVM архитектуры, которая решит выявленные проблемы.
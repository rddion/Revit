using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Reflection;
using RevitAdvancedSelectionTool.Services;
using RevitAdvancedSelectionTool.Models;
using RevitAdvancedSelectionTool.Core;
using FilterRule = RevitAdvancedSelectionTool.Models.FilterRule;
using Category = Autodesk.Revit.DB.Category;
using ParameterInfo = RevitAdvancedSelectionTool.Models.ParameterInfo;

namespace Troyan
{
    public static class SharedData
    {
        public static System.Collections.ObjectModel.ObservableCollection<string> exitParameters;
        public static System.Collections.ObjectModel.ObservableCollection<string> storageTypesOfParameters;
        public static System.Collections.ObjectModel.ObservableCollection<string> exitSelect;
        public static string[,] uslovia;
        public static string[] unions;
        public static List<CategoryInfo> categories;
        public static Autodesk.Revit.DB.Document doc;
        public static Autodesk.Revit.UI.UIDocument uidoc;
        public static ExternalEvent GetParamsEvent;
        public static ExternalEvent ApplyFilterEvent;
        public static ExternalEvent InvertEvent;

        static SharedData()
        {
            exitParameters = new System.Collections.ObjectModel.ObservableCollection<string>();
            storageTypesOfParameters = new System.Collections.ObjectModel.ObservableCollection<string>();
            exitSelect = new System.Collections.ObjectModel.ObservableCollection<string>();
            uslovia = new string[0, 3];
            unions = new string[0];
            categories = new List<CategoryInfo>();
        }
    }

    public class GetParametersHandler : IExternalEventHandler
    {
        private readonly IRevitService _revitService;
        private readonly IFilterService _filterService;

        public GetParametersHandler(IRevitService revitService, IFilterService filterService)
        {
            _revitService = revitService;
            _filterService = filterService;
        }

        public void Execute(UIApplication app)
        {
            var doc = app.ActiveUIDocument.Document;
            // Реализация получения общих параметров
            var parameterNames = GetCommonParameterNames(SharedData.exitSelect, doc);
            var storageTypes = GetCommonParameterStorageTypes(SharedData.exitSelect, doc);
            SharedData.exitParameters = parameterNames;
            SharedData.storageTypesOfParameters = storageTypes;
        }

        public static System.Collections.ObjectModel.ObservableCollection<string> GetCommonParameterNames(System.Collections.ObjectModel.ObservableCollection<string> selectedCategoryNames, Document doc)
        {
            var commonParams = GetCommonParameters(selectedCategoryNames, doc);
            return new System.Collections.ObjectModel.ObservableCollection<string>(commonParams.OrderBy(p => p.Name).Select(p => p.Name));
        }

        public static System.Collections.ObjectModel.ObservableCollection<string> GetCommonParameterStorageTypes(System.Collections.ObjectModel.ObservableCollection<string> selectedCategoryNames, Document doc)
        {
            var commonParams = GetCommonParameters(selectedCategoryNames, doc);
            return new System.Collections.ObjectModel.ObservableCollection<string>(commonParams.OrderBy(p => p.Name).Select(p => p.StorageType.ToString()));
        }

        private static List<ParameterInfo> GetCommonParameters(System.Collections.ObjectModel.ObservableCollection<string> selectedCategoryNames, Document doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));
            if (selectedCategoryNames == null || selectedCategoryNames.Count == 0)
                return new List<ParameterInfo>();

            // 1. Получить все категории документа
            var allDocCategories = new List<CategoryInfo>();
            foreach (Category cat in doc.Settings.Categories)
            {
                if (cat != null && cat.Name != null && cat.Id != null)
                {
                    allDocCategories.Add(new CategoryInfo
                    {
                        Name = cat.Name,
                        Id = cat.Id.IntegerValue
                    });
                }
            }
            // 2. Создать словарь имя -> int
            var nameToId = allDocCategories
                .GroupBy(c => c.Name) // по имени
                .ToDictionary(g => g.Key, g => g.First().Id);

            // 3. Получить ElementId для выбранных категорий
            var selectedIds = new List<ElementId>();
            foreach (var name in selectedCategoryNames)
            {
                if (nameToId.TryGetValue(name, out int id))
                {
                    selectedIds.Add(new ElementId(id));
                }
            }

            if (selectedIds.Count == 0)
                return new List<ParameterInfo>();

            // 4. Получить по одному элементу из каждой категории
            var sampleElements = new List<Element>();
            foreach (ElementId id in selectedIds)
            {
                var collector = new FilteredElementCollector(doc)
                    .OfCategoryId(id)
                    .WhereElementIsNotElementType();

                var element = collector.FirstOrDefault();
                if (element != null)
                {
                    sampleElements.Add(element);
                }
            }

            if (sampleElements.Count == 0)
                return new List<ParameterInfo>();

            // 5. Получить параметры каждого элемента
            var paramLists = new List<List<ParameterInfo>>();
            foreach (Element elem in sampleElements)
            {
                var elemParams = new List<ParameterInfo>();
                foreach (Parameter param in elem.Parameters)
                {
                    if (param?.Definition?.Name != null)
                    {
                        elemParams.Add(new ParameterInfo
                        {
                            Name = param.Definition.Name,
                            StorageType = param.StorageType
                        });
                    }
                }
                paramLists.Add(elemParams);
            }

            // 6. Найти пересечение параметров
            var commonParams = new HashSet<ParameterInfo>(paramLists[0]);
            for (int i = 1; i < paramLists.Count; i++)
            {
                commonParams.IntersectWith(paramLists[i]);
            }

            return commonParams.ToList();
        }
        public string GetName() => "GetParameters";
    }

    public class ApplyFilterHandler : IExternalEventHandler
    {
        private readonly IRevitService _revitService;

        public ApplyFilterHandler(IRevitService revitService)
        {
            _revitService = revitService;
        }

        public void Execute(UIApplication app)
        {
            // Реализация поиска и выбора
            RevitRuleFilter.ApplyFilterAndSelect(app.ActiveUIDocument);
        }

        private List<FilterRule> ConvertUsloviaToRules()
        {
            var rules = new List<FilterRule>();
            int conditionCount = SharedData.uslovia.GetLength(0);

            for (int i = 0; i < conditionCount; i++)
            {
                var rule = new FilterRule
                {
                    ParameterName = SharedData.uslovia[i, 0]?.Trim() ?? "",
                    Value = SharedData.uslovia[i, 2] ?? ""
                };

                string opStr = SharedData.uslovia[i, 1]?.Trim() ?? "Равно";
                try
                {
                    rule.Operator = FilterRule.ParseRussianOperator(opStr);
                }
                catch (ArgumentException)
                {
                    rule.Operator = RuleOperator.Equals;
                }

                rules.Add(rule);
            }

            return rules;
        }

        public string GetName() => "ApplyFilter";
    }

    public class InvertSelectionHandler : IExternalEventHandler
    {
        private readonly IRevitService _revitService;

        public InvertSelectionHandler(IRevitService revitService)
        {
            _revitService = revitService;
        }

        public void Execute(UIApplication app)
        {
            var uiDoc = app.ActiveUIDocument;
            // Реализация инверсии
            var passedIds = RevitRuleFilter.GetFilteredElementIds(uiDoc, out List<Element> allElementsInCategories);
            var notPassedIds = allElementsInCategories
                .Where(e => !passedIds.Contains(e.Id))
                .Select(e => e.Id)
                .ToList();
            uiDoc.Selection.SetElementIds(notPassedIds);
        }
        public string GetName() => "InvertSelection";
    }

    [DataContract]
    public class CategoryInfo
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Type { get; set; }
    }

    [DataContract]
    public class RevitDataMessage
    {
        [DataMember]
        public List<CategoryInfo> Categories { get; set; }
    }

    [Transaction(TransactionMode.ReadOnly)]
    public class TroyankaCommand : IExternalCommand
    {
        private static bool test = true;
        public static bool Test { get { return test; } set { test = value; } }

        private IRevitService _revitService;
        private ICategoryService _categoryService;
        private IFilterService _filterService;

        public TroyankaCommand()
        {
        }

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            _revitService = new RevitService();
            _categoryService = new CategoryService();
            _filterService = new FilterService();

            var doc = commandData.Application.ActiveUIDocument.Document;
            SharedData.doc = doc;
            var uiDoc = commandData.Application.ActiveUIDocument;
            SharedData.uidoc = uiDoc;
            var categories = GetCategories(doc);
            SharedData.categories = categories;
            SharedData.exitSelect = new System.Collections.ObjectModel.ObservableCollection<string>(categories.Select(c => c.Name));
            test = true;
            var categoryNames = categories.Select(c => c.Name).ToList();

            SendToWpfApp(categories, categoryNames);
            // Создание events
            SharedData.GetParamsEvent = ExternalEvent.Create(new GetParametersHandler(_revitService, _filterService));
            SharedData.ApplyFilterEvent = ExternalEvent.Create(new ApplyFilterHandler(_revitService));
            SharedData.InvertEvent = ExternalEvent.Create(new InvertSelectionHandler(_revitService));

            // Запуск WPF
            string revitBinDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); // D:\lk\Revit Advanced Selection Tool\bin\Debug
            string binDir = Path.GetDirectoryName(revitBinDir); // D:\lk\Revit Advanced Selection Tool\bin
            string projectDir = Path.GetDirectoryName(binDir); // D:\lk\Revit Advanced Selection Tool
            string solutionDir = Path.GetDirectoryName(projectDir); // D:\lk
            string wpfDllPath = Path.Combine(solutionDir, "Wpf", "bin", "Debug", "Wpf.dll"); // D:\lk\Wpf\bin\Debug\Wpf.dll
            Assembly wpfAssembly = Assembly.LoadFrom(wpfDllPath);
            Type mainWindowType = wpfAssembly.GetType("Wpf.MainWindow");
            object mainWindow = Activator.CreateInstance(mainWindowType);

           
            // Показать
            var showMethod = mainWindowType.GetMethod("Show");
            showMethod.Invoke(mainWindow, null);
            return Result.Succeeded;

        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Test = false;
        }

        private List<CategoryInfo> GetCategories(Document doc)
        {
            var categories = new List<CategoryInfo>();
            var cats = doc.Settings.Categories;

            foreach (Category cat in cats)
            {
                if (cat == null || string.IsNullOrWhiteSpace(cat.Name))
                    continue;

                // 1. Исключаем все аннотации
                if (cat.CategoryType == CategoryType.Annotation)
                    continue;
                if (cat.CategoryType != CategoryType.Model)
                    continue;

                // 2. Исключаем "Линии"
                if (cat.Id.IntegerValue == (int)BuiltInCategory.OST_Lines)
                    continue;

                // 3. Исключаем всё, что содержит "Материал"
                if (cat.Name.Contains("Материал"))
                    continue;

                categories.Add(new CategoryInfo
                {
                    Id = cat.Id.IntegerValue,
                    Name = cat.Name,
                    Type = cat.CategoryType.ToString()
                });
            }

            return categories.OrderBy(c => c.Name).ToList();
        }

        private void SendToWpfApp(List<CategoryInfo> categories, List<string> categoryNames)
        {

            SaveToJsonFile(categories);

        }
        private void SaveToJsonFile(List<CategoryInfo> categories)
        {
            var message = new RevitDataMessage { Categories = categories };
            var serializer = new DataContractJsonSerializer(typeof(RevitDataMessage));

            string folderPath = @"U:\02_Projects\0359-(50-23)\00.Моделирование\ТХ\НПО_Пассат системы\00_Проект\Клемантович\Плагин";
            Directory.CreateDirectory(folderPath);
            string filePath = Path.Combine(folderPath, "RevitCategories.json");

            using (var stream = File.Create(filePath))
            {
                serializer.WriteObject(stream, message);
            }

            TaskDialog.Show("Revit", $"Категории сохранены!\nВсего: {categories.Count}");
        }

        public static (System.Collections.ObjectModel.ObservableCollection<string> names, System.Collections.ObjectModel.ObservableCollection<string> types) GetParametersForCategories(System.Collections.ObjectModel.ObservableCollection<string> categories)
        {
            var names = GetParametersHandler.GetCommonParameterNames(categories, SharedData.doc);
            var types = GetParametersHandler.GetCommonParameterStorageTypes(categories, SharedData.doc);
            return (names, types);
        }

        public static System.Collections.ObjectModel.ObservableCollection<string> GetParameterNamesForCategories(System.Collections.ObjectModel.ObservableCollection<string> categories)
        {
            return GetParametersHandler.GetCommonParameterNames(categories, SharedData.doc);
        }

        public static System.Collections.ObjectModel.ObservableCollection<string> GetParameterStorageTypesForCategories(System.Collections.ObjectModel.ObservableCollection<string> categories)
        {
            return GetParametersHandler.GetCommonParameterStorageTypes(categories, SharedData.doc);
        }
    }
}
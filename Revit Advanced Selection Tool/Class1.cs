using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
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
using System.Windows.Documents;
using System.Reflection;
using YourRevitPluginNamespace;

namespace Troyan
{
    public static class SharedData
    {
        public static System.Collections.ObjectModel.ObservableCollection<string> exitParameters = new System.Collections.ObjectModel.ObservableCollection<string>();
        public static System.Collections.ObjectModel.ObservableCollection<string> storageTypesOfParameters = new System.Collections.ObjectModel.ObservableCollection<string>();
        public static List<string> exitSelect = new List<string>();
        public static string[,] uslovia = new string[0,3];
        public static string[] unions = new string[0];
        public static ExternalEvent GetParamsEvent;
        public static ExternalEvent ApplyFilterEvent;
        public static ExternalEvent InvertEvent;
    }

    public class GetParametersHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            var doc = app.ActiveUIDocument.Document;
            var commonParams = ParameterIntersectionHelper.GetCommonParameters(doc);
            SharedData.exitParameters.Clear();
            SharedData.storageTypesOfParameters.Clear();
            if (commonParams.Any())
            {
                foreach (var p in commonParams)
                {
                    SharedData.exitParameters.Add($"{p.Name}");
                    SharedData.storageTypesOfParameters.Add(p.StorageType.ToString());
                }
            }
        }
        public string GetName() => "GetParameters";
    }

    public class ApplyFilterHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            var uiDoc = app.ActiveUIDocument;
            RevitRuleFilter.ApplyFilterAndSelect(uiDoc);
        }
        public string GetName() => "ApplyFilter";
    }

    public class InvertSelectionHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            var uiDoc = app.ActiveUIDocument;
            RevitNot.GOG(uiDoc);
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
    public class Troyanka : IExternalCommand
    {
        private static bool test = true;
        public static bool Test { get { return test; } set { test = value; } }
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            var uiDoc = commandData.Application.ActiveUIDocument;
            var categories = GetCategories(doc);
            test = true;
            var categoryNames = categories.Select(c => c.Name).ToList();

            SendToWpfApp(categories, categoryNames);
            // Создать events
            SharedData.GetParamsEvent = ExternalEvent.Create(new GetParametersHandler());
            SharedData.ApplyFilterEvent = ExternalEvent.Create(new ApplyFilterHandler());
            SharedData.InvertEvent = ExternalEvent.Create(new InvertSelectionHandler());

            // Загрузить WPF
            string revitBinDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); // D:\lk\Revit Advanced Selection Tool\bin\Debug
            string binDir = Path.GetDirectoryName(revitBinDir); // D:\lk\Revit Advanced Selection Tool\bin
            string projectDir = Path.GetDirectoryName(binDir); // D:\lk\Revit Advanced Selection Tool
            string solutionDir = Path.GetDirectoryName(projectDir); // D:\lk
            string wpfDllPath = Path.Combine(solutionDir, "Wpf", "bin", "Debug", "Wpf.dll"); // D:\lk\Wpf\bin\Debug\Wpf.dll
            Assembly wpfAssembly = Assembly.LoadFrom(wpfDllPath);
            Type mainWindowType = wpfAssembly.GetType("Wpf.MainWindow");
            object mainWindow = Activator.CreateInstance(mainWindowType, categoryNames);

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
    }
}

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Windows.Documents;
using Wpf;
using YourRevitPluginNamespace;

namespace Troyan
{
   public class ll
    {
        public Document _doc;
        public UIDocument _docs;
        private readonly SynchronizationContext _uiContext;
        private readonly Wpf.MainWindow _mainWindow;
        private static object _locker = new object();
        public ll(Document doc, Wpf.MainWindow mainWindow, UIDocument docs)
        {
            _docs = docs;
            _doc = doc;
            _uiContext = SynchronizationContext.Current;
            _mainWindow = mainWindow;
        }
        public void lol(object sender, EventArgs e)
        {
            //lock (_locker)
            //{
            //    //while (true)
            //{
            //if (Troyanka.Test)
            //{
                //while (!Wpf.MainWindow.proverka)
                //{
                //    Thread.Sleep(100);
                //}

                var commonParams = ParameterIntersectionHelper.GetCommonParameters(_doc);

                _mainWindow.exitParameters.Clear();
                //  4. Показываем результат
                if (commonParams.Any())
                {
                    foreach (var p in commonParams)
                    {
                        _mainWindow.exitParameters.Add($"{p.Name}");
                        _mainWindow.storageTypesOfParameters.Add(p.StorageType.ToString());
                    }
                }


                //Wpf.MainWindow.proverka = false;
            
            //        }
            //        else break;
            //    }
            //    Wpf.MainWindow.proverka = false;
            //    Thread.CurrentThread.Abort();
            //}
        }
        public void slol(object sender, EventArgs e) 
        {
            RevitRuleFilter.ApplyFilterAndSelect(_docs);
        }
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
            // Получаем отфильтрованные категории
            var categories = GetCategories(doc);
            test = true;
            // Создаём массив ТОЛЬКО из имён
            var categoryNames = categories.Select(c => c.Name).ToList();

            // Передаём данные дальше 
            SendToWpfApp(categories, categoryNames);
            Wpf.MainWindow mainWindow = new Wpf.MainWindow(categoryNames);
   
            ll ll = new ll(doc, mainWindow, uiDoc);
            mainWindow.@event += ll.lol;
            mainWindow.SearchingEvent += ll.slol;
            
            mainWindow.Show();
            // ThreadStart threadStart = new ThreadStart(ll.lol);
            //Thread threadStop = new Thread(ll.lol);
            //threadStop.IsBackground = false;
            //threadStop.Start();
            //mainWindow.Closing += MainWindow_Closing;

            // var commonParams = ParameterIntersectionHelper.GetCommonParameters(doc);
            //  2. ТОЛЬКО ТЕПЕРЬ проверяем флаг
            //if (Wpf.MainWindow.proverka == true)
            //{
            //    //  3. Запускаем анализ

            //    //  4. Показываем результат
            //    if (commonParams.Any())
            //    {
            //        string text = string.Join("\n",
            //            commonParams.Select(p => $"{p.Name} → {p.StorageType}"));
            //        TaskDialog.Show("Общие параметры",
            //            $"Найдено: {commonParams.Count}\n\n{text}");

            //    }
            //    else
            //    {
            //        TaskDialog.Show("Результат", "Общих параметров не найдено.");
            //    }
            //}

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
            // categories — полные данные
            // categoryNames — только имена: ["Стены", "Окна", "Воздуховоды",]

            // Пример: вывести первые 5 имён
            // var preview = string.Join(", ", categoryNames.Take(5));
            // TaskDialog.Show("Имена", preview);

            SaveToJsonFile(categories);

            // Опционально: сохранить только имена в отдельный файл
            // SaveNamesToFile(categoryNames);
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
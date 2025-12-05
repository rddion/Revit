using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Wpf;
using YourRevitPluginNamespace;

namespace Troyan
{
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
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
            // Получаем отфильтрованные категории
            var categories = GetCategories(doc);

            // Создаём массив ТОЛЬКО из имён
            var categoryNames = categories.Select(c => c.Name).ToList();

            // Передаём данные дальше 
            SendToWpfApp(categories, categoryNames);
            Wpf.MainWindow mainWindow = new Wpf.MainWindow(categoryNames);
            mainWindow.ShowDialog(); // ← ЖДЁМ, пока пользователь закроет окно!

            //  2. ТОЛЬКО ТЕПЕРЬ проверяем флаг
            if (Wpf.MainWindow.proverka == true)
            {
                //  3. Запускаем анализ
                var commonParams = ParameterIntersectionHelper.GetCommonParameters(doc);

                //  4. Показываем результат
                if (commonParams.Any())
                {
                    string text = string.Join("\n",
                        commonParams.Select(p => $"{p.Name} → {p.StorageType}"));
                    TaskDialog.Show("Общие параметры",
                        $"Найдено: {commonParams.Count}\n\n{text}");
                }
                else
                {
                    TaskDialog.Show("Результат", "Общих параметров не найдено.");
                }
            }

            return Result.Succeeded;
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
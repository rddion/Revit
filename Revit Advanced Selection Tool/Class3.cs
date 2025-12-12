using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;


namespace YourRevitPluginNamespace
{
    // Вспомогательный класс для хранения параметра (имя + тип)
    public class ParameterInfo : IEquatable<ParameterInfo>
    {
        public string Name { get; set; }
        public StorageType StorageType { get; set; }

        public bool Equals(ParameterInfo other)
        {
            if (other == null) return false;
            return Name == other.Name && StorageType == other.StorageType;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ParameterInfo);
        }

        public override int GetHashCode()
        {
            return (Name?.GetHashCode() ?? 0) ^ (int)StorageType;
        }
    }

    // Класс с выбранными именами категорий (внешний)
    public static class MainWindow
    {
        // Пример (на практике заполняется из UI)
        public static List<string> strings = new List<string>();
    }

    // Основной класс команды или утилиты
    public class ParameterIntersectionHelper
    {
        /// <summary>
        /// Находит общие параметры у элементов из выбранных категорий (Troyan.categoryNames).
        /// </summary>
        /// <param name="doc">Текущий документ Revit</param>
        /// <returns>Список общих параметров</returns>
        /// 
        //static object obj = new object();

        public static List<ParameterInfo> GetCommonParameters(Document doc)
        {
            //lock (obj)
            //{
                if (doc == null)
                    throw new ArgumentNullException(nameof(doc));
                var result = Wpf.MainWindow.exitSelect;
                var selectedCategoryNames = new List<string>(result);
                if (selectedCategoryNames == null || selectedCategoryNames.Count == 0)
                    return new List<ParameterInfo>();

                // 1. Получаем все категории документа
                var allDocCategories = new List<CategoryInfo>();
                foreach (Category cat in doc.Settings.Categories)
                {
                    if (cat != null && cat.Name != null && cat.Id != null)
                    {
                        allDocCategories.Add(new CategoryInfo
                        {
                            Name = cat.Name,
                            Id = cat.Id
                        });
                    }

                }
                // 2. Создаём словарь имя -> ElementId
                var nameToId = allDocCategories
                    .GroupBy(c => c.Name) // на случай дубликатов
                    .ToDictionary(g => g.Key, g => g.First().Id);

                // 3. Получаем ElementId для выбранных имён
                var selectedIds = new List<ElementId>();
                foreach (var name in selectedCategoryNames)
                {
                    if (nameToId.TryGetValue(name, out ElementId id))
                    {
                        selectedIds.Add(id);
                    }
                }

                if (selectedIds.Count == 0)
                    return new List<ParameterInfo>();

                // 4. Берём по одному элементу из каждой категории
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

                // 5. Собираем параметры каждого элемента
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

                // 6. Находим пересечение всех списков
                var commonParams = new HashSet<ParameterInfo>(paramLists[0]);
                for (int i = 1; i < paramLists.Count; i++)
                {
                    commonParams.IntersectWith(paramLists[i]);
                }

                return commonParams.ToList();
            //}
        }
        
    }

    // Вспомогательный класс для хранения категории
    public class CategoryInfo
    {
        public string Name { get; set; }
        public ElementId Id { get; set; }
    }
    // Для кнопки (можно удалить)
    [Transaction(TransactionMode.ReadOnly)]
    public class CmdFindCommonParameters : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            Autodesk.Revit.DB.ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // 🔥 Запускаем вашу логику
                var commonParams = ParameterIntersectionHelper.GetCommonParameters(doc);

                // 📤 Вывод результата
                if (commonParams.Any())
                {
                    string resultText = string.Join("\n",
                        commonParams.Select(p => $"{p.Name} → {p.StorageType}"));

                    TaskDialog.Show("Общие параметры",
                        $"Найдено общих параметров: {commonParams.Count}\n\n{resultText}");
                }
                else
                {
                    TaskDialog.Show("Общие параметры",
                        "Общих параметров у выбранных категорий не найдено.");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Ошибка", ex.ToString());
                return Result.Failed;
            }
        }
    }
}
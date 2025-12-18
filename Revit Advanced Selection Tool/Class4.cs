using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public static class RevitRuleFilter
{
    public enum RuleOperator
    {
        Equals,
        NotEquals,
        Contains,
        StartsWith,
        GreaterThan,
        LessThan
    }
    private static BuiltInCategory? FindBuiltInCategoryByName(Document doc, string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName) || doc == null)
            return null;

        foreach (Category cat in doc.Settings.Categories)
        {
            if (cat != null && cat.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase))
            {
                if (cat.Id.IntegerValue < 0) // Built-in category
                    return (BuiltInCategory)cat.Id.IntegerValue;
            }
        }
        return null;
    }
    public static void ApplyFilterAndSelect(UIDocument uidoc)
    {
        for (int i = 0; i < Wpf.MainWindow.uslovia.GetLength(0); i++)
        {
            for (int r = 0; r < Wpf.MainWindow.uslovia.GetLength(1); r++)
            {
                switch (Wpf.MainWindow.uslovia[i, r])
                {
                    case "Равно":
                        Wpf.MainWindow.uslovia[i, r] = "Equals";
                        break;
                    case "Не равно":
                        Wpf.MainWindow.uslovia[i, r] = "NotEquals";
                        break;
                    case "Содержит":
                        Wpf.MainWindow.uslovia[i, r] = "Contains";
                        break;
                    case "Начинается с":
                        Wpf.MainWindow.uslovia[i, r] = "StartsWith";
                        break;
                    case "Больше":
                        Wpf.MainWindow.uslovia[i, r] = "GreaterThan";
                        break;
                    case "Меньше":
                        Wpf.MainWindow.uslovia[i, r] = "LessThan";
                        break;
                    default:
                        break;
                }
            }
        }
        if (uidoc == null) throw new ArgumentNullException(nameof(uidoc));

        int conditionCount = Wpf.MainWindow.uslovia.GetLength(0);
        if (conditionCount == 0)
        {
            uidoc.Selection.SetElementIds(new List<ElementId>());
            return;
        }

        // Извлекаем данные из uslovia
        string[] paramNames = new string[conditionCount];
        RuleOperator[] operators = new RuleOperator[conditionCount];
        string[] values = new string[conditionCount];

        for (int i = 0; i < conditionCount; i++)
        {
            paramNames[i] = Wpf.MainWindow.uslovia[i, 0]?.Trim() ?? "";
            string opStr = Wpf.MainWindow.uslovia[i, 1]?.Trim() ?? "Equals";
            values[i] = Wpf.MainWindow.uslovia[i, 2] ?? "";

            if (!Enum.TryParse(opStr, true, out RuleOperator op))
            {
                op = RuleOperator.Equals;
            }
            operators[i] = op;
        }

        // Разбиваем условия на группы по связке "ИЛИ"
        var groups = new List<List<(string paramName, RuleOperator op, string value)>>();

        var currentGroup = new List<(string, RuleOperator, string)>();
        groups.Add(currentGroup);

        int maxUnions = Math.Min(Wpf.MainWindow.unions?.Length ?? 0, conditionCount - 1);
        for (int i = 0; i < conditionCount; i++)
        {
            currentGroup.Add((paramNames[i], operators[i], values[i]));

            if (i < maxUnions && string.Equals(Wpf.MainWindow.unions[i]?.Trim(), "ИЛИ", StringComparison.OrdinalIgnoreCase))
            {
                currentGroup = new List<(string, RuleOperator, string)>();
                groups.Add(currentGroup);
            }
        }

        // Собираем все элементы
        Document doc = uidoc.Document;
        List<Element> allElements = new List<Element>();

        var selectedCategories = Wpf.MainWindow.exitSelect;

        // Если список категорий задан и не пуст
        if (selectedCategories != null && selectedCategories.Count > 0)
        {
            bool foundAtLeastOne = false;

            foreach (string categoryName in selectedCategories)
            {
                if (string.IsNullOrWhiteSpace(categoryName)) continue;

                var bic = FindBuiltInCategoryByName(doc, categoryName);
                if (bic.HasValue)
                {
                    foundAtLeastOne = true;
                    var elementsInCategory = new FilteredElementCollector(doc)
                        .OfCategory(bic.Value)
                        .WhereElementIsNotElementType()
                        .ToList();

                    allElements.AddRange(elementsInCategory);
                }
                // Игнорируем категории, которые не найдены (или выводим предупреждение)
            }

            if (!foundAtLeastOne)
            {
                TaskDialog.Show("Ошибка", "Ни одна из выбранных категорий не найдена в проекте.");
                uidoc.Selection.SetElementIds(new List<ElementId>());
                return;
            }
        }
        else
        {
            // Если категории не выбраны — берём все (как раньше)
            allElements = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .Where(e => e?.Category != null)
                .ToList();
        }

        // Фильтруем по группам: (группа1) OR (группа2) ...
        var resultElements = new HashSet<ElementId>();

        foreach (var group in groups)
        {
            var matchedInGroup = allElements.Where(el =>
            {
                foreach (var (paramName, op, value) in group)
                {
                    if (!MatchesRule(el, paramName, op, value, doc))
                        return false; // Не прошёл хотя бы одно условие в группе
                }
                return true; // Прошёл все условия в группе
            });

            foreach (var el in matchedInGroup)
            {
                resultElements.Add(el.Id);
            }
        }

        // Выделяем результат
        uidoc.Selection.SetElementIds(resultElements.ToList());
    }

    private static bool MatchesRule(Element element, string paramName, RuleOperator op, string userValue, Document doc)
    {
        Parameter param = element.LookupParameter(paramName);
        if (param == null) return false;

        object actual = GetParameterValue(param, doc); // без округления
        object expected = ParseToType(actual, userValue, param, doc);

        if (expected == null) expected = userValue;

        // Определить количество знаков после запятой в expected, если это число
        int decimalPlaces = CountDecimalPlaces(expected);



        return Compare(actual, expected, op, doc);
    }
    private static int CountDecimalPlaces(object value)
    {
        if (value is string str)
        {
            // Заменяем запятую на точку, чтобы парсить как число
            str = str.Replace(',', '.');
            if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                int index = str.IndexOf('.');
                if (index >= 0)
                {
                    return str.Length - index - 1;
                }
            }
            return -1; // не число или нет дробной части
        }

        if (IsNumeric(value))
        {
            string stra = Convert.ToDouble(value).ToString(CultureInfo.InvariantCulture);
            int index = stra.IndexOf('.');
            if (index >= 0)
            {
                return stra.Length - index - 1;
            }
        }

        return -1; // не число
    }
    private static object GetParameterValue(Parameter param, Document doc)
    {
        switch (param.StorageType)
        {
            case StorageType.String:
                return param.AsString();
            case StorageType.Integer:
                return param.AsInteger();
            case StorageType.Double:
                var value = param.AsDouble();
                var displayUnit = param.GetUnitTypeId();
                // Преобразуем значение из внутренних единиц (футы) в отображаемые
                var convertedValue = UnitUtils.ConvertFromInternalUnits(value, displayUnit);
                // Округляем до 3 знаков после запятой, если это число
                if (convertedValue % 1 != 0) // проверка, что не целое число
                {
                    return Math.Round(convertedValue, 3);
                }
                return convertedValue;
            case StorageType.ElementId:
                return param.AsElementId();
            default:
                return param.AsValueString();
        }
    }

    private static object ParseToType(object sampleValue, string input, Parameter param, Document doc)
    {
        if (sampleValue == null || input == null) return input;

        if (sampleValue is string) return input;

        // Заменяем запятую на точку
        input = input.Replace(',', '.');

        if (sampleValue is int || sampleValue is long)
        {
            if (long.TryParse(input, out long l)) return l;
        }

        if (sampleValue is double || sampleValue is float)
        {
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                // Если параметр — длина, то d — уже в тех единицах, в которых показывается пользователю
                // Т.е. если Revit показывает мм, а юзер ввёл 2710.111, то всё ок
                return d;
            }
        }

        if (sampleValue is ElementId)
        {
            return input;
        }

        return input; // fallback: строка
    }

    private static bool Compare(object actual, object expected, RuleOperator op, Document doc)
    {
        if (actual == null || expected == null) return false;

        // === Сравнение строк ===
        if (actual is string aStr && expected is string eStr)
        {
            switch (op)
            {
                case RuleOperator.Equals: return aStr.Equals(eStr, StringComparison.OrdinalIgnoreCase);
                case RuleOperator.NotEquals: return !aStr.Equals(eStr, StringComparison.OrdinalIgnoreCase);
                case RuleOperator.Contains: return aStr.IndexOf(eStr, StringComparison.OrdinalIgnoreCase) >= 0;
                case RuleOperator.StartsWith: return aStr.StartsWith(eStr, StringComparison.OrdinalIgnoreCase);
                default: return false;
            }
        }

        // === Сравнение чисел ===
        if (IsNumeric(actual) && IsNumeric(expected))
        {
            double a = Convert.ToDouble(actual);
            double e = Convert.ToDouble(expected);
            const double eps = 1e-9;

            switch (op)
            {
                case RuleOperator.Equals: return Math.Abs(a - e) < eps;
                case RuleOperator.NotEquals: return Math.Abs(a - e) >= eps;
                case RuleOperator.GreaterThan: return a > e;
                case RuleOperator.LessThan: return a < e;
                default: return false;
            }
        }

        // === Сравнение ElementId с именем элемента (строкой) ===
        if (actual is ElementId actualId && expected is string expectedStr)
        {
            if (actualId == ElementId.InvalidElementId) return false;

            Element referencedElement = doc.GetElement(actualId);
            string elementName = referencedElement?.Name ?? "";

            switch (op)
            {
                case RuleOperator.Equals: return elementName.Equals(expectedStr, StringComparison.OrdinalIgnoreCase);
                case RuleOperator.NotEquals: return !elementName.Equals(expectedStr, StringComparison.OrdinalIgnoreCase);
                case RuleOperator.Contains: return elementName.IndexOf(expectedStr, StringComparison.OrdinalIgnoreCase) >= 0;
                case RuleOperator.StartsWith: return elementName.StartsWith(expectedStr, StringComparison.OrdinalIgnoreCase);
                default: return false;
            }
        }

        // === Сравнение Integer как специальных значений (Yes/No) ===
        if (actual is int actualInt && expected is string expectedStr2)
        {
            if (expectedStr2.Equals("Да", StringComparison.OrdinalIgnoreCase) || expectedStr2.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                return actualInt == 1;
            }
            else if (expectedStr2.Equals("Нет", StringComparison.OrdinalIgnoreCase) || expectedStr2.Equals("No", StringComparison.OrdinalIgnoreCase))
            {
                return actualInt == 0;
            }
        }

        return false;
    }
    private static bool IsNumeric(object obj)
    {
        return obj is sbyte || obj is byte || obj is short || obj is ushort ||
               obj is int || obj is uint || obj is long || obj is ulong ||
               obj is float || obj is double || obj is decimal;
    }
}
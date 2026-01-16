using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RevitAdvancedSelectionTool.Models;
using Troyan;
using FilterRule = RevitAdvancedSelectionTool.Models.FilterRule;
using Category = Autodesk.Revit.DB.Category;

namespace RevitAdvancedSelectionTool.Core
{
    public static class RevitRuleFilter
    {
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
            ApplyFilterAndSelect(uidoc, SharedData.uslovia, SharedData.unions, SharedData.exitSelect);
        }

        public static void ApplyFilterAndSelect(UIDocument uidoc, string[,] uslovia, string[] unions, IEnumerable<string> selectedCategories)
        {
            if (uidoc == null) throw new ArgumentNullException(nameof(uidoc));

            int conditionCount = uslovia.GetLength(0);
            if (conditionCount == 0)
            {
                uidoc.Selection.SetElementIds(new List<ElementId>());
                return;
            }

            // Конвертация условий в правила фильтрации
            var rules = ConvertUsloviaToRules(uslovia, conditionCount);

            // Получение элементов
            Document doc = uidoc.Document;
            List<Element> allElements = GetElementsForCategories(doc, selectedCategories);

            // Применение фильтра
            var resultElements = ApplyRulesToElements(allElements, rules, doc, unions);

            // Установка выбора
            uidoc.Selection.SetElementIds(resultElements.ToList());
        }

        public static void ApplyFilterAndSelect(string[,] uslovia, string[] unions)
        {
            ApplyFilterAndSelect(SharedData.uidoc, uslovia, unions, SharedData.exitSelect);
        }

        private static List<FilterRule> ConvertUsloviaToRules(string[,] uslovia, int conditionCount)
        {
            var rules = new List<FilterRule>();

            for (int i = 0; i < conditionCount; i++)
            {
                var rule = new FilterRule
                {
                    ParameterName = uslovia[i, 0]?.Trim() ?? "",
                    Value = uslovia[i, 2] ?? ""
                };

                string opStr = uslovia[i, 1]?.Trim() ?? "Равно";
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

        private static List<Element> GetElementsForCategories(Document doc, System.Collections.Generic.IEnumerable<string> selectedCategories)
        {
            var allElements = new List<Element>();

            if (selectedCategories != null && selectedCategories.Any())
            {
                foreach (string categoryName in selectedCategories)
                {
                    if (string.IsNullOrWhiteSpace(categoryName)) continue;

                    var bic = FindBuiltInCategoryByName(doc, categoryName);
                    if (bic.HasValue)
                    {
                        var elementsInCategory = new FilteredElementCollector(doc)
                            .OfCategory(bic.Value)
                            .WhereElementIsNotElementType()
                            .ToList();

                        allElements.AddRange(elementsInCategory);
                    }
                }
            }
            else
            {
                // Все элементы, если категории не выбраны
                allElements = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .Where(e => e?.Category != null)
                    .ToList();
            }

            return allElements;
        }

        private static HashSet<ElementId> ApplyRulesToElements(List<Element> elements, List<FilterRule> rules, Document doc, string[] unions)
        {
            // Группировка правил по ИЛИ
            var ruleGroups = GroupRulesByOr(rules, unions);

            var resultElements = new HashSet<ElementId>();

            foreach (var group in ruleGroups)
            {
                var matchedInGroup = elements.Where(el =>
                {
                    foreach (var rule in group)
                    {
                        if (!MatchesRule(el, rule, doc))
                            return false;
                    }
                    return true;
                });

                foreach (var el in matchedInGroup)
                {
                    resultElements.Add(el.Id);
                }
            }

            return resultElements;
        }

        private static List<List<FilterRule>> GroupRulesByOr(List<FilterRule> rules, string[] unions)
        {
            var groups = new List<List<FilterRule>>();
            var currentGroup = new List<FilterRule>();
            groups.Add(currentGroup);

            for (int i = 0; i < rules.Count; i++)
            {
                currentGroup.Add(rules[i]);

                // Если есть ИЛИ на следующей позиции, начать новую группу
                if (i < unions?.Length && string.Equals(unions[i]?.Trim(), "ИЛИ", StringComparison.OrdinalIgnoreCase))
                {
                    currentGroup = new List<FilterRule>();
                    groups.Add(currentGroup);
                }
            }

            return groups;
        }

        private static bool MatchesRule(Element element, FilterRule rule, Document doc)
        {
            Parameter param = element.LookupParameter(rule.ParameterName);
            if (param == null) return false;

            object actual = GetParameterValue(param, doc);
            object expected = ParseToType(actual, rule.Value, param, doc);

            if (expected == null) expected = rule.Value;

            return Compare(actual, expected, rule.Operator, doc);
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
                    var convertedValue = UnitUtils.ConvertFromInternalUnits(value, displayUnit);
                    if (convertedValue % 1 != 0)
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

            input = input.Replace(',', '.');

            if (sampleValue is int || sampleValue is long)
            {
                if (long.TryParse(input, out long l)) return l;
            }

            if (sampleValue is double || sampleValue is float)
            {
                if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                {
                    return d;
                }
            }

            if (sampleValue is ElementId)
            {
                return input;
            }

            return input;
        }

        private static bool Compare(object actual, object expected, RuleOperator op, Document doc)
        {
            if (actual == null || expected == null) return false;

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

            if (actual is int actualInt && expected is string expectedStr2)
            {
                if (expectedStr2.Equals("да", StringComparison.OrdinalIgnoreCase) || expectedStr2.Equals("Yes", StringComparison.OrdinalIgnoreCase))
                {
                    return actualInt == 1;
                }
                else if (expectedStr2.Equals("нет", StringComparison.OrdinalIgnoreCase) || expectedStr2.Equals("No", StringComparison.OrdinalIgnoreCase))
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

        public static HashSet<ElementId> GetFilteredElementIds(UIDocument uidoc, out List<Element> allElementsInCategories)
        {
            allElementsInCategories = new List<Element>();

            if (uidoc == null) throw new ArgumentNullException(nameof(uidoc));

            int conditionCount = SharedData.uslovia.GetLength(0);
            if (conditionCount == 0)
            {
                return new HashSet<ElementId>();
            }

            // Конвертация условий
            var rules = ConvertUsloviaToRules(SharedData.uslovia, conditionCount);

            // Получение элементов
            Document doc = uidoc.Document;
            allElementsInCategories = GetElementsForCategories(doc, SharedData.exitSelect);

            // Применение фильтра
            return ApplyRulesToElements(allElementsInCategories, rules, doc, SharedData.unions);
        }
    }

    public static class RevitNot
    {
        public static void GOG(UIDocument uidoc)
        {
            if (uidoc == null) return;

            // Получить отфильтрованные элементы
            var passedIds = RevitRuleFilter.GetFilteredElementIds(uidoc, out List<Element> allElementsInCategories);

            var notPassedIds = allElementsInCategories
                .Where(e => !passedIds.Contains(e.Id))
                .Select(e => e.Id)
                .ToList();

            uidoc.Selection.SetElementIds(notPassedIds);
        }
    }
}
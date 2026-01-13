using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Troyan;

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
        for (int i = 0; i < SharedData.uslovia.GetLength(0); i++)
        {
            for (int r = 0; r < SharedData.uslovia.GetLength(1); r++)
            {
                switch (SharedData.uslovia[i, r])
                {
                    case "�����":
                        SharedData.uslovia[i, r] = "Equals";
                        break;
                    case "�� �����":
                        SharedData.uslovia[i, r] = "NotEquals";
                        break;
                    case "��������":
                        SharedData.uslovia[i, r] = "Contains";
                        break;
                    case "���������� �":
                        SharedData.uslovia[i, r] = "StartsWith";
                        break;
                    case "������":
                        SharedData.uslovia[i, r] = "GreaterThan";
                        break;
                    case "������":
                        SharedData.uslovia[i, r] = "LessThan";
                        break;
                    default:
                        break;
                }
            }
        }
        if (uidoc == null) throw new ArgumentNullException(nameof(uidoc));

        int conditionCount = SharedData.uslovia.GetLength(0);
        if (conditionCount == 0)
        {
            uidoc.Selection.SetElementIds(new List<ElementId>());
            return;
        }

        // ��������� ������ �� uslovia
        string[] paramNames = new string[conditionCount];
        RuleOperator[] operators = new RuleOperator[conditionCount];
        string[] values = new string[conditionCount];

        for (int i = 0; i < conditionCount; i++)
        {
            paramNames[i] = SharedData.uslovia[i, 0]?.Trim() ?? "";
            string opStr = SharedData.uslovia[i, 1]?.Trim() ?? "Equals";
            values[i] = SharedData.uslovia[i, 2] ?? "";

            if (!Enum.TryParse(opStr, true, out RuleOperator op))
            {
                op = RuleOperator.Equals;
            }
            operators[i] = op;
        }

        // ��������� ������� �� ������ �� ������ "���"
        var groups = new List<List<(string paramName, RuleOperator op, string value)>>();

        var currentGroup = new List<(string, RuleOperator, string)>();
        groups.Add(currentGroup);

        int maxUnions = Math.Min(SharedData.unions?.Length ?? 0, conditionCount - 1);
        for (int i = 0; i < conditionCount; i++)
        {
            currentGroup.Add((paramNames[i], operators[i], values[i]));

            if (i < maxUnions && string.Equals(SharedData.unions[i]?.Trim(), "���", StringComparison.OrdinalIgnoreCase))
            {
                currentGroup = new List<(string, RuleOperator, string)>();
                groups.Add(currentGroup);
            }
        }

        // �������� ��� ��������
        Document doc = uidoc.Document;
        List<Element> allElements = new List<Element>();

        var selectedCategories = SharedData.exitSelect;

        // ���� ������ ��������� ����� � �� ����
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
                // ���������� ���������, ������� �� ������� (��� ������� ��������������)
            }

            if (!foundAtLeastOne)
            {
                TaskDialog.Show("������", "�� ���� �� ��������� ��������� �� ������� � �������.");
                uidoc.Selection.SetElementIds(new List<ElementId>());
                return;
            }
        }
        else
        {
            // ���� ��������� �� ������� � ���� ��� (��� ������)
            allElements = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .Where(e => e?.Category != null)
                .ToList();
        }

        // ��������� �� �������: (������1) OR (������2) ...
        var resultElements = new HashSet<ElementId>();

        foreach (var group in groups)
        {
            var matchedInGroup = allElements.Where(el =>
            {
                foreach (var (paramName, op, value) in group)
                {
                    if (!MatchesRule(el, paramName, op, value, doc))
                        return false; // �� ������ ���� �� ���� ������� � ������
                }
                return true; // ������ ��� ������� � ������
            });

            foreach (var el in matchedInGroup)
            {
                resultElements.Add(el.Id);
            }
        }

        // �������� ���������
        var filteredIds = GetFilteredElementIds(uidoc, out _);
        uidoc.Selection.SetElementIds(resultElements.ToList());
    }
    public static HashSet<ElementId> GetFilteredElementIds(UIDocument uidoc, out List<Element> allElementsInCategories)
    {
        allElementsInCategories = new List<Element>();

        if (uidoc == null) throw new ArgumentNullException(nameof(uidoc));

        // ����������� ��������� (��� ������)
        for (int i = 0; i < SharedData.uslovia.GetLength(0); i++)
        {
            switch (SharedData.uslovia[i, 1]) // ������ �������� (������� 1)
            {
                case "�����": SharedData.uslovia[i, 1] = "Equals"; break;
                case "�� �����": SharedData.uslovia[i, 1] = "NotEquals"; break;
                case "��������": SharedData.uslovia[i, 1] = "Contains"; break;
                case "���������� �": SharedData.uslovia[i, 1] = "StartsWith"; break;
                case "������": SharedData.uslovia[i, 1] = "GreaterThan"; break;
                case "������": SharedData.uslovia[i, 1] = "LessThan"; break;
            }
        }

        int conditionCount = SharedData.uslovia.GetLength(0);
        if (conditionCount == 0)
        {
            return new HashSet<ElementId>(); // ������ ���������
        }

        // ��������� �������
        string[] paramNames = new string[conditionCount];
        RuleOperator[] operators = new RuleOperator[conditionCount];
        string[] values = new string[conditionCount];

        for (int i = 0; i < conditionCount; i++)
        {
            paramNames[i] = SharedData.uslovia[i, 0]?.Trim() ?? "";
            string opStr = SharedData.uslovia[i, 1]?.Trim() ?? "Equals";
            values[i] = SharedData.uslovia[i, 2] ?? "";

            operators[i] = Enum.TryParse(opStr, true, out RuleOperator op) ? op : RuleOperator.Equals;
        }

        // ����������� �� "���"
        var groups = new List<List<(string, RuleOperator, string)>>();
        var currentGroup = new List<(string, RuleOperator, string)>();
        groups.Add(currentGroup);

        int maxUnions = Math.Min(SharedData.unions?.Length ?? 0, conditionCount - 1);
        for (int i = 0; i < conditionCount; i++)
        {
            currentGroup.Add((paramNames[i], operators[i], values[i]));

            if (i < maxUnions && string.Equals(SharedData.unions[i]?.Trim(), "���", StringComparison.OrdinalIgnoreCase))
            {
                currentGroup = new List<(string, RuleOperator, string)>();
                groups.Add(currentGroup);
            }
        }

        // ���� ��������� �� ��������� ���������
        Document doc = uidoc.Document;
        var selectedCategories = SharedData.exitSelect;

        if (selectedCategories != null && selectedCategories.Count > 0)
        {
            foreach (string categoryName in selectedCategories)
            {
                if (string.IsNullOrWhiteSpace(categoryName)) continue;
                if (FindBuiltInCategoryByName(doc, categoryName) is BuiltInCategory bic)
                {
                    var elements = new FilteredElementCollector(doc)
                        .OfCategory(bic)
                        .WhereElementIsNotElementType()
                        .ToList();
                    allElementsInCategories.AddRange(elements);
                }
            }
        }
        else
        {
            allElementsInCategories = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .Where(e => e?.Category != null)
                .ToList();
        }

        // ����������
        var resultIds = new HashSet<ElementId>();
        foreach (var group in groups)
        {
            var matched = allElementsInCategories.Where(el =>
            {
                foreach (var (paramName, op, value) in group)
                {
                    if (!MatchesRule(el, paramName, op, value, doc))
                        return false;
                }
                return true;
            });

            foreach (var el in matched)
                resultIds.Add(el.Id);
        }

        return resultIds;
    }
    private static bool MatchesRule(Element element, string paramName, RuleOperator op, string userValue, Document doc)
    {
        Parameter param = element.LookupParameter(paramName);
        if (param == null) return false;

        object actual = GetParameterValue(param, doc); // ��� ����������
        object expected = ParseToType(actual, userValue, param, doc);

        if (expected == null) expected = userValue;

        // ���������� ���������� ������ ����� ������� � expected, ���� ��� �����
        int decimalPlaces = CountDecimalPlaces(expected);



        return Compare(actual, expected, op, doc);
    }
    private static int CountDecimalPlaces(object value)
    {
        if (value is string str)
        {
            // �������� ������� �� �����, ����� ������� ��� �����
            str = str.Replace(',', '.');
            if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                int index = str.IndexOf('.');
                if (index >= 0)
                {
                    return str.Length - index - 1;
                }
            }
            return -1; // �� ����� ��� ��� ������� �����
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

        return -1; // �� �����
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
                // ����������� �������� �� ���������� ������ (����) � ������������
                var convertedValue = UnitUtils.ConvertFromInternalUnits(value, displayUnit);
                // ��������� �� 3 ������ ����� �������, ���� ��� �����
                if (convertedValue % 1 != 0) // ��������, ��� �� ����� �����
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

        // �������� ������� �� �����
        input = input.Replace(',', '.');

        if (sampleValue is int || sampleValue is long)
        {
            if (long.TryParse(input, out long l)) return l;
        }

        if (sampleValue is double || sampleValue is float)
        {
            if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
            {
                // ���� �������� � �����, �� d � ��� � ��� ��������, � ������� ������������ ������������
                // �.�. ���� Revit ���������� ��, � ���� ��� 2710.111, �� �� ��
                return d;
            }
        }

        if (sampleValue is ElementId)
        {
            return input;
        }

        return input; // fallback: ������
    }

    private static bool Compare(object actual, object expected, RuleOperator op, Document doc)
    {
        if (actual == null || expected == null) return false;

        // === ��������� ����� ===
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

        // === ��������� ����� ===
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

        // === ��������� ElementId � ������ �������� (�������) ===
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

        // === ��������� Integer ��� ����������� �������� (Yes/No) ===
        if (actual is int actualInt && expected is string expectedStr2)
        {
            if (expectedStr2.Equals("��", StringComparison.OrdinalIgnoreCase) || expectedStr2.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                return actualInt == 1;
            }
            else if (expectedStr2.Equals("���", StringComparison.OrdinalIgnoreCase) || expectedStr2.Equals("No", StringComparison.OrdinalIgnoreCase))
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
public static class RevitNot
{
    public static void GOG(UIDocument uidoc)
    {
        if (uidoc == null) return;

        // �������� ��� �������� � ��������� ���������� + ��, ��� ������ ������
        var passedIds = RevitRuleFilter.GetFilteredElementIds(uidoc, out List<Element> allElementsInCategories);

        var notPassedIds = allElementsInCategories
            .Where(e => !passedIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToList();

        uidoc.Selection.SetElementIds(notPassedIds);
    }
}

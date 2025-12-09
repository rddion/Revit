using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Wpf;

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

    public static void ApplyFilterAndSelect(
        UIDocument uidoc,
        string[] parameterNames,
        RuleOperator[] operators,
        string[] values,
        bool useAnd)
    {
        if (uidoc == null) throw new ArgumentNullException(nameof(uidoc));
        if (parameterNames == null || operators == null || values == null)
            throw new ArgumentNullException();

        if (!(parameterNames.Length == operators.Length && operators.Length == values.Length))
            throw new ArgumentException("Длины массивов не совпадают.");

        if (parameterNames.Length == 0)
        {
            uidoc.Selection.SetElementIds(new List<ElementId>());
            return;
        }

        Document doc = uidoc.Document;
        var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
        var allElements = new List<Element>();

        foreach (Element e in collector)
        {
            if (e?.Category != null)
            {
                allElements.Add(e);
            }
        }

        List<Element> matchingElements;

        if (useAnd)
        {
            matchingElements = allElements
                .Where(e => RuleMatchesAll(e, parameterNames, operators, values))
                .ToList();
        }
        else
        {
            matchingElements = allElements
                .Where(e => RuleMatchesAny(e, parameterNames, operators, values))
                .ToList();
        }

        uidoc.Selection.SetElementIds(matchingElements.Select(e => e.Id).ToList());
    }

    // === ВСЕ ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДОЛЖНЫ БЫТЬ ЗДЕСЬ ===

    private static bool RuleMatchesAll(
        Element element,
        string[] paramNames,
        RuleOperator[] ops,
        string[] vals)
    {
        for (int i = 0; i < paramNames.Length; i++)
        {
            if (!MatchesRule(element, paramNames[i], ops[i], vals[i]))
                return false;
        }
        return true;
    }

    private static bool RuleMatchesAny(
        Element element,
        string[] paramNames,
        RuleOperator[] ops,
        string[] vals)
    {
        for (int i = 0; i < paramNames.Length; i++)
        {
            if (MatchesRule(element, paramNames[i], ops[i], vals[i]))
                return true;
        }
        return false;
    }

    private static bool MatchesRule(Element element, string paramName, RuleOperator op, string userValue)
    {
        Parameter param = element.LookupParameter(paramName);
        if (param == null) return false;

        object actual = GetParameterValue(param);
        object expected = ParseToType(actual, userValue);

        if (expected == null) expected = userValue;

        return Compare(actual, expected, op);
    }

    private static object GetParameterValue(Parameter param)
    {
        switch (param.StorageType)
        {
            case StorageType.String: return param.AsString();
            case StorageType.Integer: return param.AsInteger();
            case StorageType.Double: return param.AsDouble();
            case StorageType.ElementId: return param.AsElementId();
            default: return param.AsValueString();
        }
    }

    private static object ParseToType(object sampleValue, string input)
    {
        if (sampleValue is string) return input;

        if (sampleValue is int || sampleValue is long)
        {
            if (int.TryParse(input, out int i)) return i;
        }

        if (sampleValue is double || sampleValue is float)
        {
            if (double.TryParse(input, out double d)) return d;
        }

        return null;
    }

    private static bool Compare(object actual, object expected, RuleOperator op)
    {
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

        return false;
    }

    // 🔸 Вот он — метод IsNumeric! Он ОБЯЗАН быть в классе
    private static bool IsNumeric(object obj)
    {
        return obj is sbyte || obj is byte || obj is short || obj is ushort ||
               obj is int || obj is uint || obj is long || obj is ulong ||
               obj is float || obj is double || obj is decimal;
    }
}
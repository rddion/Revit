using System;

namespace RevitAdvancedSelectionTool.Models
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

    public enum LogicalOperator
    {
        And,
        Or
    }

    public class FilterRule
    {
        public string ParameterName { get; set; }
        public RuleOperator Operator { get; set; }
        public string Value { get; set; }
        public LogicalOperator LogicalOperator { get; set; }

        public string DisplayText => $"{ParameterName} {GetOperatorDisplay()} {Value}";

        private string GetOperatorDisplay()
        {
            switch (Operator)
            {
                case RuleOperator.Equals: return "=";
                case RuleOperator.NotEquals: return "≠";
                case RuleOperator.Contains: return "содержит";
                case RuleOperator.StartsWith: return "начинается с";
                case RuleOperator.GreaterThan: return ">";
                case RuleOperator.LessThan: return "<";
                default: return Operator.ToString();
            }
        }

        public static RuleOperator ParseRussianOperator(string russianOp)
        {
            switch (russianOp)
            {
                case "Равно":
                    return RuleOperator.Equals;
                case "Не равно":
                    return RuleOperator.NotEquals;
                case "Содержит":
                    return RuleOperator.Contains;
                case "Начинается с":
                    return RuleOperator.StartsWith;
                case "Больше":
                    return RuleOperator.GreaterThan;
                case "Меньше":
                    return RuleOperator.LessThan;
                default:
                    throw new ArgumentException($"Unknown operator: {russianOp}");
            }
        }
    }
}
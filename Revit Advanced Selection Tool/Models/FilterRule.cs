using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

    public class FilterRule : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private string _parameterName;
        public string ParameterName
        {
            get => _parameterName;
            set => SetProperty(ref _parameterName, value);
        }

        private RuleOperator _operator;
        public RuleOperator Operator
        {
            get => _operator;
            set => SetProperty(ref _operator, value);
        }

        private string _value;
        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        private LogicalOperator _logicalOperator;
        public LogicalOperator LogicalOperator
        {
            get => _logicalOperator;
            set => SetProperty(ref _logicalOperator, value);
        }

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
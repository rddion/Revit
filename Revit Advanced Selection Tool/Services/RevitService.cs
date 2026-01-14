using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RevitAdvancedSelectionTool.Models;
using Troyan;

namespace RevitAdvancedSelectionTool.Services
{
    public class RevitService : IRevitService
    {
        public event EventHandler<string> StatusChanged;

        public async Task<List<Models.Category>> GetCategoriesAsync()
        {
            try
            {
                // В Revit нужно получить UIApplication через статический доступ или передать
                // Пока оставим заглушку, в реальной реализации нужно передать UIApplication
                var categories = new List<Models.Category>();
                StatusChanged?.Invoke(this, "Категории загружены");
                return categories;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Ошибка получения категорий: {ex.Message}");
                return new List<Models.Category>();
            }
        }

        public async Task<List<Models.ParameterInfo>> GetCommonParametersAsync(List<string> categoryNames)
        {
            try
            {
                // Обновить SharedData.exitSelect перед вызовом
                SharedData.exitSelect = categoryNames;

                // Получить документ - в реальной реализации нужно передать UIApplication
                // var doc = uiApp.ActiveUIDocument.Document;
                // var commonParams = ParameterIntersectionHelper.GetCommonParameters(doc);

                // Пока возвращаем пустой список
                var parameters = new List<Models.ParameterInfo>();
                StatusChanged?.Invoke(this, "Параметры загружены");
                return parameters;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Ошибка получения параметров: {ex.Message}");
                return new List<Models.ParameterInfo>();
            }
        }

        public async Task<Models.SearchResult> SearchElementsAsync(List<string> categories, List<Models.FilterRule> rules)
        {
            try
            {
                // Подготовить данные для SharedData
                PrepareSharedDataForSearch(categories, rules);

                // Вызвать ExternalEvent
                SharedData.ApplyFilterEvent?.Raise();

                // В реальной реализации нужно ждать завершения операции
                var result = new Models.SearchResult
                {
                    FoundElements = new List<Models.RevitElement>(),
                    StatusMessage = "Поиск завершен"
                };

                StatusChanged?.Invoke(this, $"Найдено элементов: {result.TotalCount}");
                return result;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Ошибка поиска: {ex.Message}");
                return new Models.SearchResult { StatusMessage = $"Ошибка: {ex.Message}" };
            }
        }

        public async Task InvertSelectionAsync(List<ElementId> elementIds)
        {
            try
            {
                SharedData.InvertEvent?.Raise();
                StatusChanged?.Invoke(this, "Выборка инвертирована");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, $"Ошибка инвертирования: {ex.Message}");
            }
        }

        private void PrepareSharedDataForSearch(List<string> categories, List<Models.FilterRule> rules)
        {
            // Заполнить SharedData.exitSelect
            SharedData.exitSelect = categories;

            // Конвертировать rules в SharedData.uslovia и unions
            SharedData.uslovia = new string[rules.Count, 3];
            SharedData.unions = new string[rules.Count];

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                SharedData.uslovia[i, 0] = rule.ParameterName;
                SharedData.uslovia[i, 1] = GetRussianOperator(rule.Operator);
                SharedData.uslovia[i, 2] = rule.Value;

                // Для unions - если есть следующий rule с ИЛИ
                if (i < rules.Count - 1 && rules[i + 1].LogicalOperator == Models.LogicalOperator.Or)
                {
                    SharedData.unions[i] = "ИЛИ";
                }
            }
        }

        private string GetRussianOperator(Models.RuleOperator op)
        {
            switch (op)
            {
                case Models.RuleOperator.Equals: return "Равно";
                case Models.RuleOperator.NotEquals: return "Не равно";
                case Models.RuleOperator.Contains: return "Содержит";
                case Models.RuleOperator.StartsWith: return "Начинается с";
                case Models.RuleOperator.GreaterThan: return "Больше";
                case Models.RuleOperator.LessThan: return "Меньше";
                default: return "Равно";
            }
        }
    }
}
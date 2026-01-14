using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RevitAdvancedSelectionTool.Models;
using CategoryModel = RevitAdvancedSelectionTool.Models.Category;
using FilterRuleModel = RevitAdvancedSelectionTool.Models.FilterRule;

namespace RevitAdvancedSelectionTool.Services
{
    public class RevitService : IRevitService
    {
        public event EventHandler<string> StatusChanged;

        public async Task<List<CategoryModel>> GetCategoriesAsync()
        {
            // Реализация на основе Troyanka.GetCategories
            var categories = new List<CategoryModel>();

            // Получить активный документ через UIApplication
            // Это нужно будет передать через конструктор или статический доступ
            // Пока оставим заглушку
            return categories;
        }

        public async Task<List<ParameterInfo>> GetCommonParametersAsync(List<string> categoryNames)
        {
            // Реализация на основе ParameterIntersectionHelper.GetCommonParameters
            return new List<ParameterInfo>();
        }

        public async Task<SearchResult> SearchElementsAsync(List<string> categories, List<FilterRuleModel> rules)
        {
            // Реализация на основе RevitRuleFilter.GetFilteredElementIds
            // Но поскольку это сервис, и нет UIDocument, нужно получить через статический доступ или передать.
            // Для простоты, использовать SharedData и RevitRuleFilter
            // Предполагаем, что SharedData.uslovia уже заполнен из rules
            // Но rules переданы, так что нужно конвертировать rules в SharedData.uslovia

            // Временная заглушка
            return new SearchResult();
        }

        public async Task InvertSelectionAsync(List<ElementId> elementIds)
        {
            // Реализация на основе RevitNot.GOG
        }
    }
}
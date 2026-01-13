using System.Collections.Generic;
using System.Threading.Tasks;
using RevitAdvancedSelectionTool.Models;

namespace RevitAdvancedSelectionTool.Services
{
    public class FilterService : IFilterService
    {
        public async Task<List<ParameterInfo>> GetAvailableParametersAsync(List<string> categoryNames)
        {
            // Реализация на основе ParameterIntersectionHelper.GetCommonParameters
            return new List<ParameterInfo>();
        }

        public async Task<bool> ValidateRuleAsync(FilterRule rule)
        {
            // Валидация правила
            return !string.IsNullOrWhiteSpace(rule.ParameterName) && !string.IsNullOrWhiteSpace(rule.Value);
        }

        public async Task<SearchResult> ApplyFilterAsync(List<string> categories, List<FilterRule> rules)
        {
            // Применение фильтра
            return new SearchResult();
        }
    }
}
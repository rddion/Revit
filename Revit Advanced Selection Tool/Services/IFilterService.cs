using System.Collections.Generic;
using System.Threading.Tasks;
using RevitAdvancedSelectionTool.Models;

namespace RevitAdvancedSelectionTool.Services
{
    public interface IFilterService
    {
        Task<List<ParameterInfo>> GetAvailableParametersAsync(List<string> categoryNames);
        Task<bool> ValidateRuleAsync(FilterRule rule);
        Task<SearchResult> ApplyFilterAsync(List<string> categories, List<FilterRule> rules);
    }
}
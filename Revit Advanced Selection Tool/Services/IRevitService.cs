using System.Collections.Generic;
using System.Threading.Tasks;
using RevitAdvancedSelectionTool.Models;
using CategoryModel = RevitAdvancedSelectionTool.Models.Category;
using FilterRuleModel = RevitAdvancedSelectionTool.Models.FilterRule;

namespace RevitAdvancedSelectionTool.Services
{
    public interface IRevitService
    {
        Task<List<CategoryModel>> GetCategoriesAsync();
        Task<List<ParameterInfo>> GetCommonParametersAsync(List<string> categoryNames);
        Task<SearchResult> SearchElementsAsync(List<string> categories, List<FilterRuleModel> rules);
        Task InvertSelectionAsync(List<Autodesk.Revit.DB.ElementId> elementIds);
        event System.EventHandler<string> StatusChanged;
    }
}
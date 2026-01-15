using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using RevitAdvancedSelectionTool.Models;

namespace RevitAdvancedSelectionTool.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> LoadCategoriesAsync();
        Task<List<Category>> FilterCategoriesAsync(string searchText);
        Task SaveCategorySelectionAsync(List<Category> selectedCategories);
        Task<ObservableCollection<Category>> GetCategoriesObservableAsync();
        Task<ObservableCollection<string>> GetCategoryNamesObservableAsync();
    }
}
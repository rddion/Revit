using System.Collections.Generic;

namespace RevitAdvancedSelectionTool.Models
{
    public class SearchResult
    {
        public List<RevitElement> FoundElements { get; set; } = new List<RevitElement>();
        public int TotalCount => FoundElements.Count;
        public string StatusMessage { get; set; }
    }
}
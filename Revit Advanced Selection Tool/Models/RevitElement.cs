using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RevitAdvancedSelectionTool.Models
{
    public class RevitElement
    {
        public ElementId Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}
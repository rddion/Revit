using Autodesk.Revit.DB;
using System;

namespace RevitAdvancedSelectionTool.Models
{
    public class ParameterInfo : IEquatable<ParameterInfo>
    {
        public string Name { get; set; }
        public StorageType StorageType { get; set; }

        public string DisplayName => $"{Name} ({StorageType})";

        public bool Equals(ParameterInfo other)
        {
            if (other == null) return false;
            return Name == other.Name && StorageType == other.StorageType;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ParameterInfo);
        }

        public override int GetHashCode()
        {
            return (Name?.GetHashCode() ?? 0) ^ (int)StorageType;
        }
    }
}
using System.Runtime.Serialization;

namespace RevitAdvancedSelectionTool.Models
{
    [DataContract]
    public class Category
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Type { get; set; }

        public override string ToString() => Name;
    }
}
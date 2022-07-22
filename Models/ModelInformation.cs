using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace MiitsuColorController.Models
{
    public class ModelInformation : ObservableObject
    {
        public string ID { get; set; }
        public string ModelName { get; set; }
        public string[] ArtMeshNames { get; set; }
        public string[] ArtMeshTags { get; set; }
    }
}

using System.Collections.Generic;

namespace MiitsuColorController.Models
{
    public class ArtmeshColoringSetting
    {
        public bool Activated = false;
        public int Duration = 1000;
        public int Interpolation = 0;
        public int MaximumS = 100;
        public int MaximumV = 100;
        public int MessageCount = 1;
        public int MessageHandlingMethod = 0;
        public int MinimumS = 0;
        public int MinimumV = 0;
        public List<string> SelectedArtMesh = new();
        public List<string> SelectedTag = new();
        public string BlueEmote = "";
        public string GreenEmote = "";
        public string RedEmote = "";
    }
}

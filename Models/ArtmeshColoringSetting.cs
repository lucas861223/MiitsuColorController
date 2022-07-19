using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiitsuColorController.Models
{
    public class ArtmeshColoringSetting
    {
        public bool Activated = false;
        public int Duration = 0;
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

using MiitsuColorController.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace MiitsuColorController.Helper
{
    public class ResourceManager
    {
        public Dictionary<ResourceKey, bool> BoolResourceDictionary = new();
        public ModelInformation CurrentModelInformation = new();
        public Dictionary<ResourceKey, int> IntResourceDictionary = new();
        public Dictionary<ResourceKey, string> StringResourceDictionary = new();
        private static string _ICON_PATH = "Assets/3.0.PNG";
        private static ResourceManager _instance = null;
        private static string _SAVE_FILE_LOCATION = "SaveData";
        private ResourceManager()
        {
            if (File.Exists("SaveData"))
            {
                string[] lines = File.ReadAllLines(_SAVE_FILE_LOCATION);
                string[] tokens;
                ResourceKey enumValue;
                int section = 0;
                foreach (string line in lines)
                {
                    tokens = line.Split("\t");
                    if (tokens.Length == 2)
                    {
                        if (Enum.TryParse(tokens[0], out enumValue))
                        {
                            switch (section)
                            {
                                case 0:
                                    StringResourceDictionary.Add(enumValue, tokens[1]);
                                    break;

                                case 1:
                                    IntResourceDictionary.Add(enumValue, int.Parse(tokens[1]));
                                    break;

                                case 2:
                                    BoolResourceDictionary.Add(enumValue, bool.Parse(tokens[1]));
                                    break;
                            }
                        }
                    }
                    else
                    {
                        section += 1;
                    }
                }
            }
            InitializeMissingResource();
        }

        public static ResourceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ResourceManager();
                }
                return _instance;
            }
        }

        public static string GetAppIcon()
        {
            return Convert.ToBase64String(File.ReadAllBytes(_ICON_PATH));
        }

        public ArtmeshColoringSetting LoadModelSetting()
        {
            ArtmeshColoringSetting setting = new();
            if (File.Exists(CurrentModelInformation.ID + ".Miitsu"))
            {
                string[] lines = File.ReadAllLines(CurrentModelInformation.ID + ".Miitsu");
                string[] tokens;
                ResourceKey enumValue;
                foreach (string line in lines)
                {
                    tokens = line.Split("\t");
                    if (tokens.Length > 0)
                    {
                        if (Enum.TryParse(tokens[0], out enumValue))
                        {
                            switch (enumValue)
                            {
                                case ResourceKey.Activated:
                                    setting.Activated = bool.Parse(tokens[1]);
                                    break;

                                case ResourceKey.SelectedArtmesh:
                                    foreach (string name in tokens[1].Split(","))
                                    {
                                        setting.SelectedArtMesh.Add(name);
                                    }
                                    break;

                                case ResourceKey.SelectedTags:
                                    foreach (string tag in tokens[1].Split(","))
                                    {
                                        setting.SelectedTag.Add(tag);
                                    }
                                    break;

                                case ResourceKey.MessageCount:
                                    setting.MessageCount = int.Parse(tokens[1]);
                                    break;

                                case ResourceKey.ColoringMethod:
                                    setting.MessageHandlingMethod = int.Parse(tokens[1]);
                                    break;

                                case ResourceKey.RedEmote:
                                    setting.RedEmote = tokens[1];
                                    break;

                                case ResourceKey.GreenEmote:
                                    setting.GreenEmote = tokens[1];
                                    break;

                                case ResourceKey.BlueEmote:
                                    setting.BlueEmote = tokens[1];
                                    break;

                                case ResourceKey.Interpolation:
                                    setting.Interpolation = int.Parse(tokens[1]);
                                    break;

                                case ResourceKey.Duration:
                                    setting.Duration = int.Parse(tokens[1]);
                                    break;

                                case ResourceKey.VValue:
                                    setting.MinimumV = int.Parse(tokens[1]);
                                    setting.MaximumV = int.Parse(tokens[2]);
                                    break;

                                case ResourceKey.SValue:
                                    setting.MinimumS = int.Parse(tokens[1]);
                                    setting.MaximumS = int.Parse(tokens[2]);
                                    break;
                            }
                        }
                    }
                }
            }
            return setting;
        }

        public void SaveModelSetting(ArtmeshColoringSetting setting)
        {
            string content = "";
            content += Enum.GetName(ResourceKey.Activated) + "\t" + setting.Activated + "\n";
            if (setting.SelectedArtMesh.Count > 0)
            {
                string tmp = "";
                foreach (string name in setting.SelectedArtMesh)
                {
                    tmp += name + ",";
                }
                content += Enum.GetName(ResourceKey.SelectedArtmesh) + "\t" + tmp.Substring(0, tmp.Length - 1) + "\n";
            }
            if (setting.SelectedTag.Count > 0)
            {
                string tagTmp = "";
                foreach (string tag in setting.SelectedTag)
                {
                    tagTmp += tag + ",";
                }
                content += Enum.GetName(ResourceKey.SelectedTags) + "\t" + tagTmp.Substring(0, tagTmp.Length - 1) + "\n";
            }
            content += Enum.GetName(ResourceKey.MessageCount) + "\t" + setting.MessageCount + "\n";
            content += Enum.GetName(ResourceKey.ColoringMethod) + "\t" + setting.MessageHandlingMethod + "\n";
            if (setting.RedEmote.Length > 0)
            {
                content += Enum.GetName(ResourceKey.RedEmote) + "\t" + setting.RedEmote + "\n";
            }
            if (setting.GreenEmote.Length > 0)
            {
                content += Enum.GetName(ResourceKey.GreenEmote) + "\t" + setting.GreenEmote + "\n";
            }
            if (setting.BlueEmote.Length > 0)
            {
                content += Enum.GetName(ResourceKey.BlueEmote) + "\t" + setting.BlueEmote + "\n";
            }
            content += Enum.GetName(ResourceKey.Interpolation) + "\t" + setting.Interpolation + "\n";
            content += Enum.GetName(ResourceKey.Duration) + "\t" + setting.Duration + "\n";
            content += Enum.GetName(ResourceKey.VValue) + "\t" + setting.MinimumV + "\t" + setting.MaximumV + "\n";
            content += Enum.GetName(ResourceKey.SValue) + "\t" + setting.MinimumS + "\t" + setting.MaximumS + "\n";
            File.WriteAllText(CurrentModelInformation.ID + ".Miitsu", content);
        }

        public void SaveToPersistantStorage()
        {
            string content = "";
            foreach (ResourceKey key in StringResourceDictionary.Keys)
            {
                content += Enum.GetName(key) + "\t" + StringResourceDictionary[key] + "\n";
            }
            content += "-\n";
            foreach (ResourceKey key in IntResourceDictionary.Keys)
            {
                content += Enum.GetName(key) + "\t" + IntResourceDictionary[key] + "\n";
            }
            content += "-\n";
            foreach (ResourceKey key in BoolResourceDictionary.Keys)
            {
                content += Enum.GetName(key) + "\t" + BoolResourceDictionary[key] + "\n";
            }
            File.WriteAllText(_SAVE_FILE_LOCATION, content);
        }

        public void UpdateCurrentModelInformation(VTSCurrentModelData.Data modelData)
        {
            CurrentModelInformation.ID = modelData.modelID;
            CurrentModelInformation.ModelName = modelData.modelName;
        }

        public void UpdateCurrentModelMeshes(VTSArtMeshListData.Data artmeshData)
        {
            CurrentModelInformation.ArtMeshNames = artmeshData.artMeshNames;
            CurrentModelInformation.ArtMeshTags = artmeshData.artMeshTags;
        }

        private void InitializeMissingResource()
        {
            if (!BoolResourceDictionary.ContainsKey(ResourceKey.ConnectVTSOnStart))
            {
                BoolResourceDictionary.Add(ResourceKey.ConnectVTSOnStart, false);
            }
            if (!BoolResourceDictionary.ContainsKey(ResourceKey.ConnectTwitchOnStart))
            {
                BoolResourceDictionary.Add(ResourceKey.ConnectTwitchOnStart, false);
            }
        }
    }
}
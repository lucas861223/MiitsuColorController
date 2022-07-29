using System;

namespace MiitsuColorController.Models
{
    public class VTSMessageData
    {
        public string apiName = "VTubeStudioPublicAPI";
        public long timestamp;
        public string apiVersion = "1.0";
        public string requestID = Guid.NewGuid().ToString();
        public string messageType;
    }

    public class VTSErrorData : VTSMessageData
    {
        public VTSErrorData()
        {
            messageType = "APIError";
            data = new Data();
        }

        public void Copy(VTSErrorData e)
        {
            data = e.data;
        }

        public Data data;

        public class Data
        {
            public int errorID;
            public string message;
        }
    }

    public class VTSStateData : VTSMessageData
    {
        public VTSStateData()
        {
            messageType = "APIStateRequest";
            data = new Data();
        }

        public void Copy(VTSStateData e)
        {
            data = e.data;
        }

        public Data data;

        public class Data
        {
            public bool active;
            public string vTubeStudioVersion;
            public bool currentSessionAuthenticated;
        }
    }

    public class VTSAuthData : VTSMessageData
    {
        public VTSAuthData()
        {
            messageType = "AuthenticationTokenRequest";
            data = new Data();
        }

        public void Copy(VTSAuthData e)
        {
            data = e.data;
        }

        public Data data;

        public class Data
        {
            public string pluginName;
            public string pluginDeveloper;
            public string pluginIcon;
            public string authenticationToken;
            public bool authenticated;
            public string reason;
        }
    }

    public class ArtMeshColorTint : ColorTint
    {
        public float mixWithSceneLightingColor = 1.0f;
    }

    public class ArtMeshMatcher
    {
        public bool tintAll = false;
        public int[] artMeshNumber;
        public string[] nameExact;
        public string[] nameContains;
        public string[] tagExact;
        public string[] tagContains;
    }

    public class ColorTint
    {
        public int colorR;
        public int colorG;
        public int colorB;
        public int colorA;
    }

    public class VTSColorTintData : VTSMessageData
    {
        public VTSColorTintData()
        {
            messageType = "ColorTintRequest";
            data = new Data();
        }

        public Data data;

        public void Copy(VTSColorTintData e)
        {
            data = e.data;
        }

        public class Data
        {
            public ArtMeshColorTint colorTint;
            public ArtMeshMatcher artMeshMatcher;
            public int matchedArtMeshes;
        }
    }

    public class ColorCapturePart : ColorTint
    {
        public bool active;
    }

    public class VTSSceneColorOverlayData : VTSMessageData
    {
        public VTSSceneColorOverlayData()
        {
            messageType = "SceneColorOverlayInfoRequest";
            data = new Data();
        }

        public Data data;

        public void Copy(VTSSceneColorOverlayData e)
        {
            data = e.data;
        }

        public class Data
        {
            public bool active;
            public bool itemsIncluded;
            public bool isWindowCapture;
            public int baseBrightness;
            public int colorBoost;
            public int smoothing;
            public int colorOverlayR;
            public int colorOverlayG;
            public int colorOverlayB;
            public ColorCapturePart leftCapturePart;
            public ColorCapturePart middleCapturePart;
            public ColorCapturePart rightCapturePart;
        }
    }

    public class VTSCurrentModelData : VTSMessageData
    {
        public VTSCurrentModelData()
        {
            this.messageType = "CurrentModelRequest";
            this.data = new Data();
        }

        public Data data;

        public void Copy(VTSCurrentModelData e)
        {
            data = e.data;
        }

        public class Data : VTSModelData
        {
            public string live2DModelName;
            public long modelLoadTime;
            public long timeSinceModelLoaded;
            public int numberOfLive2DParameters;
            public int numberOfLive2DArtmeshes;
            public bool hasPhysicsFile;
            public int numberOfTextures;
            public int textureResolution;
            public ModelPosition modelPosition;
        }
    }

    public class VTSArtMeshListData : VTSMessageData
    {
        public VTSArtMeshListData()
        {
            this.messageType = "ArtMeshListRequest";
            this.data = new Data();
        }

        public Data data;

        public void Copy(VTSArtMeshListData e)
        {
            data = e.data;
        }

        public class Data
        {
            public bool modelLoaded;
            public int numberOfArtMeshNames;
            public int numberOfArtMeshTags;
            public string[] artMeshNames;
            public string[] artMeshTags;
        }
    }

    public class VTSModelData
    {
        public bool modelLoaded;
        public string modelName;
        public string modelID;
        public string vtsModelName;
        public string vtsModelIconName;
    }

    public class ModelPosition
    {
        public float positionX = float.MinValue;
        public float positionY = float.MinValue;
        public float rotation = float.MinValue;
        public float size = float.MinValue;
    }

    public class VTSParameter
    {
        public string name;
        public string addedBy;
        public float value;
        public float min;
        public float max;
        public float defaultValue;
    }

    public class VTSInputParameterListData : VTSMessageData
    {
        public VTSInputParameterListData()
        {
            messageType = "InputParameterListRequest";
            data = new Data();
        }

        public Data data;

        public void Copy(VTSInputParameterListData e)
        {
            data = e.data;
        }

        public class Data
        {
            public bool modelLoaded;
            public string modelName;
            public string modelID;
            public VTSParameter[] customParameters;
            public VTSParameter[] defaultParameters;
        }
    }

    public class VTSParameterValueData : VTSMessageData
    {
        public VTSParameterValueData()
        {
            messageType = "ParameterValueRequest";
            data = new Data();
        }

        public Data data;

        public void Copy(VTSParameterValueData e)
        {
            data = e.data;
        }

        public class Data : VTSParameter
        { }
    }

    public class VTSLive2DParameterListData : VTSMessageData
    {
        public VTSLive2DParameterListData()
        {
            messageType = "Live2DParameterListRequest";
            data = new Data();
        }

        public Data data;

        public void Copy(VTSLive2DParameterListData e)
        {
            data = e.data;
        }

        public class Data
        {
            public bool modelLoaded;
            public string modelName;
            public string modelID;
            public VTSParameter[] parameters;
        }
    }

    public class VTSCustomParameter
    {
        // 4-32 characters, alphanumeric
        public string parameterName;

        public string explanation;
        public float min;
        public float max;
        public float defaultValue;
    }

    public class VTSParameterCreationData : VTSMessageData
    {
        public VTSParameterCreationData()
        {
            messageType = "ParameterCreationRequest";
            data = new Data();
        }

        public Data data;

        public void Copy(VTSParameterCreationData e)
        {
            data = e.data;
        }

        public class Data : VTSCustomParameter
        { }
    }

    public class VTSParameterDeletionData : VTSMessageData
    {
        public VTSParameterDeletionData()
        {
            messageType = "ParameterDeletionRequest";
            data = new Data();
        }

        public Data data;

        public void Copy(VTSParameterDeletionData e)
        {
            data = e.data;
        }

        public class Data
        {
            public string parameterName;
        }
    }

    public class VTSParameterInjectionValue
    {
        public string id;
        public float value = float.MinValue;
        public float weight = float.MinValue;
    }

    public class VTSInjectParameterData : VTSMessageData
    {
        public VTSInjectParameterData()
        {
            messageType = "InjectParameterDataRequest";
            data = new Data();
        }

        public Data data;

        public void Copy(VTSInjectParameterData e)
        {
            data = e.data;
        }

        public class Data
        {
            public VTSParameterInjectionValue[] parameterValues;
        }
    }
}
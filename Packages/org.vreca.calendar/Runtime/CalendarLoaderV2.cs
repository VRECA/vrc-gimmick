using JLChnToZ.VRC.Foundation;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.Image;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

namespace VRCEA.Calendar {
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public partial class CalendarLoaderV2 : UdonSharpBehaviour {
        [SerializeField, Multiline] string instnaceTypeNameMapJson;
        [SerializeField] VRCUrl dataUrl;
        [SerializeField] string imageUrlPattern;
        [GeneratedUrls(PatternSourceProperty = nameof(imageUrlPattern))]
        [SerializeField, HideInInspector] DataDictionary key2url;
        [SerializeField, HideInInspector] DataDictionary instanceTypeNameMap;
        [SerializeField] GameObject entryPrefab;
        [SerializeField] TextureInfo defaultPosterTextureInfo;
        Transform entryParent;
        DataList spawnedEntries = new DataList();
        VRCImageDownloader imageDownloader;

        void Start() {
            _Reload();
            entryParent = entryPrefab.transform.parent;
            imageDownloader = new VRCImageDownloader();
        }

        public void _Reload() {
            VRCStringDownloader.LoadUrl(dataUrl, (IUdonEventReceiver)(object)this);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result) {
            if (!VRCJson.TryDeserializeFromJson(result.Result, out var data) || data.TokenType != TokenType.DataDictionary) return;
            var rawDataRoot = data.DataDictionary;
            if (!rawDataRoot.TryGetValue("data", TokenType.DataList, out data)) return;
            var rawData = data.DataList;
            int count = rawData.Count;
            int spawnedCount = spawnedEntries.Count;
            for (int i = 0; i < count; i++) {
                CalendarEntry entryHandler;
                if (i >= spawnedCount) {
                    var entry = Instantiate(entryPrefab);
                    entry.transform.SetParent(entryParent, false);
                    entryHandler = entry.GetComponent<CalendarEntry>();
                    spawnedEntries.Add(entryHandler);
                    entryHandler.instanceTypeNameMap = instanceTypeNameMap;
                    entryHandler.key2Url = key2url;
                    entryHandler.imageDownloader = imageDownloader;
                    entryHandler.posterTextureInfo = defaultPosterTextureInfo;
                } else
                    entryHandler = (CalendarEntry)spawnedEntries[i].Reference;
                entryHandler.data = rawData[i].DataDictionary;
            }
            for (int i = count; i < spawnedCount; i++)
                ((CalendarEntry)spawnedEntries[i].Reference).gameObject.SetActive(false);
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    public partial class CalendarLoaderV2 : ISelfPreProcess {
        int IPrioritizedPreProcessor.Priority => 0;

        void ISelfPreProcess.PreProcess() {
            instanceTypeNameMap = VRCJson.TryDeserializeFromJson(instnaceTypeNameMapJson, out var data) &&
                data.TokenType == TokenType.DataDictionary ?
                data.DataDictionary : new DataDictionary();
            instnaceTypeNameMapJson = "";
        }
    }
#endif
}
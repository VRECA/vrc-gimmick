
using JLChnToZ.VRC.Foundation;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.Image;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace VRCEA.Calendar {
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class CalendarLoaderV2 : UdonSharpBehaviour {
        [SerializeField] VRCUrl dataUrl;
        [SerializeField] VRCUrl[] imageUrls;
        [SerializeField] GameObject entryPrefab;
        Transform entryParent;
        DataList spawnedEntries;

        void Start() {
            _Reload();
            entryParent = entryPrefab.transform.parent;
        }

        public void _Reload() {
            VRCStringDownloader.LoadUrl(dataUrl, (IUdonEventReceiver)(object)this);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result) {
            if (!VRCJson.TryDeserializeFromJson(result.Result, out var data) || data.TokenType != TokenType.DataList) return;
            var rawData = data.DataList;
            if (spawnedEntries == null) spawnedEntries = new DataList();
            int count = rawData.Count;
            int spawnedCount = spawnedEntries.Count;
            for (int i = 0; i < count; i++) {
                CalendarEntry entryHandler;
                if (i >= spawnedCount) {
                    var entry = Instantiate(entryPrefab);
                    entry.transform.SetParent(entryParent, false);
                    entryHandler = entry.GetComponent<CalendarEntry>();
                    spawnedEntries.Add(entryHandler);
                } else
                    entryHandler = (CalendarEntry)spawnedEntries[i].Reference;
                entryHandler.data = rawData[i].DataDictionary;
            }
            for (int i = count; i < spawnedCount; i++)
                ((CalendarEntry)spawnedEntries[i].Reference).gameObject.SetActive(false);
        }
    }
}
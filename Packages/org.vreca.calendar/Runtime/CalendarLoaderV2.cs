using System.Globalization;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDK3.Image;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using JLChnToZ.VRC.Foundation;

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
        [SerializeField, BindEvent(nameof(Button.onClick), nameof(_Reload))] Button reloadButton;
        [SerializeField] GameObject loadingIndicator, errorIndicator, noDataIndicator;
        [SerializeField] TMP_Text errorText;
        [SerializeField] string dataErrorFormatMessage;
        [SerializeField] string regionCode;
        string errorFormat;
        Transform entryParent;
        DataList spawnedEntries = new DataList();
        VRCImageDownloader imageDownloader;
        CultureInfo cultureInfo;

        void Start() {
            cultureInfo = !string.IsNullOrEmpty(regionCode) ? CultureInfo.GetCultureInfo(regionCode) : CultureInfo.InvariantCulture;
            _Reload();
            entryParent = entryPrefab.transform.parent;
            imageDownloader = new VRCImageDownloader();
        }

        public void _Reload() {
            if (Utilities.IsValid(reloadButton)) reloadButton.interactable = false;
            if (Utilities.IsValid(loadingIndicator)) loadingIndicator.SetActive(true);
            if (Utilities.IsValid(errorIndicator)) errorIndicator.SetActive(false);
            VRCStringDownloader.LoadUrl(dataUrl, (IUdonEventReceiver)(object)this);
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result) {
            if (!VRCJson.TryDeserializeFromJson(result.Result, out var data) || data.TokenType != TokenType.DataDictionary) {
                ShowError(data.ToString());
                return;
            }
            var rawDataRoot = data.DataDictionary;
            if (!rawDataRoot.TryGetValue("data", TokenType.DataList, out data)) {
                ShowError(dataErrorFormatMessage);
                return;
            }
            if (Utilities.IsValid(reloadButton)) reloadButton.interactable = true;
            if (Utilities.IsValid(loadingIndicator)) loadingIndicator.SetActive(false);
            if (Utilities.IsValid(errorIndicator)) errorIndicator.SetActive(false);
            var rawData = data.DataList;
            int count = rawData.Count;
            if (Utilities.IsValid(noDataIndicator)) noDataIndicator.SetActive(count == 0);
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
                    entryHandler.currentCultureInfo = cultureInfo;
                } else
                    entryHandler = (CalendarEntry)spawnedEntries[i].Reference;
                entryHandler.data = rawData[i].DataDictionary;
            }
            for (int i = count; i < spawnedCount; i++)
                ((CalendarEntry)spawnedEntries[i].Reference).gameObject.SetActive(false);
        }

        public override void OnStringLoadError(IVRCStringDownload result) {
            ShowError(result.Error);
        }

        void ShowError(string message) {
            if (Utilities.IsValid(reloadButton))
                reloadButton.interactable = true;
            if (Utilities.IsValid(loadingIndicator))
                loadingIndicator.SetActive(false);
            if (Utilities.IsValid(errorIndicator))
                errorIndicator.SetActive(true);
            if (Utilities.IsValid(errorText)) {
                if (string.IsNullOrEmpty(errorFormat))
                    errorFormat = errorText.text;
                errorText.text = string.Format(errorFormat, message);
            }
        }
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    public partial class CalendarLoaderV2 : ISelfPreProcess {
        int IPrioritizedPreProcessor.Priority => 0;

        void ISelfPreProcess.PreProcess() {
            instanceTypeNameMap = VRCJson.TryDeserializeFromJson(instnaceTypeNameMapJson, out var data) &&
                data.TokenType == TokenType.DataDictionary ?
                data.DataDictionary.DeepClone() : new DataDictionary();
            instnaceTypeNameMapJson = "";
        }
    }
#endif
}
using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;
using VRC.SDK3.Image;
using VRC.SDK3.Data;
using VRC.Udon.Common.Interfaces;
using VRC.Economy;
using UdonSharp;
using JLChnToZ.VRC.Foundation;

namespace VRCEA.Calendar {
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class CalendarEntry : UdonSharpBehaviour {
        [SerializeField] TMP_Text[] contents;
        string[] foramts;
        [SerializeField, TextArea] string[] cancelledFormats;
        [SerializeField, BindEvent(nameof(Toggle.onValueChanged), nameof(_ExpandToggleClick))]
        Toggle expandToggle;
        [SerializeField, BindEvent(nameof(Button.onClick), nameof(_GroupButtonClick))]
        Button groupButton;
        [SerializeField, HideInInspector, Resolve(nameof(groupButton))]
        GameObject groupButtonObject;
        [SerializeField] GameObject posterImageContainer;
        [SerializeField, HideInInspector, Resolve(nameof(posterImageContainer) + "#/**")]
        RawImage posterImage;
        [SerializeField, HideInInspector, Resolve(nameof(posterImage))]
        AspectRatioFitter posterAspect;
        [SerializeField, Resolve(nameof(posterAspect) + "#..*")]
        LayoutElement posterLayoutElement;
        StringBuilder sb;
        string groupId;
        DataToken poster;
        object[] args;
        DateTime timeStart, timeEnd;
        bool hasLoadedImage;

        [NonSerialized]
#if COMPILER_UDONSHARP
        public
#else
        internal
#endif
        DataDictionary data, instanceTypeNameMap, key2Url;

        [NonSerialized]
#if COMPILER_UDONSHARP
        public
#else
        internal
#endif
        VRCImageDownloader imageDownloader;

        [NonSerialized]
#if COMPILER_UDONSHARP
        public
#else
        internal
#endif
        TextureInfo posterTextureInfo;

#if COMPILER_UDONSHARP
        public
#else
        internal
#endif
        void _onVarChange_data() {
            if (!Utilities.IsValid(args) || args.Length < 10) args = new object[10];
            ParseString("title", 0);
            ParseString("description", 1);
            ParseString("group_name", 5);
            ParseStringArray("tags", " ", "#{0}", 7);
            timeStart = ParseTime("time_start");
            timeEnd = ParseTime("time_end");
            args[2] = timeStart;
            args[3] = timeEnd;
            args[4] = (timeEnd - timeStart).TotalHours;
            args[6] = data.TryGetValue("instance_type", out var dt) && instanceTypeNameMap.TryGetValue(dt, TokenType.String, out dt) ? dt.String : "";
            groupId = ParseString("group_id");
            data.TryGetValue("poster", out poster);
            var cancelled = data.TryGetValue("cancelled", TokenType.Boolean, out dt) && dt.Boolean;
            if (!Utilities.IsValid(foramts) || foramts.Length != contents.Length) {
                foramts = new string[contents.Length];
                for (int i = 0; i < contents.Length; i++)
                    foramts[i] = contents[i].text;
            }
            for (int i = 0; i < contents.Length; i++)
                contents[i].text = string.Format(cancelled ? cancelledFormats[i] : foramts[i], args);
            gameObject.SetActive(true);
            if (Utilities.IsValid(posterImageContainer))
                posterImageContainer.SetActive(false);
            if (Utilities.IsValid(groupButtonObject))
                groupButtonObject.SetActive(!string.IsNullOrEmpty(groupId));
            hasLoadedImage = false;
        }

        string ParseString(string key) => data.TryGetValue(key, TokenType.String, out var dt) ? dt.String : "";

        void ParseString(string key, int index) {
            if (data.TryGetValue(key, TokenType.String, out var dt)) {
                args[index] = dt.String;
                return;
            }
            args[index] = "";
        }

        DateTime ParseTime(string key) {
            if (data.TryGetValue(key, TokenType.String, out var dt) &&
                DateTime.TryParse(dt.String, out var t)) {
                return t.ToLocalTime();
            }
            return default;
        }

        void ParseStringArray(string key, string separator, string format, int index) {
            if (data.TryGetValue(key, TokenType.DataList, out var dt)) {
                var list = dt.DataList;
                if (sb == null) sb = new StringBuilder();
                else sb.Clear();
                for (int i = 0, count = list.Count; i < count; i++) {
                    dt = list[i];
                    sb.AppendFormat(format, dt);
                    if (i < count - 1) sb.Append(separator);
                }
                args[index] = sb.ToString();
                return;
            }
            args[index] = "";
        }

#if COMPILER_UDONSHARP
        public
#else
        internal
#endif
        void _ExpandToggleClick() {
            if (!expandToggle.isOn || hasLoadedImage || !Utilities.IsValid(posterImage)) return;
            hasLoadedImage = true;
            if (!key2Url.TryGetValue(poster, TokenType.Reference, out var url)) return;
            imageDownloader.DownloadImage((VRCUrl)url.Reference, null, (IUdonEventReceiver)(object)this, posterTextureInfo);
        }

#if COMPILER_UDONSHARP
        public
#else
        internal
#endif
        void _GroupButtonClick() {
            if (string.IsNullOrEmpty(groupId)) return;
#if DEBUG
            Debug.Log($"Opening group page: {groupId}");
#endif
            Store.OpenGroupPage(groupId);
        }

        public override void OnImageLoadSuccess(IVRCImageDownload result) {
            var resultTexture = result.Result;
            posterImage.texture = resultTexture;
            var ratio = (float)resultTexture.width / resultTexture.height;
            if (Utilities.IsValid(posterAspect))
                posterAspect.aspectRatio = ratio;
            if (Utilities.IsValid(posterLayoutElement)) {
                var preferredWidth = posterLayoutElement.preferredWidth;
                if (preferredWidth >= 0) posterLayoutElement.preferredHeight = preferredWidth / ratio;
                else {
                    var preferredHeight = posterLayoutElement.preferredHeight;
                    if (preferredHeight >= 0) posterLayoutElement.preferredWidth = preferredHeight * ratio;
                }
            }
            if (Utilities.IsValid(posterImageContainer))
                posterImageContainer.SetActive(true);
        }
    }
}
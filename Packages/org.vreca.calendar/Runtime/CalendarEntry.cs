using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDK3.Data;
using VRC.Economy;
using UdonSharp;
using JLChnToZ.VRC.Foundation;

namespace VRCEA.Calendar {
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class CalendarEntry : UdonSharpBehaviour {
        [SerializeField] TMP_Text dateText, titleText;
        [SerializeField, Multiline] string dateFormat, titleFormat;
        [SerializeField, BindEvent(nameof(Button.onClick), nameof(_ButtonClick))] Button clickButton;
        StringBuilder sb;
#if COMPILER_UDONSHARP
        public
#else
        internal
#endif
        DataDictionary data;
        string title, summary, groupId, tags;
        DateTime timeStart, timeEnd;
        double duration;

#if COMPILER_UDONSHARP
        public
#else
        internal
#endif
        void _onVarChange_data() {
            ParseString("title", out title);
            ParseString("summary", out summary);
            ParseString("group_id", out groupId);
            ParseTime("time_start", out timeStart);
            ParseTime("time_end", out timeEnd);
            ParseStringArray("tags", " ", "#{0}", out tags);
            duration = (timeEnd - timeStart).TotalHours;
            dateText.text = string.Format(dateFormat, title, summary, groupId, tags, timeStart, timeEnd, duration, "", "");
            titleText.text = string.Format(titleFormat, title, summary, groupId, tags, timeStart, timeEnd, duration, "", "");
            gameObject.SetActive(true);
        }

        void ParseString(string key, out string value) {
            if (data.TryGetValue(key, TokenType.String, out var dt)) {
                value = dt.String;
                return;
            }
            value = "";
        }

        void ParseTime(string key, out DateTime time) {
            if (data.TryGetValue(key, TokenType.String, out var dt) &&
                DateTime.TryParse(dt.String, out var t)) {
                time = t.ToLocalTime();
                return;
            }
            time = default;
        }

        void ParseStringArray(string key, string separator, string format, out string values) {
            if (data.TryGetValue(key, TokenType.DataList, out var dt)) {
                var list = dt.DataList;
                if (sb == null) sb = new StringBuilder();
                else sb.Clear();
                for (int i = 0, count = list.Count; i < count; i++) {
                    dt = list[i];
                    sb.AppendFormat(format, dt);
                    if (i < count - 1) sb.Append(separator);
                }
                values = sb.ToString();
                return;
            }
            values = "";
        }

#if COMPILER_UDONSHARP
        public
#else
        internal
#endif
        void _ButtonClick() {
            if (string.IsNullOrEmpty(groupId)) return;
#if DEBUG
            Debug.Log($"Opening group page: {groupId}");
#endif
            Store.OpenGroupPage(groupId);
        }
    }
}
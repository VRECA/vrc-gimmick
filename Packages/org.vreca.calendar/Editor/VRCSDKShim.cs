using UnityEngine;
using UnityEditor;
using VRC.Economy;

namespace VRCEA.Calendar {
    public static class VRCSDKShim {
        [InitializeOnLoadMethod]
        static void OnInitialize() =>
            Store._openGroupPage ??= OpenGroupPage;

        private static void OpenGroupPage(string groupId) =>
            Application.OpenURL($"https://vrchat.com/home/group/{groupId}");
    }
}

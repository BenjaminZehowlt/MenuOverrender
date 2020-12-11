using Harmony;
using MelonLoader;
using System.Linq;
using System.Reflection;
using UnityEngine;
using VRC;
using VRC_MenuOverrender;

[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonInfo(typeof(MenuOverrenderMod), "MenuOverrender", "1.0.1", "Ben")]

namespace VRC_MenuOverrender
{
    public class MenuOverrenderMod : MelonMod
    {

        private static GameObject _menuCameraClone;
        private static Camera _menuCameraUI;
        private static Camera _originalCamera;

        private static int _uiLayer;
        private static int _uiMenuLayer;
        private static int _playerLocalLayer;
        private static int _playerLayer;

        private static int _uiPlayerNameplateLayer = 30;

        public override void VRChat_OnUiManagerInit()
        {
            VRCVrCamera vrCamera = VRCVrCamera.field_Private_Static_VRCVrCamera_0;
            if (!vrCamera)
                return;
            Camera screenCamera = vrCamera.screenCamera;
            if (!screenCamera)
                return;

            _originalCamera = screenCamera;

            MelonLogger.Log("Current Mask: " + screenCamera.cullingMask);

            screenCamera.cullingMask = screenCamera.cullingMask
                & ~(1 << LayerMask.NameToLayer("UiMenu"))
                & ~(1 << LayerMask.NameToLayer("UI"));
            screenCamera.cullingMask = screenCamera.cullingMask | (1 << _uiPlayerNameplateLayer);

            MelonLogger.Log("New Mask: " + screenCamera.cullingMask);

            _menuCameraClone = new GameObject();
            _menuCameraClone.transform.parent = screenCamera.transform.parent;

            _menuCameraUI = _menuCameraClone.AddComponent<Camera>();
            _menuCameraUI.cullingMask =
                (1 << LayerMask.NameToLayer("UiMenu"))
                | (1 << LayerMask.NameToLayer("UI"));
            _menuCameraUI.clearFlags = CameraClearFlags.Depth;

            // VRChat does 10/10 UI Setup
            _uiLayer = LayerMask.NameToLayer("UI");
            _uiMenuLayer = LayerMask.NameToLayer("UiMenu");
            _playerLocalLayer = LayerMask.NameToLayer("PlayerLocal");
            _playerLayer = LayerMask.NameToLayer("Player");

            GameObject loadingScreenOverlayPanel = GameObject.Find("/UserInterface/MenuContent/Popups/LoadingPopup/3DElements/LoadingInfoPanel");
            
            foreach (Transform infoPanel in loadingScreenOverlayPanel.transform.GetComponentsInChildren<Transform>())
            {
                infoPanel.gameObject.layer = _uiMenuLayer;
            }

            var harmonyInstance = HarmonyInstance.Create("VRC-MenuOverrender");

            harmonyInstance.Patch(
                typeof(VRCUiBackgroundFade).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.Contains("Method_Public_Void_Single_Action") && !m.Name.Contains("PDM")).First(),
                postfix: new HarmonyMethod(typeof(MenuOverrenderMod).GetMethod("OnFade", BindingFlags.Static | BindingFlags.NonPublic)));

            harmonyInstance.Patch(
                typeof(SimpleAvatarPedestal).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.Contains("Method_Private_Void_GameObject")).First(),
                postfix: new HarmonyMethod(typeof(MenuOverrenderMod).GetMethod("OnAvatarScale", BindingFlags.Static | BindingFlags.NonPublic)));

            harmonyInstance.Patch(typeof(PlayerNameplate).GetMethod("Method_Public_Void_0", BindingFlags.Public | BindingFlags.Instance), 
                prefix: new HarmonyMethod(typeof(MenuOverrenderMod).GetMethod("OnRebuild", BindingFlags.NonPublic | BindingFlags.Static)));
        }

        public override void OnUpdate()
        {
            if (_menuCameraClone != null)
            {
                _menuCameraClone.transform.localPosition = _originalCamera.transform.localPosition;

                if (_menuCameraUI != null)
                {
                    _menuCameraUI.nearClipPlane = _originalCamera.nearClipPlane;
                    _menuCameraUI.farClipPlane = _originalCamera.farClipPlane;
                }
            }
        }

        // Switch the nameplate to a new layer (probably a bad idea but fixes the nameplates overrendering the world)
        private static void OnRebuild(PlayerNameplate __instance)
        {
            if (__instance != null
                && __instance.gameObject.layer != _uiPlayerNameplateLayer)
            {
                SetLayerRecursively(__instance.transform.parent.parent.parent, _uiPlayerNameplateLayer, _uiMenuLayer);
                SetLayerRecursively(__instance.transform.parent.parent.parent, _uiPlayerNameplateLayer, _uiLayer);
            }
        }

        // Make the avatar render on the menu not behind it
        private static void OnAvatarScale(ref SimpleAvatarPedestal __instance, GameObject __0)
        {
            if (__instance != null && __0 != null)
            {
                SetLayerRecursively(__0.transform, _uiMenuLayer, _playerLocalLayer);
            }
        }

        private static void OnFade()
        {
            if (_menuCameraClone != null)
            {
                _menuCameraUI.clearFlags = CameraClearFlags.Depth;
            }
        }

        public static void SetLayerRecursively(Transform obj, int newLayer, int match)
        {
            if (obj.gameObject.name.Equals("SelectRegion"))
            {
                return;
            }

            if (obj.gameObject.layer == match)
            {
                obj.gameObject.layer = newLayer;
            }

            foreach (var o in obj)
            {
                var otherTransform = o.Cast<Transform>();
                SetLayerRecursively(otherTransform, newLayer, match);
            }
        }
    }
}

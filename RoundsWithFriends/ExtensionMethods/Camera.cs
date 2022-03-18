using UnityEngine;

namespace RWF
{
    // Extension methods for dealing with ultrawide displays
    internal static class CameraExtensions
    {
        private static float correction => (Screen.width - FixedScreen.fixedWidth) / 2f;

        internal static Vector3 FixedWorldToScreenPoint(this Camera camera, Vector3 worldPoint)
        {
            var fixedScreenPoint = camera.WorldToScreenPoint(worldPoint);
            return FixedScreen.isUltraWide
                ? new Vector3(fixedScreenPoint.x - correction, fixedScreenPoint.y, fixedScreenPoint.z)
                : camera.WorldToScreenPoint(worldPoint);
        }

        internal static Vector3 FixedScreenToWorldPoint(this Camera camera, Vector3 fixedScreenPoint)
        {
            return FixedScreen.isUltraWide
                ? new Vector3(fixedScreenPoint.x + correction, fixedScreenPoint.y, fixedScreenPoint.z)
                : camera.ScreenToWorldPoint(fixedScreenPoint);
        }
    }

    // extension for dealing with ultrawide displays
    internal static class FixedScreen
    {
        internal static bool isUltraWide => ((float) Screen.width / (float) Screen.height - FixedScreen.ratio >= FixedScreen.eps);
        private const float ratio = 16f / 9f;
        private const float eps = 1E-1f;

        internal static int fixedWidth => FixedScreen.isUltraWide
            ? (int) UnityEngine.Mathf.RoundToInt(Screen.height * FixedScreen.ratio)
            : Screen.width;
    }
}

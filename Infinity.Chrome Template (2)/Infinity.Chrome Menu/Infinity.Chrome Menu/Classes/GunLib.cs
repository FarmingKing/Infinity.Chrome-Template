using BepInEx;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR;

namespace Kings.Classes
{
    public static class GunLib
    {
        public static VRRig LockedPlayer;
        public static GameObject spherepointer;
        public static GameObject linepointer;
        public static LineRenderer linerenderer;
        public static RaycastHit raycastHitGun;

        private static GameObject glowLinePointer;
        private static LineRenderer glowLineRenderer;

        private static Material sphereMaterial;
        private static Material lineMaterial;
        private static Material glowLineMaterial;

        private const float pointerBaseScale = 0.18f;
        private const float lineWidth = 0.02f;
        private const float glowWidthMultiplier = 2.75f;
        private const int lightningSegments = 30;
        private const float lightningAmplitude = 0.16f;
        private const float lightningAnimationSpeed = 26f;
        private const float lightningArcStrength = 0.22f;
        private const float lightningPulseSpeed = 9f;
        private const float lightningRetargetInterval = 0.035f;

        private static readonly Vector3[] lightningPoints = new Vector3[lightningSegments + 1];

        private static float lastLightningRetargetTime;
        private static float lightningSeedA = 17.37f;
        private static float lightningSeedB = 83.91f;

        public static void StartBothGuns(Action action, bool lockOn)
        {
            if (XRSettings.isDeviceActive) StartVrGun(action, lockOn);
            else StartPcGun(action, lockOn);
        }

        public static void StartVrGun(Action action, bool lockOn)
        {
            bool gripHeld = ControllerInputPoller.instance.rightGrab;
            bool triggerHeld = ControllerInputPoller.instance.rightControllerIndexFloat > 0.5f;

            if (!gripHeld)
            {
                HandleGunLogic(action, lockOn, XR: true, triggerHeld: false);
                return;
            }

            Physics.Raycast(GorillaTagger.Instance.rightHandTransform.position, -GorillaTagger.Instance.rightHandTransform.up, out raycastHitGun, float.MaxValue);
            HandleGunLogic(action, lockOn, XR: true, triggerHeld: triggerHeld);
        }

        public static void StartPcGun(Action action, bool lockOn)
        {
            Camera cam = GameObject.Find("Shoulder Camera")?.GetComponent<Camera>();
            if (cam == null) cam = GorillaTagger.Instance.mainCamera.GetComponent<Camera>();

            Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());

            bool gripHeld = UnityInput.Current.GetMouseButton(1);
            bool triggerPressed = UnityInput.Current.GetMouseButton(0);

            Physics.Raycast(ray.origin, ray.direction, out raycastHitGun, 100f);
            HandleGunLogic(action, lockOn, XR: false, triggerHeld: gripHeld && triggerPressed);
        }

        private static void HandleGunLogic(Action action, bool lockOn, bool XR, bool triggerHeld)
        {
            bool gripHeld = XR ? ControllerInputPoller.instance.rightGrab : UnityInput.Current.GetMouseButton(1);

            if (!gripHeld)
            {
                Cleanup();
                return;
            }

            if (spherepointer == null || linepointer == null || linerenderer == null || glowLinePointer == null || glowLineRenderer == null)
                CreatePointer();

            Color pointerColor = triggerHeld
                ? Color.red
                : StupidTemplate.Settings.backgroundColor.GetCurrentColor();

            UpdatePointerColor(pointerColor);

            Vector3 startPos = XR
                ? GorillaTagger.Instance.rightHandTransform.position
                : GorillaTagger.Instance.mainCamera.transform.position;

            Vector3 targetPos = startPos;

            if (lockOn && LockedPlayer != null)
                targetPos = LockedPlayer.transform.position;
            else if (raycastHitGun.collider != null)
                targetPos = raycastHitGun.point;

            spherepointer.transform.position = targetPos;
            spherepointer.transform.localScale = Vector3.one * (pointerBaseScale * (1f + (Mathf.Sin(Time.unscaledTime * 14f) * 0.14f)));

            UpdateLightningLine(startPos, targetPos);

            if (triggerHeld)
            {
                if (lockOn && LockedPlayer == null)
                    LockedPlayer = raycastHitGun.collider?.GetComponentInParent<VRRig>();

                action();
            }
            else if (LockedPlayer != null)
            {
                LockedPlayer = null;
            }
        }

        private static void CreatePointer()
        {
            Color color = StupidTemplate.Settings.backgroundColor.GetCurrentColor();

            spherepointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(spherepointer.GetComponent<Collider>());
            spherepointer.transform.localScale = Vector3.zero;

            sphereMaterial = new Material(Shader.Find("GorillaTag/UberShader"));
            spherepointer.GetComponent<Renderer>().material = sphereMaterial;

            linepointer = new GameObject("GunLine");
            linerenderer = linepointer.AddComponent<LineRenderer>();
            lineMaterial = new Material(sphereMaterial);
            linerenderer.material = lineMaterial;
            ConfigureLineRenderer(linerenderer, lineWidth);

            glowLinePointer = new GameObject("GunLineGlow");
            glowLineRenderer = glowLinePointer.AddComponent<LineRenderer>();
            glowLineMaterial = new Material(sphereMaterial);
            glowLineRenderer.material = glowLineMaterial;
            ConfigureLineRenderer(glowLineRenderer, lineWidth * glowWidthMultiplier);

            UpdatePointerColor(color);
        }

        private static void ConfigureLineRenderer(LineRenderer renderer, float width)
        {
            renderer.positionCount = lightningSegments + 1;
            renderer.widthMultiplier = width;
            renderer.useWorldSpace = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.numCapVertices = 8;
            renderer.numCornerVertices = 6;
            renderer.alignment = LineAlignment.View;
            renderer.textureMode = LineTextureMode.Stretch;
            renderer.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.06f, 1f),
                new Keyframe(0.5f, 0.92f),
                new Keyframe(0.94f, 1f),
                new Keyframe(1f, 0f));
        }

        private static void UpdateLightningLine(Vector3 startPos, Vector3 targetPos)
        {
            if (linerenderer == null || glowLineRenderer == null)
                return;

            RefreshLightningSeeds();

            float time = Time.unscaledTime;
            float animatedTime = time * lightningAnimationSpeed;

            Vector3 direction = targetPos - startPos;
            float distance = direction.magnitude;

            if (distance <= 0.001f)
            {
                for (int i = 0; i <= lightningSegments; i++)
                    lightningPoints[i] = Vector3.Lerp(startPos, targetPos, i / (float)lightningSegments);

                ApplyLightningPoints();
                return;
            }

            direction /= distance;

            Vector3 right = Vector3.Cross(direction, Vector3.up);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.Cross(direction, Vector3.forward);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.right;
            right.Normalize();

            Vector3 up = Vector3.Cross(right, direction).normalized;

            float offsetScale = Mathf.Clamp(distance * 0.055f, 0.04f, lightningAmplitude);
            float arcScale = Mathf.Clamp(distance * lightningArcStrength, 0.025f, 0.45f);
            float travelingPulseTime = time * lightningPulseSpeed;

            linerenderer.widthMultiplier = lineWidth * (1f + (Mathf.Sin(time * 20f) * 0.08f));
            glowLineRenderer.widthMultiplier = (lineWidth * glowWidthMultiplier) * (1.05f + (Mathf.Sin(time * 14f) * 0.12f));

            for (int i = 0; i <= lightningSegments; i++)
            {
                float t = i / (float)lightningSegments;
                Vector3 point = Vector3.Lerp(startPos, targetPos, t);

                if (i != 0 && i != lightningSegments)
                {
                    float envelope = Mathf.Sin(t * Mathf.PI);
                    envelope *= envelope;

                    float coarseX = (Mathf.PerlinNoise(lightningSeedA + (t * 2.2f), (animatedTime * 0.045f) + (t * 1.9f)) - 0.5f) * 1.35f;
                    float coarseY = (Mathf.PerlinNoise(lightningSeedB + (t * 2.6f), (animatedTime * 0.05f) + (t * 2.1f)) - 0.5f) * 1.35f;

                    float fineX = (Mathf.PerlinNoise((lightningSeedA * 0.37f) + (t * 7.4f), (animatedTime * 0.12f) + (t * 6.8f)) - 0.5f) * 0.95f;
                    float fineY = (Mathf.PerlinNoise((lightningSeedB * 0.41f) + (t * 6.9f), (animatedTime * 0.14f) + (t * 7.2f)) - 0.5f) * 0.95f;

                    float pulse = 0.7f + Mathf.Abs(Mathf.Sin((t * 13f) - travelingPulseTime + lightningSeedA));
                    float spiral = Mathf.Sin((t * 18f) + (time * 8f) + lightningSeedB);
                    float sideWave = Mathf.Sin((t * 11f) - (time * 10f) + lightningSeedA) * 0.45f;

                    Vector3 arcOffset = Vector3.Lerp(right, up, (spiral + 1f) * 0.5f).normalized * (arcScale * envelope * 0.38f);
                    Vector3 noiseOffset =
                        (right * (coarseX + fineX + sideWave) + up * (coarseY + fineY - sideWave)) *
                        (offsetScale * envelope * pulse);

                    point += arcOffset + noiseOffset;
                }

                lightningPoints[i] = point;
            }

            ApplyLightningPoints();
        }

        private static void ApplyLightningPoints()
        {
            if (linerenderer.positionCount != lightningPoints.Length)
                linerenderer.positionCount = lightningPoints.Length;
            if (glowLineRenderer.positionCount != lightningPoints.Length)
                glowLineRenderer.positionCount = lightningPoints.Length;

            linerenderer.SetPositions(lightningPoints);
            glowLineRenderer.SetPositions(lightningPoints);
        }

        private static void RefreshLightningSeeds()
        {
            float time = Time.unscaledTime;
            if (time - lastLightningRetargetTime < lightningRetargetInterval)
                return;

            lastLightningRetargetTime = time;
            lightningSeedA = UnityEngine.Random.Range(0f, 500f);
            lightningSeedB = UnityEngine.Random.Range(500f, 1000f);
        }

        private static void UpdatePointerColor(Color color)
        {
            SetMaterialColor(sphereMaterial, color, 2.5f);
            SetMaterialColor(lineMaterial, color, 4f);
            SetMaterialColor(glowLineMaterial, color, 6f);

            if (linerenderer != null)
                linerenderer.colorGradient = CreateGradient(color, 0.95f);

            if (glowLineRenderer != null)
                glowLineRenderer.colorGradient = CreateGradient(color, 0.32f);
        }

        private static void SetMaterialColor(Material material, Color color, float emissionStrength)
        {
            if (material == null)
                return;

            Color emissionColor = color * emissionStrength;

            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetColor("_EmissionColor", emissionColor);
            material.EnableKeyword("_EMISSION");
        }

        private static Gradient CreateGradient(Color color, float maxAlpha)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(Color.Lerp(color, Color.white, 0.35f), 0.5f),
                    new GradientColorKey(color, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(maxAlpha, 0.08f),
                    new GradientAlphaKey(maxAlpha, 0.5f),
                    new GradientAlphaKey(maxAlpha, 0.92f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private static void Cleanup()
        {
            if (spherepointer != null)
                UnityEngine.Object.Destroy(spherepointer);
            if (linepointer != null)
                UnityEngine.Object.Destroy(linepointer);
            if (glowLinePointer != null)
                UnityEngine.Object.Destroy(glowLinePointer);
            if (sphereMaterial != null)
                UnityEngine.Object.Destroy(sphereMaterial);
            if (lineMaterial != null)
                UnityEngine.Object.Destroy(lineMaterial);
            if (glowLineMaterial != null)
                UnityEngine.Object.Destroy(glowLineMaterial);

            spherepointer = null;
            linepointer = null;
            glowLinePointer = null;
            linerenderer = null;
            glowLineRenderer = null;
            sphereMaterial = null;
            lineMaterial = null;
            glowLineMaterial = null;
            LockedPlayer = null;
        }
    }
}
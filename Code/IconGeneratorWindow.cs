#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Boddle.IconGenerator
{
    public class IconGeneratorWindow : EditorWindow
    {
        private static readonly Vector2Int windowSize = new Vector2Int(500, 560);
        private static readonly Vector2Int imageSize = new Vector2Int(1024, 1024);
        private static readonly StringBuilder debugInfo = new StringBuilder();
        private static GUIStyle debugStyle = null;
        private Object asset;
        private Camera camera;
        private GameObject preview;
        private float cameraFov;
        private float cameraDistance;
        private float outlineThickness = 2f;
        private Color outlineColor = Color.red;
        private Vector3 cameraOffset;
        private Vector2 cameraRotation;
        private float delta;
        private double lastEditorTime;
        private Shader shader;
        private Material material;
        private Texture2D previewImage;
        private Light sunLight;
        private string directory = "Assets";

        private void OnEnable()
        {
            ClearPreviewCamera();
            CreatePreviewCamera();
            CreateSunLight();
        }

        private void OnDisable()
        {
            ClearPreviewObject();
            ClearPreviewCamera();
            ClearSunLight();
        }

        private void Update()
        {
            Repaint();
            if (preview && camera)
            {
                OnUpdate();
                delta = (float)(EditorApplication.timeSinceStartup - lastEditorTime);
                lastEditorTime = EditorApplication.timeSinceStartup;
                delta *= 0.3f;

                material.SetColor("_OutlineColor", outlineColor);
                material.SetFloat("_OutlineThickness", outlineThickness);
                material.SetVector("_ImageSize", new Vector4(imageSize.x, imageSize.y, 0, 0));
                camera.Render();
                RenderTexture temporary = RenderTexture.GetTemporary(imageSize.x, imageSize.y);
                Graphics.Blit(camera.targetTexture, temporary, material);

                RenderTexture lastActive = RenderTexture.active;
                RenderTexture.active = temporary;
                if (!previewImage)
                {
                    previewImage = new Texture2D(imageSize.x, imageSize.y);
                }

                previewImage.ReadPixels(new Rect(0, 0, imageSize.x, imageSize.y), 0, 0);
                previewImage.Apply();
                RenderTexture.active = lastActive;
                RenderTexture.ReleaseTemporary(temporary);
            }
        }

        private void OnGUI()
        {
            Event currentEvent = Event.current;
            float gap = 5f;
            float createButtonHeight = 40f;
            bool assetIsValid = false;

            Object newAsset = EditorGUILayout.ObjectField("Asset", asset, typeof(Object), false);
            if (asset != newAsset)
            {
                ClearPreviewObject();
                asset = newAsset;
            }

            if (debugStyle is null)
            {
                CreateDebugStyle();
            }

            IconGenerationConfig config = null;
            if (asset is GameObject assetGameObject)
            {
                config = new IconGenerationConfig();
                config.gameObject = assetGameObject;
                assetIsValid = true;
            }
            else if (asset is IIconGenerationCandidate candidate)
            {
                config = candidate.GetConfig();
                assetIsValid = true;
            }

            bool disabled = config is null;
            if (asset && !assetIsValid)
            {
                EditorGUILayout.HelpBox($"Asset must be a prefab or dervied from {nameof(IIconGenerationCandidate)}", MessageType.Warning);
            }
            else if (!disabled && assetIsValid && !config.gameObject)
            {
                EditorGUILayout.HelpBox("A game object is not provided in the config", MessageType.Warning);
            }
            else if (assetIsValid && config is null && asset is IIconGenerationCandidate)
            {
                EditorGUILayout.HelpBox("No config returned from GetConfig() method", MessageType.Warning);
            }

            if (assetIsValid && config != null && config.gameObject)
            {
                cameraDistance = EditorGUILayout.Slider("Distance", cameraDistance, 0.5f, 12f);
                cameraFov = EditorGUILayout.Slider("FOV", cameraFov, 14f, 90f);
                outlineThickness = EditorGUILayout.Slider("Thickness", outlineThickness, 1f, 64f);
                outlineColor = EditorGUILayout.ColorField(new GUIContent("Color"), outlineColor, true, false, false);

                if (!preview)
                {
                    preview = CreatePreviewObject(config);
                }

                float topOffset = 100f;
                Rect previewArea = new Rect(0, topOffset, windowSize.x, windowSize.y - createButtonHeight - gap - gap - topOffset);
                GUI.DrawTexture(previewArea, previewImage, ScaleMode.ScaleToFit);

                debugInfo.Clear();
                debugInfo.Append("Offset: ");
                debugInfo.Append(cameraOffset);
                debugInfo.AppendLine();

                debugInfo.Append("Rotation: ");
                debugInfo.Append(cameraRotation);
                debugInfo.AppendLine();

                GUI.Label(previewArea, debugInfo.ToString(), debugStyle);

                Vector2 mousePosition = currentEvent.mousePosition;
                if (previewArea.Contains(mousePosition))
                {
                    if (currentEvent.type == EventType.MouseDrag)
                    {
                        Vector2 dragDelta = currentEvent.delta;
                        if (currentEvent.button == 2)
                        {
                            //move the preview around
                            cameraOffset -= camera.transform.right * dragDelta.x * delta * 0.5f;
                            cameraOffset -= -camera.transform.up * dragDelta.y * delta * 0.5f;
                        }
                        else if (currentEvent.button == 1)
                        {
                            //rotate the preview around
                            cameraRotation.x += dragDelta.y * delta * 45f;
                            cameraRotation.y += dragDelta.x * delta * 45f;
                        }
                    }
                    else if (currentEvent.type == EventType.ScrollWheel)
                    {
                        cameraDistance += currentEvent.delta.y * 0.05f;
                    }
                }
            }
            else
            {
                if (preview)
                {
                    ClearPreviewObject();
                }
            }

            EditorGUI.BeginDisabledGroup(disabled);
            {
                Rect saveButton = new Rect(gap, windowSize.y - createButtonHeight - gap, windowSize.x - gap - gap, createButtonHeight);
                if (GUI.Button(saveButton, "Save"))
                {
                    string extension = "png";
                    string fileName = $"{asset.name}.{extension}";
                    string pathToSaveTo = EditorUtility.SaveFilePanel("Save new icon", directory, fileName, extension);
                    if (!string.IsNullOrEmpty(pathToSaveTo))
                    {
                        directory = Directory.GetParent(pathToSaveTo).FullName;
                        byte[] resultData = previewImage.EncodeToPNG();
                        File.WriteAllBytes(pathToSaveTo, resultData);
                        AssetDatabase.Refresh();
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private void CreateSunLight()
        {
            sunLight = new GameObject("Sun").AddComponent<Light>();
            sunLight.gameObject.hideFlags = HideFlags.HideAndDontSave;
            sunLight.transform.eulerAngles = new Vector3(50f, -30f, 0f);
            sunLight.type = LightType.Directional;
        }

        private void ClearSunLight()
        {
            if (sunLight)
            {
                DestroyImmediate(sunLight.gameObject);
                sunLight = null;
            }
        }

        private void ClearPreviewCamera()
        {
            if (camera)
            {
                DestroyImmediate(camera.gameObject);
                camera = null;
            }
        }

        private void CreatePreviewCamera()
        {
            camera = new GameObject("Camera").AddComponent<Camera>();
            camera.gameObject.hideFlags = HideFlags.HideAndDontSave;
            camera.targetTexture = new RenderTexture(imageSize.x, imageSize.y, 24);
            camera.forceIntoRenderTexture = true;

            shader = Shader.Find("Boddle/Icon Generator/Screen");
            material = new Material(shader);
        }

        private void CreateDebugStyle()
        {
            debugStyle = new GUIStyle(EditorStyles.miniLabel);
            debugStyle.alignment = TextAnchor.LowerLeft;
            debugStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);
        }

        private void ClearPreviewObject()
        {
            if (preview)
            {
                DestroyImmediate(preview);
                preview = null;
            }
        }

        private void OnUpdate()
        {
            camera.fieldOfView = cameraFov;
            camera.transform.eulerAngles = cameraRotation;
            camera.transform.position = preview.transform.position - camera.transform.forward * cameraDistance;
            camera.transform.position += cameraOffset;

            float maxDistanceAwayFromCenter = 4f;
            if (cameraOffset.sqrMagnitude > maxDistanceAwayFromCenter * maxDistanceAwayFromCenter)
            {
                cameraOffset = cameraOffset.normalized * maxDistanceAwayFromCenter;
            }
        }

        private GameObject CreatePreviewObject(IconGenerationConfig config)
        {
            if (!camera)
            {
                CreatePreviewCamera();
            }

            GameObject gameObject = Instantiate(config.gameObject);
            gameObject.name = "Preview";
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(mpb);
                foreach (MaterialPropertyBlockProperty property in config.properties)
                {
                    if (property.HasValue)
                    {
                        object value = property.Value;
                        if (value is int intValue)
                        {
                            mpb.SetInt(property.Name, intValue);
                        }
                        else if (value is float floatValue)
                        {
                            mpb.SetFloat(property.Name, floatValue);
                        }
                        else if (value is Color colorValue)
                        {
                            mpb.SetColor(property.Name, colorValue);
                        }
                    }
                }

                renderer.SetPropertyBlock(mpb);
            }

            return gameObject;
        }

        [MenuItem("Boddle/Icon Generator %#g")]
        public static void ShowWindow()
        {
            IconGeneratorWindow window = GetWindow<IconGeneratorWindow>(true, "Icon Generator");
            window.minSize = windowSize;
            window.maxSize = windowSize;
        }
    }
}
#endif
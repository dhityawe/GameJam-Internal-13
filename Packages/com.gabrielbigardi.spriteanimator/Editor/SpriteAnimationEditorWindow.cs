#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Sprites;
using UnityEngine;

namespace GabrielBigardi.SpriteAnimator
{
    public class SpriteAnimationEditorWindow : ExtendedEditorWindow
    {
        private enum PreviewFrameEventType
        {
            Unknown,
            Hit,
            UnlockMovement,
            MoveForce,
            SpawnVFX,
            PlaySound,
            CameraShake
        }

        private struct PreviewFrameEventInfo
        {
            public PreviewFrameEventType Type;
            public int StartFrame;
            public int EndFrame;
            public string Label;
        }

        private struct PreviewHitboxInfo
        {
            public bool IsValid;
            public bool IsCircle;
            public Vector2 Offset;
            public float Radius;
            public Vector2 Size;
        }

        private Texture2D _sheetGenerationTexture;
        private Texture2D _transparentCheckboardTexture;
        [SerializeField] private SpriteAnimationObject _spriteAnimationObject;

        private float _lastTime;
        private float _unscaledDeltaTime;
        private float _deltaTime;
        private float _timeScale = 1f;
        private double _lastIdleRepaintTime;

        private int _currentFrame = 0;
        private float _animationTime;

        private bool _animationIsRunning;
        private bool _animationComplete;

        private Texture2D _buttonPlayTexture;
        private Texture2D _buttonPauseTexture;
        private Texture2D _buttonNextFrameTexture;
        private Texture2D _buttonPreviousFrameTexture;
        private readonly float _maxItemListColumnWidth = 150;
        private readonly float _maxSpritePreviewColumnWidth = 320;
        private Vector2 _scrollPosition;
        private Texture2D CurrentPauseButtonLabel => _animationIsRunning ? _buttonPauseTexture : _buttonPlayTexture;

        private int _previousFramesPropertySize = 0;

        public static void Open(SpriteAnimationObject spriteAnimationObject)
        {
            SpriteAnimationEditorWindow window = GetWindow<SpriteAnimationEditorWindow>("Sprite Animation Editor");
            window._serializedObject = new SerializedObject(spriteAnimationObject);
            window._spriteAnimationObject = spriteAnimationObject;
            window._selectedProperty = null;
            window._selectedPropertyPath = "";
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _scrollPosition = Vector2.zero;

            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string scriptFolder = Path.GetDirectoryName(scriptPath);
            string spritesPath = Path.Combine("Icons");

            string checkeredTexturePath = Path.Combine(scriptFolder, spritesPath, "checkered.png");
            _transparentCheckboardTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(checkeredTexturePath);

            string buttonPlayFullPath = Path.Combine(scriptFolder, spritesPath, "buttonplaytexture.png");
            _buttonPlayTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(buttonPlayFullPath);

            string buttonPauseFullPath = Path.Combine(scriptFolder, spritesPath, "buttonpausetexture.png");
            _buttonPauseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(buttonPauseFullPath);

            string buttonNextFrameFullPath = Path.Combine(scriptFolder, spritesPath, "buttonnextframetexture.png");
            _buttonNextFrameTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(buttonNextFrameFullPath);

            string buttonPreviousFrameFullPath = Path.Combine(scriptFolder, spritesPath, "buttonpreviousframetexture.png");
            _buttonPreviousFrameTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(buttonPreviousFrameFullPath);

            if (_spriteAnimationObject != null)
            {
                _serializedObject = new SerializedObject(_spriteAnimationObject);
                _currentProperty = _serializedObject.FindProperty("SpriteAnimations");
            }

            _lastTime = Time.realtimeSinceStartup;
            EditorApplication.update += OnEditorApplicationUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorApplicationUpdate;
        }

        private void OnEditorApplicationUpdate()
        {
            _unscaledDeltaTime = Time.realtimeSinceStartup - _lastTime;
            _deltaTime = (Time.realtimeSinceStartup - _lastTime) * _timeScale;
            _lastTime = Time.realtimeSinceStartup;

            if (_spriteAnimationObject != null)
            {
                if (_selectedProperty == null)
                    return;

                SerializedProperty framesProperty = _currentProperty.FindPropertyRelative("Frames");
                SerializedProperty fpsProperty = _selectedProperty.FindPropertyRelative("FPS");
                SerializedProperty animationTypeProperty = _selectedProperty.FindPropertyRelative("SpriteAnimationType");

                if (fpsProperty == null || framesProperty == null || animationTypeProperty == null)
                    return;

                bool shouldRepaint = false;

                // If frames property array size has changed
                if (_previousFramesPropertySize != framesProperty.arraySize)
                {
                    // If current frame is greater than new frames array size, set it to the array size
                    if(_currentFrame >= framesProperty.arraySize && framesProperty.arraySize > 0)
                    {
                        _currentFrame = framesProperty.arraySize - 1;
                        _animationTime = _currentFrame;
                        _animationIsRunning = false;
                    }

                    _previousFramesPropertySize = framesProperty.arraySize;
                    shouldRepaint = true;
                }

                if (_animationIsRunning)
                {
                    int previousFrame = _currentFrame;
                    _animationTime += _deltaTime * fpsProperty.intValue;

                    var frameDuration = 1f / fpsProperty.intValue;
                    var animationDuration = frameDuration * (framesProperty.arraySize);

                    if (!_animationComplete && _animationTime >= (animationDuration * fpsProperty.intValue))
                    {
                        if (animationTypeProperty.intValue == (int)SpriteAnimationType.Looping)
                        {
                            _animationTime = 0f;
                            _currentFrame = 0;
                        }
                        else
                        {
                            _animationIsRunning = false;
                            _animationTime = _currentFrame;
                            _animationComplete = true;
                        }
                    }

                    _currentFrame = Mathf.Min((int)_animationTime, Mathf.Max(0, framesProperty.arraySize - 1));
                    if (previousFrame != _currentFrame)
                        shouldRepaint = true;
                }

                if (shouldRepaint)
                {
                    Repaint();
                }
                else if (EditorApplication.timeSinceStartup - _lastIdleRepaintTime > 0.15d)
                {
                    // Keep occasional redraws for editor responsiveness without forcing 60 FPS repaint.
                    _lastIdleRepaintTime = EditorApplication.timeSinceStartup;
                    Repaint();
                }
            }

            if (_animationIsRunning)
                EditorApplication.QueuePlayerLoopUpdate();
        }

        private void OnGUI()
        {
            if (_serializedObject == null)
                return;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            minSize = new Vector2(640, 480);
            _currentProperty = _serializedObject.FindProperty("SpriteAnimations");

            if (_currentProperty == null)
                return;

            EditorGUILayout.BeginHorizontal();

            // === List Items ===
            EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(_maxItemListColumnWidth), GUILayout.ExpandHeight(true));
            DrawSideBar(_currentProperty);
            EditorGUILayout.EndVertical();

            // === Show Object Details ===
            EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(int.MaxValue), GUILayout.ExpandHeight(true));
            if (_selectedProperty != null)
            {
                //DrawProperties(_selectedProperty, true);
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
                DrawSelectedPropertiesPanel();
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("Select an item from the list", labelStyle);
            }
            EditorGUILayout.EndVertical();

            // === Animation Preview ===
            if (_selectedProperty != null)
            {
                EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(_maxSpritePreviewColumnWidth), GUILayout.ExpandHeight(true));
                DrawAnimationPreview();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
            Apply();
        }

        private void DrawSelectedPropertiesPanel()
        {
            if (_selectedProperty == null)
                return;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            _currentProperty = _selectedProperty;


            // === Sprite Animation Data ===
            EditorGUILayout.LabelField("Sprite Animation Data", labelStyle);
            EditorGUILayout.BeginVertical("box");

            DrawField("Name", true);
            DrawField("FPS", true);
            DrawField("Frames", true);
            DrawField("SpriteAnimationType", true);
            DrawAttackDataSection();
            // and so on...

            EditorGUILayout.EndVertical();
            // ======================

            EditorGUILayout.Space(15, true);

            // === Sheet Generation ===
            EditorGUILayout.LabelField("Sheet Generation", labelStyle);
            EditorGUILayout.BeginVertical("box");
            _sheetGenerationTexture = (Texture2D)EditorGUILayout.ObjectField("Texture", _sheetGenerationTexture, typeof(Texture2D), false);
            if (_sheetGenerationTexture != null && GUILayout.Button("Generate Sheet"))
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(_sheetGenerationTexture));

                Sprite[] sprites = assets.OfType<Sprite>().ToArray();
                SerializedProperty framesProperty = _currentProperty.FindPropertyRelative("Frames");
                framesProperty.ClearArray();
                for (int i = 0; i < sprites.Length; i++)
                {
                    framesProperty.InsertArrayElementAtIndex(i);
                    framesProperty.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
                }

                _sheetGenerationTexture = null;
            }
            EditorGUILayout.EndVertical();
            // =========================
        }

        private void DrawAttackDataSection()
        {
            SerializedProperty attackDataProperty = _selectedProperty.FindPropertyRelative("AttackData");
            if (attackDataProperty == null)
                return;

            EditorGUILayout.Space(15, true);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("Attack Config", labelStyle);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.PropertyField(attackDataProperty);

            if (attackDataProperty.objectReferenceValue is ScriptableObject attackData)
            {
                DrawAttackDataInspector(attackData);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAttackDataInspector(ScriptableObject attackData)
        {
            SerializedObject attackDataSerializedObject = new SerializedObject(attackData);
            attackDataSerializedObject.Update();

            SerializedProperty scriptProperty = attackDataSerializedObject.FindProperty("m_Script");
            if (scriptProperty != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(scriptProperty, true);
                }
            }

            SerializedProperty hitboxProperty = attackDataSerializedObject.FindProperty("hitbox");
            SerializedProperty frameEventsProperty = attackDataSerializedObject.FindProperty("frameEvents");
            if (hitboxProperty == null || frameEventsProperty == null)
            {
                DrawGenericScriptableObjectInspector(attackDataSerializedObject);
                attackDataSerializedObject.ApplyModifiedProperties();
                return;
            }

            DrawAttackHitboxInspector(hitboxProperty);
            DrawFrameEventsInspector(frameEventsProperty);

            attackDataSerializedObject.ApplyModifiedProperties();
        }

        private void DrawAttackHitboxInspector(SerializedProperty hitboxProperty)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Hitbox", EditorStyles.boldLabel);

            SerializedProperty shapeProperty = hitboxProperty.FindPropertyRelative("shape");
            SerializedProperty offsetProperty = hitboxProperty.FindPropertyRelative("offset");
            SerializedProperty radiusProperty = hitboxProperty.FindPropertyRelative("radius");
            SerializedProperty sizeProperty = hitboxProperty.FindPropertyRelative("size");

            EditorGUILayout.PropertyField(shapeProperty);
            EditorGUILayout.PropertyField(offsetProperty);

            bool isCircle = shapeProperty != null && shapeProperty.enumValueIndex == 0;
            if (isCircle)
            {
                EditorGUILayout.PropertyField(radiusProperty);
            }
            else
            {
                EditorGUILayout.PropertyField(sizeProperty);
            }
        }

        private void DrawFrameEventsInspector(SerializedProperty frameEventsProperty)
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Frame Events", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Frame Event"))
            {
                frameEventsProperty.arraySize++;
            }

            for (int i = 0; i < frameEventsProperty.arraySize; i++)
            {
                SerializedProperty frameEventProperty = frameEventsProperty.GetArrayElementAtIndex(i);
                SerializedProperty eventTypeProperty = frameEventProperty.FindPropertyRelative("eventType");
                SerializedProperty startFrameProperty = frameEventProperty.FindPropertyRelative("startFrame");
                SerializedProperty endFrameProperty = frameEventProperty.FindPropertyRelative("endFrame");

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Event {i}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(72f)))
                {
                    frameEventsProperty.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(eventTypeProperty);
                EditorGUILayout.PropertyField(startFrameProperty);
                EditorGUILayout.PropertyField(endFrameProperty);
                DrawEventSpecificFields(frameEventProperty, eventTypeProperty);
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawEventSpecificFields(SerializedProperty frameEventProperty, SerializedProperty eventTypeProperty)
        {
            string eventTypeName = string.Empty;
            if (eventTypeProperty != null && eventTypeProperty.enumValueIndex >= 0 && eventTypeProperty.enumValueIndex < eventTypeProperty.enumNames.Length)
            {
                eventTypeName = eventTypeProperty.enumNames[eventTypeProperty.enumValueIndex];
            }

            switch (eventTypeName)
            {
                case "MoveForce":
                    EditorGUILayout.PropertyField(frameEventProperty.FindPropertyRelative("moveForce"));
                    EditorGUILayout.PropertyField(frameEventProperty.FindPropertyRelative("moveForceUsesFacing"));
                    EditorGUILayout.PropertyField(frameEventProperty.FindPropertyRelative("overrideHorizontalVelocity"));
                    break;
                case "SpawnVFX":
                    EditorGUILayout.PropertyField(frameEventProperty.FindPropertyRelative("vfxPrefab"));
                    EditorGUILayout.PropertyField(frameEventProperty.FindPropertyRelative("vfxOffset"));
                    break;
                case "PlaySound":
                    EditorGUILayout.PropertyField(frameEventProperty.FindPropertyRelative("sound"));
                    EditorGUILayout.PropertyField(frameEventProperty.FindPropertyRelative("soundVolume"));
                    break;
                case "CameraShake":
                    EditorGUILayout.PropertyField(frameEventProperty.FindPropertyRelative("cameraShakeStrength"));
                    EditorGUILayout.PropertyField(frameEventProperty.FindPropertyRelative("cameraShakeDuration"));
                    break;
            }
        }

        private void DrawGenericScriptableObjectInspector(SerializedObject serializedObject)
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                if (iterator.propertyPath != "m_Script")
                    EditorGUILayout.PropertyField(iterator, true);

                enterChildren = false;
            }
        }

        private void DrawAnimationPreview()
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            _currentProperty = _selectedProperty;
            EditorGUILayout.LabelField("Sprite Animation Preview", labelStyle);

            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            Rect previewRect = GUILayoutUtility.GetRect(260, 260, GUILayout.ExpandWidth(true));
            DrawTextureDisplay(previewRect);

            Rect labelRect = GUILayoutUtility.GetRect(new GUIContent($"Current Frame: {_currentFrame}"), labelStyle);
            EditorGUI.DropShadowLabel(labelRect, $"Current Frame: {_currentFrame}");

            DrawCurrentFrameSlider();
            DrawFrameTimeline();
            DrawDisplayButtons();
            EditorGUILayout.EndVertical();
        }

        private void DrawTextureDisplay(Rect previewRect)
        {
            SerializedProperty framesProperty = _currentProperty.FindPropertyRelative("Frames");
            EditorGUI.DrawTextureTransparent(previewRect, _transparentCheckboardTexture, ScaleMode.ScaleToFit);

            if (framesProperty != null && framesProperty.arraySize > 0 && _currentFrame < framesProperty.arraySize)
            {
                SerializedProperty framesPropertyElement = framesProperty.GetArrayElementAtIndex(_currentFrame);
                Sprite sprite = framesPropertyElement.objectReferenceValue as Sprite;

                if (sprite != null)
                {
                    Rect drawnRect = GUIDrawSprite(previewRect, sprite);
                    DrawActiveHitboxPreview(drawnRect, sprite);
                }
            }
        }

        private void DrawCurrentFrameSlider()
        {
            SerializedProperty framesProperty = _currentProperty.FindPropertyRelative("Frames");
            int frameCount = framesProperty != null ? framesProperty.arraySize : 0;

            using (new EditorGUI.DisabledScope(frameCount == 0))
            {
                EditorGUI.BeginChangeCheck();
                int newFrame = EditorGUILayout.IntSlider("Preview Frame", _currentFrame, 0, Mathf.Max(0, frameCount - 1));
                if (EditorGUI.EndChangeCheck())
                {
                    _animationIsRunning = false;
                    _currentFrame = newFrame;
                    _animationTime = _currentFrame;
                    _animationComplete = frameCount > 0 && _currentFrame == frameCount - 1;
                }
            }
        }

        private void DrawFrameTimeline()
        {
            SerializedProperty framesProperty = _currentProperty.FindPropertyRelative("Frames");
            int frameCount = framesProperty != null ? framesProperty.arraySize : 0;
            Rect rect = GUILayoutUtility.GetRect(280f, 112f, GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1f));

            if (frameCount <= 0)
            {
                EditorGUI.DropShadowLabel(rect, "No frames available");
                return;
            }

            List<PreviewFrameEventInfo> previewEvents = GetPreviewFrameEvents(frameCount);
            PreviewFrameEventType[] laneTypes = {
                PreviewFrameEventType.Hit,
                PreviewFrameEventType.UnlockMovement,
                PreviewFrameEventType.MoveForce,
                PreviewFrameEventType.SpawnVFX,
                PreviewFrameEventType.PlaySound,
                PreviewFrameEventType.CameraShake,
            };

            float labelWidth = 54f;
            float laneGap = 2f;
            float contentWidth = rect.width - labelWidth - 8f;
            float laneHeight = (rect.height - ((laneTypes.Length + 1) * laneGap) - 18f) / laneTypes.Length;
            Rect contentRect = new(rect.x + labelWidth, rect.y + laneGap, contentWidth, rect.height - 18f);

            for (int laneIndex = 0; laneIndex < laneTypes.Length; laneIndex++)
            {
                Rect laneRect = new(
                    contentRect.x,
                    contentRect.y + laneIndex * (laneHeight + laneGap),
                    contentRect.width,
                    laneHeight);

                Rect laneLabelRect = new(rect.x + 4f, laneRect.y, labelWidth - 8f, laneRect.height);
                EditorGUI.LabelField(laneLabelRect, GetEventShortLabel(laneTypes[laneIndex]));
                EditorGUI.DrawRect(laneRect, new Color(0.18f, 0.18f, 0.18f, 1f));

                foreach (PreviewFrameEventInfo previewEvent in previewEvents.Where(evt => evt.Type == laneTypes[laneIndex]))
                {
                    DrawTimelineEventRange(laneRect, frameCount, previewEvent, GetEventColor(previewEvent.Type));
                }

                DrawFrameDividers(laneRect, frameCount);
            }

            DrawCurrentFrameMarker(contentRect, frameCount);
            DrawTimelineFooter(rect, frameCount);
        }

        private void DrawTimelineEventRange(Rect laneRect, int frameCount, PreviewFrameEventInfo previewEvent, Color color)
        {
            float startNormalized = FrameToNormalizedPosition(previewEvent.StartFrame, frameCount);
            float endNormalized = FrameToNormalizedPosition(previewEvent.EndFrame + 1, frameCount);
            float minWidth = Mathf.Max(4f, laneRect.width / Mathf.Max(frameCount, 1));
            float xMin = laneRect.x + laneRect.width * startNormalized;
            float xMax = laneRect.x + laneRect.width * endNormalized;
            Rect eventRect = new(xMin, laneRect.y + 1f, Mathf.Max(minWidth, xMax - xMin), laneRect.height - 2f);
            EditorGUI.DrawRect(eventRect, color);
        }

        private void DrawFrameDividers(Rect rect, int frameCount)
        {
            if (frameCount <= 1)
                return;

            Color dividerColor = new(1f, 1f, 1f, 0.08f);
            for (int frame = 1; frame < frameCount; frame++)
            {
                float x = rect.x + rect.width * FrameToNormalizedPosition(frame, frameCount);
                EditorGUI.DrawRect(new Rect(x, rect.y, 1f, rect.height), dividerColor);
            }
        }

        private void DrawCurrentFrameMarker(Rect rect, int frameCount)
        {
            float normalized = FrameToNormalizedPosition(_currentFrame, frameCount);
            float x = rect.x + rect.width * normalized;
            EditorGUI.DrawRect(new Rect(x, rect.y, 2f, rect.height), new Color(1f, 1f, 1f, 0.9f));
        }

        private void DrawTimelineFooter(Rect rect, int frameCount)
        {
            Rect footerRect = new(rect.x + 4f, rect.yMax - 16f, rect.width - 8f, 14f);
            EditorGUI.LabelField(footerRect, $"Frames 0-{Mathf.Max(0, frameCount - 1)}");
        }

        private float FrameToNormalizedPosition(int frame, int frameCount)
        {
            if (frameCount <= 0)
                return 0f;

            return Mathf.Clamp01((float)frame / frameCount);
        }

        private void DrawTimeScaleSlider()
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("Playback Speed", labelStyle);
            _timeScale = EditorGUILayout.Slider(_timeScale, 0f, 2f);
        }

        private void DrawDisplayButtons()
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(_buttonPreviousFrameTexture, GUILayout.MinWidth(20), GUILayout.MinHeight(20), GUILayout.MaxWidth(20), GUILayout.MaxHeight(20)))
            {
                SerializedProperty framesProperty = _currentProperty.FindPropertyRelative("Frames");

                _animationIsRunning = false;
                _currentFrame--;

                if (_currentFrame < 0)
                    _currentFrame = framesProperty.arraySize > 0 ? framesProperty.arraySize - 1 : 0;

                _animationTime = _currentFrame;
                _animationComplete = _currentFrame == framesProperty.arraySize - 1;
            }

            if (GUILayout.Button(CurrentPauseButtonLabel, GUILayout.MinWidth(20), GUILayout.MinHeight(20), GUILayout.MaxWidth(20), GUILayout.MaxHeight(20)))
            {
                _animationIsRunning = !_animationIsRunning;

                if (_animationComplete)
                {
                    _animationTime = 0f;
                    _animationComplete = false;
                }

                _timeScale = _animationIsRunning ? 1f : 0f;
            }

            if (GUILayout.Button(_buttonNextFrameTexture, GUILayout.MinWidth(20), GUILayout.MinHeight(20), GUILayout.MaxWidth(20), GUILayout.MaxHeight(20)))
            {
                SerializedProperty framesProperty = _currentProperty.FindPropertyRelative("Frames");

                _animationIsRunning = false;
                _currentFrame++;

                if (_currentFrame > framesProperty.arraySize - 1)
                    _currentFrame = 0;

                _animationTime = _currentFrame;
                _animationComplete = _currentFrame == framesProperty.arraySize - 1;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        public Rect GUIDrawSprite(Rect previewRect, Sprite sprite)
        {
            // Get the sprite's original rect and texture
            Rect spriteRect = sprite.rect;
            Texture2D texture = SpriteUtility.GetSpriteTexture(sprite, false);

            // Calculate the aspect ratio of the sprite
            float spriteAspectRatio = spriteRect.width / spriteRect.height;

            // Calculate the maximum integer scaling factor that fits within the preview rect
            int scaleFactor = Mathf.FloorToInt(Mathf.Min(previewRect.width / spriteRect.width, previewRect.height / spriteRect.height));

            // If scaleFactor is greater than 0, use pixel-perfect scaling
            if (scaleFactor > 0)
            {
                // Calculate the new size of the sprite based on the scale factor
                float displayWidth = scaleFactor * spriteRect.width;
                float displayHeight = scaleFactor * spriteRect.height;

                // Center the scaled sprite within the preview rect
                Rect fitRect = new Rect(
                    previewRect.x + (previewRect.width - displayWidth) / 2f,
                    previewRect.y + (previewRect.height - displayHeight) / 2f,
                    displayWidth,
                    displayHeight
                );

                // Ensure the rect aligns with the pixel grid to avoid cutting off part of the sprite
                fitRect.x = Mathf.Floor(fitRect.x);
                fitRect.y = Mathf.Floor(fitRect.y);
                fitRect.width = Mathf.Floor(fitRect.width);
                fitRect.height = Mathf.Floor(fitRect.height);

                // Draw the sprite's texture using the calculated scaling
                GUI.DrawTextureWithTexCoords(fitRect, texture, new Rect(
                    spriteRect.x / texture.width,
                    spriteRect.y / texture.height,
                    spriteRect.width / texture.width,
                    spriteRect.height / texture.height));

                return fitRect;
            }
            else
            {
                // Fallback to aspect ratio scaling (non-pixel-perfect) if scaleFactor is 0

                Rect fitRect = previewRect;

                if (spriteAspectRatio > 1) // Sprite is wider than tall
                {
                    float height = previewRect.width / spriteAspectRatio;
                    fitRect = new Rect(previewRect.x, previewRect.y + (previewRect.height - height) / 2f, previewRect.width, height);
                }
                else // Sprite is taller than wide or square
                {
                    float width = previewRect.height * spriteAspectRatio;
                    fitRect = new Rect(previewRect.x + (previewRect.width - width) / 2f, previewRect.y, width, previewRect.height);
                }

                // Draw the sprite's texture within the calculated rect while keeping its aspect ratio
                GUI.DrawTextureWithTexCoords(fitRect, texture, new Rect(
                    spriteRect.x / texture.width,
                    spriteRect.y / texture.height,
                    spriteRect.width / texture.width,
                    spriteRect.height / texture.height));

                return fitRect;
            }
        }

        private void DrawActiveHitboxPreview(Rect spriteRect, Sprite sprite)
        {
            PreviewHitboxInfo hitbox = GetPreviewHitbox();
            if (!hitbox.IsValid || !IsHitFrameActive())
                return;

            float unitsToGuiScale = sprite.bounds.size.x > 0f
                ? spriteRect.width / sprite.bounds.size.x
                : sprite.pixelsPerUnit;

            Vector2 center = new(
                spriteRect.center.x + hitbox.Offset.x * unitsToGuiScale,
                spriteRect.center.y - hitbox.Offset.y * unitsToGuiScale);

            Handles.BeginGUI();
            Handles.color = new Color(1f, 0.3f, 0.3f, 0.95f);

            if (hitbox.IsCircle)
            {
                float radius = hitbox.Radius * unitsToGuiScale;
                Handles.DrawSolidDisc(center, Vector3.forward, radius);
                Handles.color = new Color(1f, 0.8f, 0.8f, 1f);
                Handles.DrawWireDisc(center, Vector3.forward, radius);
            }
            else
            {
                Vector2 size = new(hitbox.Size.x * unitsToGuiScale, hitbox.Size.y * unitsToGuiScale);
                Rect boxRect = new(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
                EditorGUI.DrawRect(boxRect, new Color(1f, 0.3f, 0.3f, 0.22f));
                Handles.color = new Color(1f, 0.8f, 0.8f, 1f);
                Handles.DrawWireCube(center, size);
            }

            Handles.EndGUI();
        }

        private bool IsHitFrameActive()
        {
            List<PreviewFrameEventInfo> previewEvents = GetPreviewFrameEvents(int.MaxValue);
            return previewEvents.Any(evt => evt.Type == PreviewFrameEventType.Hit && _currentFrame >= evt.StartFrame && _currentFrame <= evt.EndFrame);
        }

        private List<PreviewFrameEventInfo> GetPreviewFrameEvents(int frameCount)
        {
            var result = new List<PreviewFrameEventInfo>();
            ScriptableObject attackData = GetSelectedAttackDataObject();
            if (attackData == null)
                return result;

            SerializedObject attackDataSerializedObject = new SerializedObject(attackData);
            SerializedProperty frameEventsProperty = attackDataSerializedObject.FindProperty("frameEvents");
            if (frameEventsProperty == null || !frameEventsProperty.isArray)
                return result;

            for (int i = 0; i < frameEventsProperty.arraySize; i++)
            {
                SerializedProperty frameEventProperty = frameEventsProperty.GetArrayElementAtIndex(i);
                SerializedProperty eventTypeProperty = frameEventProperty.FindPropertyRelative("eventType");
                SerializedProperty startFrameProperty = frameEventProperty.FindPropertyRelative("startFrame");
                SerializedProperty endFrameProperty = frameEventProperty.FindPropertyRelative("endFrame");
                if (eventTypeProperty == null || startFrameProperty == null || endFrameProperty == null)
                    continue;

                int startFrame = Mathf.Max(0, startFrameProperty.intValue);
                int endFrame = Mathf.Max(startFrame, endFrameProperty.intValue);
                if (frameCount != int.MaxValue && frameCount > 0)
                {
                    startFrame = Mathf.Clamp(startFrame, 0, frameCount - 1);
                    endFrame = Mathf.Clamp(endFrame, startFrame, frameCount - 1);
                }

                string[] enumNames = eventTypeProperty.enumNames;
                string enumName = eventTypeProperty.enumValueIndex >= 0 && eventTypeProperty.enumValueIndex < enumNames.Length
                    ? enumNames[eventTypeProperty.enumValueIndex]
                    : string.Empty;

                PreviewFrameEventType previewType = ParsePreviewFrameEventType(enumName, eventTypeProperty.intValue);
                result.Add(new PreviewFrameEventInfo {
                    Type = previewType,
                    StartFrame = startFrame,
                    EndFrame = endFrame,
                    Label = enumName,
                });
            }

            return result;
        }

        private PreviewHitboxInfo GetPreviewHitbox()
        {
            ScriptableObject attackData = GetSelectedAttackDataObject();
            if (attackData == null)
                return default;

            SerializedObject attackDataSerializedObject = new SerializedObject(attackData);
            SerializedProperty hitboxProperty = attackDataSerializedObject.FindProperty("hitbox");
            if (hitboxProperty == null)
                return default;

            SerializedProperty shapeProperty = hitboxProperty.FindPropertyRelative("shape");
            SerializedProperty offsetProperty = hitboxProperty.FindPropertyRelative("offset");
            SerializedProperty radiusProperty = hitboxProperty.FindPropertyRelative("radius");
            SerializedProperty sizeProperty = hitboxProperty.FindPropertyRelative("size");
            if (shapeProperty == null || offsetProperty == null || radiusProperty == null || sizeProperty == null)
                return default;

            return new PreviewHitboxInfo {
                IsValid = true,
                IsCircle = shapeProperty.enumValueIndex == 0,
                Offset = offsetProperty.vector2Value,
                Radius = Mathf.Max(0.01f, radiusProperty.floatValue),
                Size = sizeProperty.vector2Value,
            };
        }

        private ScriptableObject GetSelectedAttackDataObject()
        {
            SerializedProperty attackDataProperty = _selectedProperty.FindPropertyRelative("AttackData");
            return attackDataProperty != null ? attackDataProperty.objectReferenceValue as ScriptableObject : null;
        }

        private PreviewFrameEventType ParsePreviewFrameEventType(string enumName, int enumRawValue)
        {
            string normalizedEnumName = string.IsNullOrEmpty(enumName)
                ? string.Empty
                : enumName.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();

            return normalizedEnumName switch
            {
                "hit" => PreviewFrameEventType.Hit,
                "unlockmovement" => PreviewFrameEventType.UnlockMovement,
                "moveforce" => PreviewFrameEventType.MoveForce,
                "spawnvfx" => PreviewFrameEventType.SpawnVFX,
                "playsound" => PreviewFrameEventType.PlaySound,
                "camerashake" => PreviewFrameEventType.CameraShake,
                _ => ParsePreviewFrameEventTypeFromRawValue(enumRawValue),
            };
        }

        private PreviewFrameEventType ParsePreviewFrameEventTypeFromRawValue(int enumRawValue)
        {
            return enumRawValue switch
            {
                0 => PreviewFrameEventType.Hit,
                1 => PreviewFrameEventType.UnlockMovement,
                2 => PreviewFrameEventType.MoveForce,
                3 => PreviewFrameEventType.SpawnVFX,
                4 => PreviewFrameEventType.PlaySound,
                5 => PreviewFrameEventType.CameraShake,
                _ => PreviewFrameEventType.Unknown,
            };
        }

        private Color GetEventColor(PreviewFrameEventType type)
        {
            return type switch
            {
                PreviewFrameEventType.Hit => new Color(0.92f, 0.28f, 0.28f, 0.55f),
                PreviewFrameEventType.UnlockMovement => new Color(0.96f, 0.72f, 0.25f, 0.55f),
                PreviewFrameEventType.MoveForce => new Color(1f, 0.5f, 0.12f, 0.58f),
                PreviewFrameEventType.SpawnVFX => new Color(0.34f, 0.76f, 0.98f, 0.55f),
                PreviewFrameEventType.PlaySound => new Color(0.44f, 0.86f, 0.48f, 0.55f),
                PreviewFrameEventType.CameraShake => new Color(0.78f, 0.48f, 0.96f, 0.55f),
                _ => new Color(0.7f, 0.7f, 0.7f, 0.4f),
            };
        }

        private string GetEventShortLabel(PreviewFrameEventType type)
        {
            return type switch
            {
                PreviewFrameEventType.Hit => "Hit",
                PreviewFrameEventType.UnlockMovement => "Unlock",
                PreviewFrameEventType.MoveForce => "Force",
                PreviewFrameEventType.SpawnVFX => "VFX",
                PreviewFrameEventType.PlaySound => "SFX",
                PreviewFrameEventType.CameraShake => "Shake",
                _ => "Other",
            };
        }

        protected override void OnElementClicked()
        {
            _currentFrame = 0;
            _animationTime = 0f;
            _animationIsRunning = true;
            _animationComplete = false;
            _timeScale = 1f;
        }
    }
}
#endif
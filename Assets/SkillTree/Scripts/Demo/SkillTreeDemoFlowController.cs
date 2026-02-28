using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SkillTree.Demo
{
    [DefaultExecutionOrder(-900)]
    public sealed class SkillTreeDemoFlowController : MonoBehaviour
    {
        private const string FlowObjectName = "SkillTreeDemoFlowController";
        private const string GameplayShellName = "MockGameplayShell";
        private const string HintLabelName = "ToggleHintLabel";

        [SerializeField] private KeyCode _toggleKey = KeyCode.K;
        [SerializeField] private KeyCode _closeKey = KeyCode.Escape;
#if ENABLE_INPUT_SYSTEM
        [SerializeField] private Key _toggleKeyInputSystem = Key.K;
        [SerializeField] private Key _closeKeyInputSystem = Key.Escape;
#endif
        [SerializeField] private bool _pauseWhenSkillTreeOpen;
        [SerializeField] private bool _hideCursorWhenSkillTreeClosed = true;
        [SerializeField] private string _openHintText = "Press K to open Skill Tree";

        private SkillTreeCanvasController _skillTreeController;
        private RectTransform _skillTreeRoot;
        private CanvasGroup _skillTreeCanvasGroup;
        private GameObject _gameplayShell;
        private TextMeshProUGUI _hintLabel;
        private bool _isSkillTreeOpen;
        private bool _initialized;
        private float _timeScaleBeforePause = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<SkillTreeDemoFlowController>() != null)
                return;

            SkillTreeCanvasController skillTree = FindFirstObjectByType<SkillTreeCanvasController>();
            if (skillTree == null)
                return;

            GameObject flowObject = new(FlowObjectName);
            flowObject.AddComponent<SkillTreeDemoFlowController>();
        }

        private void Awake()
        {
            TryInitialize();
        }

        private void Update()
        {
            if (!_initialized && !TryInitialize())
                return;

            if (WasTogglePressed())
            {
                SetSkillTreeVisibility(!_isSkillTreeOpen);
                return;
            }

            if (_isSkillTreeOpen && WasClosePressed())
                SetSkillTreeVisibility(false);
        }

        private void OnDestroy()
        {
            if (_pauseWhenSkillTreeOpen && _isSkillTreeOpen)
                Time.timeScale = _timeScaleBeforePause;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private bool TryInitialize()
        {
            ResolveReferences();
            if (_skillTreeRoot == null)
                return false;

            EnsureCanvasGroup();
            EnsureGameplayShell();
            SetSkillTreeVisibility(false, force: true);
            _initialized = true;
            return true;
        }

        private void ResolveReferences()
        {
            if (_skillTreeController == null)
                _skillTreeController = FindFirstObjectByType<SkillTreeCanvasController>();

            if (_skillTreeRoot == null && _skillTreeController != null)
                _skillTreeRoot = _skillTreeController.transform as RectTransform;
        }

        private void EnsureCanvasGroup()
        {
            if (_skillTreeRoot == null)
                return;

            if (_skillTreeCanvasGroup == null)
                _skillTreeCanvasGroup = _skillTreeRoot.GetComponent<CanvasGroup>();

            if (_skillTreeCanvasGroup == null)
                _skillTreeCanvasGroup = _skillTreeRoot.gameObject.AddComponent<CanvasGroup>();
        }

        private void EnsureGameplayShell()
        {
            if (_gameplayShell != null)
                return;

            if (_skillTreeRoot == null)
                return;

            RectTransform canvasRoot = _skillTreeRoot.parent as RectTransform;
            if (canvasRoot == null)
                return;

            Transform existing = canvasRoot.Find(GameplayShellName);
            if (existing != null)
            {
                _gameplayShell = existing.gameObject;
                _hintLabel = FindHintLabel(existing);
                return;
            }

            RectTransform shellRoot = CreateRect(GameplayShellName, canvasRoot);
            Stretch(shellRoot, Vector2.zero, Vector2.zero);
            _gameplayShell = shellRoot.gameObject;

            int skillTreeIndex = _skillTreeRoot.GetSiblingIndex();
            shellRoot.SetSiblingIndex(skillTreeIndex);

            Image background = shellRoot.gameObject.AddComponent<Image>();
            background.color = new Color(0.07f, 0.09f, 0.12f, 1f);
            background.raycastTarget = false;

            RectTransform skyline = CreateRect("Skyline", shellRoot);
            Stretch(skyline, Vector2.zero, Vector2.zero);
            Image skylineImage = skyline.gameObject.AddComponent<Image>();
            skylineImage.color = new Color(0.15f, 0.2f, 0.3f, 0.45f);
            skylineImage.raycastTarget = false;

            RectTransform topBar = CreateRect("TopBar", shellRoot);
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.offsetMin = new Vector2(0f, -92f);
            topBar.offsetMax = new Vector2(0f, 0f);
            Image topBarImage = topBar.gameObject.AddComponent<Image>();
            topBarImage.color = new Color(0.03f, 0.05f, 0.08f, 0.92f);
            topBarImage.raycastTarget = false;

            RectTransform titleRect = CreateRect("Title", topBar);
            Stretch(titleRect, new Vector2(24f, 18f), new Vector2(-24f, -18f));
            TextMeshProUGUI title = CreateLabel(titleRect, "Template Gameplay Screen", 38, FontStyles.Bold);
            title.alignment = TextAlignmentOptions.Left;

            RectTransform statsRect = CreateRect("StatsPanel", shellRoot);
            statsRect.anchorMin = new Vector2(0f, 1f);
            statsRect.anchorMax = new Vector2(0f, 1f);
            statsRect.pivot = new Vector2(0f, 1f);
            statsRect.anchoredPosition = new Vector2(36f, -130f);
            statsRect.sizeDelta = new Vector2(360f, 190f);
            Image statsImage = statsRect.gameObject.AddComponent<Image>();
            statsImage.color = new Color(0.03f, 0.05f, 0.08f, 0.68f);
            statsImage.raycastTarget = false;

            RectTransform statsTextRect = CreateRect("StatsText", statsRect);
            Stretch(statsTextRect, new Vector2(18f, 18f), new Vector2(-18f, -18f));
            TextMeshProUGUI statsText = CreateLabel(
                statsTextRect,
                "Class: Arcane Initiate\nHP: 340 / 340\nMana: 180 / 180\nObjective: Grow your build in Skill Tree",
                24,
                FontStyles.Normal);
            statsText.alignment = TextAlignmentOptions.TopLeft;
            statsText.lineSpacing = 8f;

            RectTransform heroCard = CreateRect("HeroCard", shellRoot);
            heroCard.anchorMin = new Vector2(0.5f, 0.5f);
            heroCard.anchorMax = new Vector2(0.5f, 0.5f);
            heroCard.pivot = new Vector2(0.5f, 0.5f);
            heroCard.anchoredPosition = new Vector2(0f, -20f);
            heroCard.sizeDelta = new Vector2(420f, 260f);
            Image heroCardImage = heroCard.gameObject.AddComponent<Image>();
            heroCardImage.color = new Color(0.04f, 0.06f, 0.09f, 0.75f);
            heroCardImage.raycastTarget = false;

            RectTransform heroTitleRect = CreateRect("HeroTitle", heroCard);
            heroTitleRect.anchorMin = new Vector2(0f, 1f);
            heroTitleRect.anchorMax = new Vector2(1f, 1f);
            heroTitleRect.pivot = new Vector2(0.5f, 1f);
            heroTitleRect.offsetMin = new Vector2(0f, -68f);
            heroTitleRect.offsetMax = new Vector2(0f, -16f);
            TextMeshProUGUI heroTitle = CreateLabel(heroTitleRect, "Mage Trainee", 42, FontStyles.Bold);
            heroTitle.alignment = TextAlignmentOptions.Center;

            RectTransform heroStateRect = CreateRect("HeroState", heroCard);
            Stretch(heroStateRect, new Vector2(20f, 24f), new Vector2(-20f, -28f));
            TextMeshProUGUI heroState = CreateLabel(
                heroStateRect,
                "Status: Idle\nLocation: Training Grounds",
                26,
                FontStyles.Normal);
            heroState.alignment = TextAlignmentOptions.Center;
            heroState.lineSpacing = 8f;

            RectTransform hintRect = CreateRect(HintLabelName, shellRoot);
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 36f);
            hintRect.sizeDelta = new Vector2(760f, 60f);
            _hintLabel = CreateLabel(hintRect, _openHintText, 30, FontStyles.Bold);
            _hintLabel.alignment = TextAlignmentOptions.Center;
        }

        private void SetSkillTreeVisibility(bool visible, bool force = false)
        {
            if (!force && _isSkillTreeOpen == visible)
                return;

            _isSkillTreeOpen = visible;

            if (_skillTreeCanvasGroup != null)
            {
                _skillTreeCanvasGroup.alpha = visible ? 1f : 0f;
                _skillTreeCanvasGroup.interactable = visible;
                _skillTreeCanvasGroup.blocksRaycasts = visible;
            }
            else if (_skillTreeRoot != null)
            {
                _skillTreeRoot.gameObject.SetActive(visible);
            }

            if (_gameplayShell != null)
                _gameplayShell.SetActive(!visible);

            if (_pauseWhenSkillTreeOpen)
            {
                if (visible)
                {
                    _timeScaleBeforePause = Time.timeScale;
                    Time.timeScale = 0f;
                }
                else
                {
                    Time.timeScale = Mathf.Approximately(_timeScaleBeforePause, 0f) ? 1f : _timeScaleBeforePause;
                }
            }

            if (_hintLabel != null)
                _hintLabel.text = _openHintText;

            Cursor.visible = visible || !_hideCursorWhenSkillTreeClosed;
            Cursor.lockState = CursorLockMode.None;
        }

        private bool WasTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_toggleKeyInputSystem].wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(_toggleKey);
#else
            return false;
#endif
        }

        private bool WasClosePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_closeKeyInputSystem].wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(_closeKey);
#else
            return false;
#endif
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return rectTransform;
        }

        private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private static TextMeshProUGUI CreateLabel(
            RectTransform rectTransform,
            string text,
            float fontSize,
            FontStyles style)
        {
            TextMeshProUGUI label = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = new Color(0.94f, 0.96f, 1f, 1f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;

            return label;
        }

        private static TextMeshProUGUI FindHintLabel(Transform root)
        {
            Transform hintTransform = root.Find(HintLabelName);
            if (hintTransform == null)
                return null;

            return hintTransform.GetComponent<TextMeshProUGUI>();
        }
    }
}

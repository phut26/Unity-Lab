using SkillTree.Core;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace SkillTree.Demo
{
    public sealed class SkillTreeDebugController : MonoBehaviour
    {
        [SerializeField] private SkillTreeBehaviour _skillTreeBehaviour;
        [SerializeField] private StatSystem _statSystem;

        private SkillTreeService _service;
        private WalletContext _wallet;
        private bool _initialized;

        private static readonly string[] DemoStatIds =
        {
            "mana_regen",
            "spell_power",
            "mana_cost_ratio",
            "aoe_damage",
            "shield_value",
            "spell_crit"
        };

        private void Awake()
        {
            AutoResolveReferences();
        }

        private void Start()
        {
            TryInitialize();
        }

        private void Update()
        {
            if (!_initialized)
                return;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (WasPressed(keyboard.digit1Key, keyboard.numpad1Key)) TryUpgrade("core_attunement");
            if (WasPressed(keyboard.digit2Key, keyboard.numpad2Key)) TryUpgrade("ember_bolt");
            if (WasPressed(keyboard.digit3Key, keyboard.numpad3Key)) TryUpgrade("arcane_efficiency");
            if (WasPressed(keyboard.digit4Key, keyboard.numpad4Key)) TryUpgrade("flame_wave");
            if (WasPressed(keyboard.digit5Key, keyboard.numpad5Key)) TryUpgrade("mana_barrier");
            if (WasPressed(keyboard.digit6Key, keyboard.numpad6Key)) TryUpgrade("phoenix_core");

            if (WasPressed(keyboard.rKey))
            {
                _service.ResetProgression();
                Debug.Log("[SkillTreeDemo] Progression reset.");
                LogStatsSnapshot();
            }

            if (WasPressed(keyboard.gKey))
            {
                _wallet.Add("gold", 100);
                LogWallet();
            }

            if (WasPressed(keyboard.eKey))
            {
                _wallet.Add("essence", 5);
                LogWallet();
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha1)) TryUpgrade("core_attunement");
            if (Input.GetKeyDown(KeyCode.Alpha2)) TryUpgrade("ember_bolt");
            if (Input.GetKeyDown(KeyCode.Alpha3)) TryUpgrade("arcane_efficiency");
            if (Input.GetKeyDown(KeyCode.Alpha4)) TryUpgrade("flame_wave");
            if (Input.GetKeyDown(KeyCode.Alpha5)) TryUpgrade("mana_barrier");
            if (Input.GetKeyDown(KeyCode.Alpha6)) TryUpgrade("phoenix_core");

            if (Input.GetKeyDown(KeyCode.R))
            {
                _service.ResetProgression();
                Debug.Log("[SkillTreeDemo] Progression reset.");
                LogStatsSnapshot();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                _wallet.Add("gold", 100);
                LogWallet();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                _wallet.Add("essence", 5);
                LogWallet();
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static bool WasPressed(KeyControl primary, KeyControl secondary = null)
        {
            if (primary != null && primary.wasPressedThisFrame)
                return true;

            return secondary != null && secondary.wasPressedThisFrame;
        }
#endif

        private void OnDestroy()
        {
            if (_statSystem != null)
                _statSystem.OnStatChanged -= HandleStatChanged;
        }

        private void AutoResolveReferences()
        {
            if (_skillTreeBehaviour == null)
                _skillTreeBehaviour = FindFirstObjectByType<SkillTreeBehaviour>();

            if (_statSystem == null)
                _statSystem = FindFirstObjectByType<StatSystem>();
        }

        private void TryInitialize()
        {
            if (_initialized)
                return;

            AutoResolveReferences();
            if (_skillTreeBehaviour == null || _statSystem == null)
            {
                Debug.LogWarning("[SkillTreeDemo] Missing references. Cannot initialize debug controller.");
                return;
            }

            _service = _skillTreeBehaviour.Service;
            _wallet = _skillTreeBehaviour.Wallet;
            if (_service == null || _wallet == null)
            {
                Debug.LogWarning("[SkillTreeDemo] SkillTreeSession not ready.");
                return;
            }

            _statSystem.OnStatChanged += HandleStatChanged;
            _initialized = true;

            Debug.Log("[SkillTreeDemo] Ready. Keys: 1..6 upgrade, R reset, G +100 gold, E +5 essence.");
            LogWallet();
            LogStatsSnapshot();
        }

        private void TryUpgrade(string skillId)
        {
            SkillUpgradeResult result = _service.TryUpgrade(skillId, _wallet);
            int level = _service.GetSkillById(skillId).Level;
            Debug.Log($"[SkillTreeDemo] Upgrade '{skillId}' => {result}. Level={level}");
            LogWallet();
            LogStatsSnapshot();
        }

        private void HandleStatChanged(string statId, float value)
        {
            Debug.Log($"[SkillTreeDemo] Stat changed: {statId} = {value:0.###}");
        }

        private void LogWallet()
        {
            Debug.Log($"[SkillTreeDemo] Wallet: gold={_wallet.GetBalance("gold")}, essence={_wallet.GetBalance("essence")}");
        }

        private void LogStatsSnapshot()
        {
            foreach (string statId in DemoStatIds)
            {
                float value = _statSystem.GetFinalValue(statId);
                Debug.Log($"[SkillTreeDemo] {statId} => {value:0.###}");
            }
        }
    }
}

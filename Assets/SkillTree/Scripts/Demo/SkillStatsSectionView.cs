using System;
using System.Globalization;
using System.Text;
using SkillTree.Core;
using TMPro;
using UnityEngine;

namespace SkillTree.Demo
{
    internal sealed class SkillStatsSectionView
    {
        private readonly TextMeshProUGUI _titleText;
        private readonly TextMeshProUGUI _bodyText;
        private readonly StatSystem _statSystem;
        private readonly string _emptyTitle;
        private readonly string _emptyBody;
        private readonly string _noStatEffectsText;

        public SkillStatsSectionView(
            TextMeshProUGUI titleText,
            TextMeshProUGUI bodyText,
            StatSystem statSystem,
            string emptyTitle,
            string emptyBody,
            string noStatEffectsText)
        {
            _titleText = titleText;
            _bodyText = bodyText;
            _statSystem = statSystem;
            _emptyTitle = emptyTitle;
            _emptyBody = emptyBody;
            _noStatEffectsText = noStatEffectsText;
        }

        public void Bind(SkillNodeViewModel node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            if (_titleText != null)
                _titleText.text = "Stats";

            if (_bodyText != null)
                _bodyText.text = BuildStatsText(node);
        }

        public void Clear()
        {
            if (_titleText != null)
                _titleText.text = _emptyTitle;

            if (_bodyText != null)
                _bodyText.text = _emptyBody;
        }

        private string BuildStatsText(SkillNodeViewModel node)
        {
            bool hasStatEffect = false;
            StringBuilder builder = new();

            foreach (EffectDefinition effect in node.Skill.EffectDefinitions)
            {
                if (!string.Equals(effect.TargetType, "Stat", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrWhiteSpace(effect.StatId))
                    continue;

                hasStatEffect = true;
                string statId = effect.StatId.Trim();
                string statDisplayName = ResolveStatDisplayName(statId);
                string statDescription = ResolveStatDescription(statId);
                string currentValue = node.Level > 0
                    ? FormatModifier(effect.Operation, ResolveValueByLevel(effect.Operation, effect.Value, node.Level))
                    : "Inactive";

                builder.Append(statDisplayName);
                builder.AppendLine();

                if (!string.IsNullOrWhiteSpace(statDescription))
                {
                    builder.Append(statDescription.Trim());
                    builder.AppendLine();
                }

                builder.Append("Current: ");
                builder.Append(currentValue);
                builder.AppendLine();

                if (!node.IsMaxed)
                {
                    int nextLevel = Mathf.Min(node.Level + 1, node.MaxLevel);
                    string nextValue = FormatModifier(
                        effect.Operation,
                        ResolveValueByLevel(effect.Operation, effect.Value, nextLevel));

                    builder.Append("Next (Lv ");
                    builder.Append(nextLevel);
                    builder.Append("): ");
                    builder.Append(nextValue);
                    builder.AppendLine();
                }
                else
                {
                    builder.Append("Next: Max level reached");
                    builder.AppendLine();
                }

                builder.AppendLine();
            }

            if (!hasStatEffect)
                return _noStatEffectsText;

            return builder.ToString().TrimEnd();
        }

        private string ResolveStatDisplayName(string statId)
        {
            if (_statSystem != null && _statSystem.TryGetDefinition(statId, out StatDefinition definition))
            {
                if (!string.IsNullOrWhiteSpace(definition.DisplayName))
                    return definition.DisplayName.Trim();
            }

            return HumanizeStatId(statId);
        }

        private string ResolveStatDescription(string statId)
        {
            if (_statSystem != null && _statSystem.TryGetDefinition(statId, out StatDefinition definition))
            {
                if (!string.IsNullOrWhiteSpace(definition.Description))
                    return definition.Description.Trim();
            }

            return string.Empty;
        }

        private static float ResolveValueByLevel(ModifierOperation operation, float baseValue, int level)
        {
            if (level <= 0) return 0f;

            return operation switch
            {
                ModifierOperation.Add => baseValue * level,
                ModifierOperation.Subtract => baseValue * level,
                ModifierOperation.Multiply => Mathf.Pow(baseValue, level),
                ModifierOperation.Divide => Mathf.Pow(baseValue, level),
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }

        private static string FormatModifier(ModifierOperation operation, float value)
        {
            string valueText = value.ToString("0.##");

            return operation switch
            {
                ModifierOperation.Add => $"+{valueText}",
                ModifierOperation.Subtract => $"-{valueText}",
                ModifierOperation.Multiply => $"x{valueText}",
                ModifierOperation.Divide => $"/{valueText}",
                _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            };
        }

        private static string HumanizeStatId(string statId)
        {
            if (string.IsNullOrWhiteSpace(statId))
                return "Unknown Stat";

            string[] parts = statId.Trim().Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return "Unknown Stat";

            TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
            for (int i = 0; i < parts.Length; i++)
                parts[i] = textInfo.ToTitleCase(parts[i].ToLowerInvariant());

            return string.Join(" ", parts);
        }
    }
}

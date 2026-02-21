# Skill Tree System – Architecture Design Memo

## 1. 🎯 Project Goal

Design and implement a **generic, reusable, data-driven Skill Tree system for Unity** that:

- Is decoupled from game-specific logic (no direct dependency on Player, Combat, Stats, etc.)
- Supports unlock + upgrade progression
- Supports multiple cost types
- Emits effects without applying gameplay logic directly
- Can be integrated into different games with minimal modification
- Is suitable for portfolio demonstration as a reusable framework

---

## 2. 🧠 High-Level Design Philosophy

### Core Principles

1. Skill system handles **progression only**
2. Game systems handle **gameplay effects**
3. Skill system emits **descriptive data**, not direct actions
4. Dependency direction:
    - Game depends on SkillTree
    - SkillTree does NOT depend on Game

---

## 3. 🏗 Layered Architecture

### 3.1 Generic Layer (Framework)

Contains:

- `SkillTree`
- `Skill`
- `SkillSO`
- `EffectDefinition`
- `CostDefinition`
- `ISkillContext`

This layer:

- Contains no reference to Player
- Contains no reference to Stats
- Contains no reference to Combat
- Emits events only

---

### 3.2 Game Layer (Game-Specific)

Contains:

- `StatSystem`
- `CombatSystem`
- `CurrencySystem`
- `InventorySystem`
- `SkillEffectAdapter`

This layer:

- Subscribes to skill events
- Applies effects appropriately
- Implements cost providers

---

## 4. 🧩 Core Data Structures

---

### 4.1 SkillSO (Design-Time Data)

ScriptableObject describing skill configuration.

This class:

- Contains no runtime state
- Contains no gameplay logic

---

### 4.2 Skill (Runtime Entity)

Represents runtime state of a skill.

Responsibilities:

- Track level
- Validate prerequisites
- Validate cost
- Emit level change event
- Never apply gameplay effects

---

### 4.3 CostDefinition

Describes a required cost in a generic way.

---

### 4.4 EffectDefinition (Design-Time)

Describes effect in abstract way.

This is NOT applied directly.

---

## 5. 🔁 Runtime Flow

### 5.1 Upgrade Flow

```
UI Click Upgrade
↓
Skill.TryUpgrade(context)
↓
Validate:
    - not maxed
    - prerequisites met
    - context.CanPay(all costs)
↓
context.Pay(all costs)
↓
Increase level
↓
EmitOnLevelChanged(this)
```

---

### 5.2 Effect Application Flow (Game Layer)

```
Skill emits OnLevelChanged
↓
Game Adapter listens
↓
Remove previous modifiers from this skill
↓
Convert EffectDefinition → EffectModifier
↓
Apply to StatSystem / Other systems
```

---

## 6. 🔌 Skill Context Contract

Skill does not know:

- Player
- XP
- Inventory

Instead, it receives:

```csharp
public interface ISkillContext
{
		bool CanPay(CostDefinition cost);
		void Pay(CostDefinition cost);
}
```

Game implements this interface.

---

## 7. 📊 Effect Modifier (Runtime – Game Layer)

Runtime object applied to game systems.

```csharp
public struct EffectModifier
{
		public string sourceId; // skillId
		public string key;
		public ModifierOperation operation;
		public float value;
}
```

Modifiers are:

- Stored in StatSystem
- Removed by sourceId
- Used to calculate final values

---

## 8. 🧮 Stat System (Game Layer Example)

Example calculation model:

```
FinalValue = BaseValue + Sum(Add modifiers) * Product(Multiply modifiers)
```

Stat system:

- Does not know SkillTree
- Only knows modifiers

---

## 9. 💾 Persistence

Persistence stores:

```
Dictionary<string skillId,int level>
```

On load:

- Create Skill from SkillSO
- Apply saved level
- Derive state from level

State is not stored separately.

---

## 10. 🚫 What SkillTree Must NOT Do

- Must not modify Player directly
- Must not modify Stats directly
- Must not contain MonoBehaviour logic in Skill class
- Must not reference CombatSystem
- Must not reference InventorySystem

---

## 11. 🧭 Dependency Direction

Correct direction:

```
Game Layer → Generic SkillTree Layer
```

Never:

```
SkillTree → Game Layer
```

---

## 12. 🎯 Portfolio Focus

To demonstrate system quality:

- Show decoupled architecture
- Show event-driven design
- Show context-based cost validation
- Show effect abstraction
- Provide minimal demo StatSystem implementation
- Include architecture diagram in README

---

## 13. 📌 Scope Clarification (Important)

This system supports:

- Unlock-only skills
- Upgradeable skills
- Multiple cost types
- Multiple effect types

It does NOT:

- Define gameplay rules
- Define stat formulas
- Define combat logic

---

## 14. 🧠 Key Design Decisions

- State derived from level
- Effects described, not executed
- Two-phase cost validation (check → commit)
- Event-driven integration
- Context abstraction for cost

---

## 15. 🧱 Minimal Viable Framework Version

To avoid over-engineering:

- No transaction system
- No generic service locator
- No reflection
- No complex effect hierarchy

Keep it:

- Simple
- Explicit
- Extensible

---

## 16. 📎 Future Extension Possibilities

- Respec system
- Temporary skill modifiers
- Conditional effects
- Multi-target effects
- Editor visualization tools
- Graph-based node layout

---

## 17. 🏁 End State

When completed:

The system should:

- Plug into another game
- Require only a new adapter layer
- Require no modification to core SkillTree classes

---
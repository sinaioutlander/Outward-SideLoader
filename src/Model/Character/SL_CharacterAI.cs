using UnityEngine;
using UnityEngine.AI;

namespace SideLoader
{
    [SL_Serialized]
    public abstract class SL_CharacterAI
    {
        public bool CanDodge = true;
        public bool CanBlock = true;
        public bool CanWanderFar = true;
        public bool ForceNonCombat;

        public void Apply(Character character) => ApplyToCharacter(character);

        protected abstract void ApplyToCharacter(Character character);
    }

    /// <summary>
    /// WIP, prototype basic AI Preset class.
    /// Eventually this may be superceded by a more complete solution.
    /// </summary>
    public class SL_CharacterAIMelee : SL_CharacterAI
    {
        public float AIContagionRange = 20f;

        public float Wander_Speed = 1.1f;
        public bool Wander_FollowPlayer;
        public AISWander.WanderType Wander_Type = AISWander.WanderType.Wander;
        public SL_Waypoint[] Wander_PatrolWaypoints;

        public float Suspicious_Speed = 1.75f;
        public float Suspicious_Duration = 5f;
        public float Suspicious_Range = 30f;
        public float Suspicious_TurnModif = 0.9f;

        public Vector2 Combat_ChargeTime = new Vector2(4, 8);
        public float Combat_TargetVulnerableChargeModifier = 0.5f;
        public float Combat_ChargeAttackRangeMulti = 1.0f;
        public float Combat_ChargeTimeToAttack = 0.4f;
        public Vector2 Combat_ChargeStartRangeMult = new Vector2(0.8f, 3.0f);
        public float[] Combat_SpeedModifiers = new float[] { 0.8f, 1.3f, 1.7f };
        public float Combat_ChanceToAttack = 75f;
        public bool Combat_KnowsUnblockable = true;
        public float Combat_DodgeCooldown = 3f;

        public AttackPattern[] Combat_AttackPatterns = new AttackPattern[]
        {
            new AttackPattern { ID = 0, Chance = 20, Range = new Vector2(0.9f, 2.5f), Attacks = new AttackPattern.AtkTypes[] { AttackPattern.AtkTypes.Normal } },
            new AttackPattern { ID = 1, Chance = 15, Range = new Vector2(0.0f, 2.9f), Attacks = new AttackPattern.AtkTypes[] { AttackPattern.AtkTypes.Normal, AttackPattern.AtkTypes.Normal } },
            new AttackPattern { ID = 2, Chance = 30, Range = new Vector2(0.0f, 1.5f), Attacks = new AttackPattern.AtkTypes[] { AttackPattern.AtkTypes.Special }},
            new AttackPattern { ID = 3, Chance = 30, Range = new Vector2(0.0f, 1.5f), Attacks = new AttackPattern.AtkTypes[] { AttackPattern.AtkTypes.Normal, AttackPattern.AtkTypes.Special }},
            new AttackPattern { ID = 4, Chance = 30, Range = new Vector2(0.0f, 1.3f), Attacks = new AttackPattern.AtkTypes[] { AttackPattern.AtkTypes.Normal, AttackPattern.AtkTypes.Normal, AttackPattern.AtkTypes.Special }}
        };

        protected override void ApplyToCharacter(Character character)
        {
            var aiRootPrefab = new GameObject("BasicMelee_AIRoot").AddComponent<AIRoot>();
            aiRootPrefab.gameObject.SetActive(false);

            aiRootPrefab.transform.parent = character.transform;

            // -- create base state objects --

            // state 1: Wander
            var wanderState = new GameObject("1_Wander").AddComponent<AISWander>();
            wanderState.transform.parent = aiRootPrefab.transform;

            // state 2: Suspicious
            var susState = new GameObject("2_Suspicious").AddComponent<AISSuspicious>();
            susState.transform.parent = aiRootPrefab.transform;

            //state 3: alert
            var alertState = new GameObject("3_Alert").AddComponent<AISSuspicious>();
            alertState.transform.parent = aiRootPrefab.transform;

            //state 4: Combat
            var combatState = new GameObject("4_Combat").AddComponent<AISCombatMelee>();
            combatState.transform.parent = aiRootPrefab.transform;

            // ---- setup actual state parameters and links ----

            // setup 1 - Wander

            wanderState.ContagionRange = this.AIContagionRange;
            wanderState.ForceNotCombat = this.ForceNonCombat;
            wanderState.SpeedModif = this.Wander_Speed;
            wanderState.WanderFar = this.CanWanderFar;
            wanderState.AutoFollowPlayer = this.Wander_FollowPlayer;

            if (this.Wander_PatrolWaypoints != null && this.Wander_Type == AISWander.WanderType.Patrol)
            {
                var wanderTrans = new GameObject($"Waypoints_{character.UID}");
                wanderState.WaypointsParent = wanderTrans.transform;

                for (int i = 0; i < this.Wander_PatrolWaypoints.Length; i++)
                {
                    var waypointObj = new GameObject("Waypoint " + i + 1);
                    var waypoint = waypointObj.AddComponent<Waypoint>();

                    waypointObj.transform.parent = wanderTrans.transform;

                    waypointObj.transform.position = Wander_PatrolWaypoints[i].WorldPosition;
                    waypoint.RandomRadius = Wander_PatrolWaypoints[i].RandomRadius;
                    waypoint.WaitTime = Wander_PatrolWaypoints[i].WaitTime;
                }
            }

            var wanderDetection = new GameObject("Detection").AddComponent<AICEnemyDetection>();
            wanderDetection.transform.parent = wanderState.transform;
            var wanderDetectEffects = new GameObject("DetectEffects").AddComponent<AIESwitchState>();
            wanderDetectEffects.ToState = susState;
            wanderDetectEffects.transform.parent = wanderDetection.transform;
            wanderDetection.DetectEffectsTrans = wanderDetectEffects.transform;

            //setup 2 - Suspicious

            susState.SpeedModif = this.Suspicious_Speed;
            susState.SuspiciousDuration = this.Suspicious_Duration;
            susState.Range = this.Suspicious_Range;
            susState.WanderFar = this.CanWanderFar;
            susState.TurnModif = this.Suspicious_TurnModif;

            var susEnd = new GameObject("EndSuspiciousEffects").AddComponent<AIESwitchState>();
            susEnd.ToState = wanderState;
            var sheathe = susEnd.gameObject.AddComponent<AIESheathe>();
            sheathe.Sheathed = true;
            susEnd.transform.parent = susState.transform;
            susState.EndSuspiciousEffectsTrans = susEnd.transform;

            var susDetection = new GameObject("Detection").AddComponent<AICEnemyDetection>();
            susDetection.transform.parent = susState.transform;
            var susDetectEffects = new GameObject("DetectEffects").AddComponent<AIESwitchState>();
            susDetectEffects.ToState = combatState;
            susDetectEffects.transform.parent = susDetection.transform;
            susDetection.DetectEffectsTrans = susDetectEffects.transform;
            var susSuspiciousEffects = new GameObject("SuspiciousEffects").AddComponent<AIESwitchState>();
            susSuspiciousEffects.ToState = alertState;
            susSuspiciousEffects.transform.parent = susDetection.transform;
            susDetection.SuspiciousEffectsTrans = susSuspiciousEffects.transform;

            // setup 3 - alert

            alertState.SpeedModif = this.Suspicious_Speed;
            alertState.SuspiciousDuration = this.Suspicious_Duration;
            alertState.Range = this.Suspicious_Range;
            alertState.WanderFar = this.CanWanderFar;
            alertState.TurnModif = this.Suspicious_TurnModif;

            var alertEnd = new GameObject("EndSuspiciousEffects").AddComponent<AIESwitchState>();
            alertEnd.ToState = susState;
            var alertsheathe = alertEnd.gameObject.AddComponent<AIESheathe>();
            alertsheathe.Sheathed = true;
            alertEnd.transform.parent = alertState.transform;
            alertState.EndSuspiciousEffectsTrans = alertEnd.transform;

            var alertDetection = new GameObject("Detection").AddComponent<AICEnemyDetection>();
            alertDetection.transform.parent = alertState.transform;
            var alertDetectEffects = new GameObject("DetectEffects").AddComponent<AIESwitchState>();
            alertDetectEffects.ToState = combatState;
            alertDetectEffects.transform.parent = alertDetection.transform;
            alertDetection.DetectEffectsTrans = alertDetectEffects.transform;

            // setup 4 - Combat

            combatState.ChargeTime = this.Combat_ChargeTime;
            combatState.TargetVulnerableChargeTimeMult = this.Combat_TargetVulnerableChargeModifier;
            combatState.ChargeAttackRangeMult = this.Combat_ChargeAttackRangeMulti;
            combatState.ChargeAttackTimeToAttack = this.Combat_ChargeTimeToAttack;
            combatState.ChargeStartRangeMult = this.Combat_ChargeStartRangeMult;
            combatState.AttackPatterns = this.Combat_AttackPatterns;
            combatState.SpeedModifs = this.Combat_SpeedModifiers;
            combatState.ChanceToAttack = this.Combat_ChanceToAttack;
            combatState.KnowsUnblockable = this.Combat_KnowsUnblockable;
            combatState.DodgeCooldown = this.Combat_DodgeCooldown;
            combatState.CanBlock = this.CanBlock;
            combatState.CanDodge = this.CanDodge;

            var combatDetect = new GameObject("Detection").AddComponent<AICEnemyDetection>();
            combatDetect.transform.parent = combatState.transform;
            var combatEnd = new GameObject("EndCombatEffects").AddComponent<AIESwitchState>();
            combatEnd.ToState = wanderState;
            combatEnd.transform.parent = combatState.transform;

            // add required components for AIs (no setup required)
            character.gameObject.AddComponent<NavMeshAgent>();
            character.gameObject.AddComponent<AISquadMember>();
            character.gameObject.AddComponent<EditorCharacterAILoadAI>();

            if (character.GetComponent<NavMeshObstacle>() is NavMeshObstacle navObstacle)
                GameObject.Destroy(navObstacle);

            // add our basic AIStatesPrefab to a CharacterAI component. This is the prefab set up by SetupBasicAIPrefab(), below.
            CharacterAI charAI = character.gameObject.AddComponent<CharacterAI>();
            charAI.m_character = character;
            charAI.AIStatesPrefab = aiRootPrefab;

            // initialize the AI States (not entirely necessary, but helpful if we want to do something with the AI immediately after)
            charAI.GetAIStates();
        }
    }

    [SL_Serialized]
    public class SL_Waypoint
    {
        public Vector3 WorldPosition;
        public float RandomRadius;
        public Vector2 WaitTime;
    }
}

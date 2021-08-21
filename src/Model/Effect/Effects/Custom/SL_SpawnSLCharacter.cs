using System;
using System.Collections;
using System.Xml.Serialization;
using UnityEngine;

namespace SideLoader
{
    public class SL_SpawnSLCharacter : SL_Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_SpawnSLCharacter);
        public Type GameModel => typeof(SpawnSLCharacter);

        /// <summary>The SL_Character.UID you want to spawn.</summary>
        public string SLCharacter_UID;
        /// <summary>Whether to generate a random UID for each spawn, or just use the SLCharacter UID.</summary>
        public bool GenerateRandomUIDForSpawn;
        /// <summary>If true, will attempt to follow the caster character (should set the Wander_Type to Follow in this case).</summary>
        public bool TryFollowCaster;
        /// <summary>Position offset for character spawn position</summary>
        public Vector3 SpawnOffset;

        public override void ApplyToComponent<T>(T component)
        {
            var comp = component as SpawnSLCharacter;

            comp.SLCharacter_UID = this.SLCharacter_UID;
            comp.TryFollowCaster = this.TryFollowCaster;
            comp.GenerateRandomUIDForSpawn = this.GenerateRandomUIDForSpawn;
            comp.SpawnOffset = this.SpawnOffset;

            // Required for SL_SpawnSLCharacter.
            comp.SyncType = Effect.SyncTypes.MasterSync;
        }

        public override void SerializeEffect<T>(T effect)
        {
            var comp = effect as SpawnSLCharacter;

            this.SLCharacter_UID = comp.SLCharacter_UID;
            this.TryFollowCaster = comp.TryFollowCaster;
            this.SpawnOffset = comp.SpawnOffset;
            this.GenerateRandomUIDForSpawn = comp.GenerateRandomUIDForSpawn;
        }
    }

    public class SpawnSLCharacter : Effect, ICustomModel
    {
        public Type SLTemplateModel => typeof(SL_SpawnSLCharacter);
        public Type GameModel => typeof(SpawnSLCharacter);

        public string SLCharacter_UID;
        public bool GenerateRandomUIDForSpawn;
        public bool TryFollowCaster;
        public Vector3 SpawnOffset;

        [XmlIgnore] public string ExtraRpcData;

        protected SL_Character m_charTemplate;

        public override void AwakeInit()
        {
            CustomCharacters.Templates.TryGetValue(this.SLCharacter_UID, out m_charTemplate);

            if (m_charTemplate == null)
                SL.LogWarning("SpawnSLCharacter.Awake - m_charTemplate is null, could not find from UID '" + this.SLCharacter_UID + "'");
        }

        public override bool TryTriggerConditions()
        {
            if (m_charTemplate == null)
            {
                m_affectedCharacter?.CharacterUI?.ShowInfoNotification("Error - Custom Character template not found!");
                return false;
            }

            return base.TryTriggerConditions();
        }

        public override void ActivateLocally(Character _affectedCharacter, object[] _infos)
        {
            if (PhotonNetwork.isNonMasterClientInRoom)
                return;

            if (m_charTemplate != null)
            {
                var pos = this.OwnerCharacter
                            ? this.OwnerCharacter.transform.position
                            : (Vector3)_infos[0];

                pos += this.SpawnOffset;

                var ai = m_charTemplate.Spawn(pos,
                    Vector3.zero,
                    this.GenerateRandomUIDForSpawn
                        ? (string)UID.Generate()
                        : this.SLCharacter_UID,
                    this.ExtraRpcData);

                if (!ai)
                    SL.LogWarning("SpawnSLCharacter.ActivateLocally - spawn failed!");
                else if (TryFollowCaster)
                    StartCoroutine(WaitForAIAndFollow(ai, this.OwnerCharacter));
            }
        }

        private IEnumerator WaitForAIAndFollow(Character ai, Character owner)
        {
            if (!ai || !owner)
                yield break;

            var aiWander = ai.GetComponentInChildren<AISWander>();
            if (!aiWander)
            {
                var time = 5f;
                var wait = new WaitForEndOfFrame();
                while (!aiWander && time > 0f)
                {
                    time -= Time.deltaTime;
                    yield return wait;
                    aiWander = ai.GetComponentInChildren<AISWander>();
                }
            }

            if (aiWander)
                aiWander.FollowTransform = owner.transform;
            else
                SL.LogWarning("WaitForAIAndFollow timeout");
        }
    }
}

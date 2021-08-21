using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SideLoader
{
    [SL_Serialized]
    public class SL_SkillTree
    {
        /// <summary>The name of the SLPack used to load certain assets from (eg if using SigilIconName)</summary>
        [XmlIgnore] public string SLPackName;

        /// <summary>Can be used to directly set the Sigil, and not use the SLPackName/SigilIconName fields. </summary>
        [XmlIgnore] public Sprite Sigil;

        /// <summary>If SLPackName is set, SideLoader will look for a Texture2D (in your Texture2D or Texture2D\Local folder) 
        /// with this name (without .png) and use that for the sigil icon.</summary>
        public string SigilIconName;

        /// <summary>Displayed Skill Tree name</summary>
        public string Name;
        /// <summary>The unique identifier for this skill tree, eg "com.me.mymod"</summary>
        public string UID;

        ///// <summary>
        ///// The Item ID of the Item used for buying the skills, by default it's Silver (9000010).
        ///// </summary>
        //public int CurrencyItemID = 9000010;

        // todo alternate currency icon sprite

        /// <summary>The actual skill tree rows </summary>
        public List<SL_SkillRow> SkillRows = new List<SL_SkillRow>();

        [XmlIgnore]
        private GameObject m_object;

        public SkillSchool CreateBaseSchool()
        {
            SceneManager.sceneLoaded += FixOnMainMenu;
            return CreateSchool(false);
        }

        public SkillSchool CreateBaseSchool(bool applyRowsInstantly)
        {
            SceneManager.sceneLoaded += FixOnMainMenu;
            return CreateSchool(applyRowsInstantly);
        }

        private SkillSchool CreateSchool(bool applyRowsInstantly = false)
        {
            var template = (Resources.Load("_characters/CharacterProgression") as GameObject).transform.Find("Test");

            // instantiate a copy of the dev template
            m_object = UnityEngine.Object.Instantiate(template).gameObject;

            var school = m_object.GetComponent<SkillSchool>();

            // set the name to the gameobject and the skill tree name/uid
            m_object.name = this.Name;
            school.m_defaultName = this.Name;
            school.m_nameLocKey = "";

            if (string.IsNullOrEmpty(this.UID))
                this.UID = this.Name;
            school.m_uid = new UID(this.UID);

            // TODO set currency and icon

            // fix the breakthrough int
            school.m_breakthroughSkillIndex = -1;

            // set the sprite
            if (this.Sigil)
                school.SchoolSigil = this.Sigil;
            else if (!string.IsNullOrEmpty(this.SigilIconName) && !string.IsNullOrEmpty(this.SLPackName))
            {
                var pack = SL.GetSLPack(this.SLPackName);
                if (pack != null)
                {
                    if (pack.Texture2D.TryGetValue(this.SigilIconName, out Texture2D sigilTex))
                    {
                        var sprite = CustomTextures.CreateSprite(sigilTex);
                        school.SchoolSigil = sprite;
                    }
                    else
                        SL.LogWarning("Applying an SL_SkillTree, could not find any loaded Texture by the name of '" + this.SigilIconName + "'");
                }
                else
                    SL.LogWarning("Applying an SL_SkillSchool, could not find any loaded SLPack with the name '" + this.SLPackName + "'");
            }

            // add it to the game's skill tree holder.
            var list = SkillTreeHolder.Instance.m_skillTrees.ToList();
            list.Add(school);
            SkillTreeHolder.Instance.m_skillTrees = list.ToArray();

            if (applyRowsInstantly)
                ApplyRows();

            return school;
        }

        public void ApplyRows() => ApplyRows(SkillTreeHolder.Instance.m_skillTrees.Length);

        public void ApplyRows(int treeID)
        {
            if (!this.m_object)
            {
                SL.Log("Trying to apply SL_SkillSchool but it is not created yet! Call CreateBaseSchool first!");
                return;
            }

            var school = m_object.GetComponent<SkillSchool>();

            school.m_branches = new List<SkillBranch>();
            school.m_skillSlots = new List<BaseSkillSlot>();

            for (int i = 0; i < 6; i++)
            {
                if (m_object.transform.Find("Row" + i) is Transform row)
                    UnityEngine.Object.DestroyImmediate(row.gameObject);
            }

            foreach (var row in this.SkillRows)
            {
                row.ApplyToSchoolTransform(m_object.transform, treeID);
            }

            m_object.transform.parent = SkillTreeHolder.Instance.transform;
            m_object.SetActive(true);
        }

        private void FixOnMainMenu(Scene scene, LoadSceneMode mode)
        {
            if (scene.name.ToLower().Contains("mainmenu"))
                SLPlugin.Instance.StartCoroutine(FixOnMenuCoroutine());
        }

        private IEnumerator FixOnMenuCoroutine()
        {
            yield return new WaitForSeconds(1f);

            while (!SkillTreeHolder.Instance)
                yield return null;

            CreateSchool();
            ApplyRows();
        }
    }

    [SL_Serialized]
    public class SL_SkillRow
    {
        public int RowIndex;

        public List<SL_BaseSkillSlot> Slots = new List<SL_BaseSkillSlot>();

        public void ApplyToSchoolTransform(Transform schoolTransform, int treeID)
        {
            var row = schoolTransform.Find("Row" + RowIndex);
            if (!row)
            {
                row = new GameObject("Row" + this.RowIndex).transform;
                row.parent = schoolTransform;
                row.gameObject.AddComponent<SkillBranch>();
            }

            foreach (var slot in this.Slots)
                slot.ApplyToRow(row, treeID);

        }
    }

    [SL_Serialized]
    public abstract class SL_BaseSkillSlot
    {
        public int ColumnIndex;
        public Vector2 RequiredSkillSlot = Vector2.zero;

        public abstract SkillSlot ApplyToRow(Transform row, int treeID);

        /// <summary>
        /// Internal use for setting a required slot.
        /// </summary>
        /// <param name="comp">The component that this SkillSlot is setting. Not the required slot.</param>
        public void SetRequiredSlot(BaseSkillSlot comp)
        {
            bool success = false;
            var reqrow = RequiredSkillSlot.x;
            var reqcol = RequiredSkillSlot.y;

            if (comp.transform.root.Find("Row" + reqrow) is Transform reqrowTrans
                && reqrowTrans.Find("Col" + reqcol) is Transform reqcolTrans)
            {
                var reqslot = reqcolTrans.GetComponent<BaseSkillSlot>();
                if (reqslot)
                {
                    comp.m_requiredSkillSlot = reqslot;
                    success = true;
                }
            }

            if (!success)
                SL.Log("Could not set required slot. Maybe it's not set yet?");
        }
    }

    public class SL_SkillSlotFork : SL_BaseSkillSlot
    {
        public SL_SkillSlot Choice1;
        public SL_SkillSlot Choice2;

        public override SkillSlot ApplyToRow(Transform row, int treeID)
        {
            var col = new GameObject("Col" + this.ColumnIndex);
            col.transform.parent = row;

            var comp = col.AddComponent<SkillSlotFork>();
            comp.m_columnIndex = this.ColumnIndex;

            if (this.RequiredSkillSlot != Vector2.zero)
                SetRequiredSlot(comp);

            Choice1.ApplyToRow(col.transform, treeID);
            Choice2.ApplyToRow(col.transform, treeID);

            return null;
        }
    }

    public class SL_SkillSlot : SL_BaseSkillSlot
    {
        public int SkillID;
        public int SilverCost;
        public bool Breakthrough;

        public override SkillSlot ApplyToRow(Transform row, int treeID)
        {
            var col = new GameObject("Col" + this.ColumnIndex);
            col.transform.parent = row;

            var comp = col.AddComponent<SkillSlot>();
            comp.IsBreakthrough = Breakthrough;

            comp.m_requiredMoney = SilverCost;
            comp.m_columnIndex = ColumnIndex;

            var skill = ResourcesPrefabManager.Instance.GetItemPrefab(SkillID) as Skill;

            if (!skill)
            {
                SL.LogWarning("SL_SkillSlot: Could not find skill by id '" + SkillID + "'");
                return comp;
            }

            comp.m_skill = skill;
            skill.m_schoolIndex = treeID;
            //SL.LogWarning("Set " + treeID + " for " + skill.Name + "'s treeID");

            if (this.RequiredSkillSlot != Vector2.zero)
                SetRequiredSlot(comp);

            return comp;
        }
    }
}

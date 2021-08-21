using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using NodeCanvas.Tasks.Actions;
using System.Collections.Generic;
using UnityEngine;

namespace SideLoader
{
    public class SL_CharacterTrainer : SL_Character
    {
        public SL_SkillTree SkillTree;

        public string InitialDialogue;

        internal static SkillSchool m_skillTree;

        internal override void OnPrepare()
        {
            if (this.SkillTree != null)
            {
                if (string.IsNullOrEmpty(SkillTree.SLPackName))
                    SkillTree.SLPackName = this.SerializedSLPackName;

                m_skillTree = this.SkillTree.CreateBaseSchool(true);
            }
        }

        public override void ApplyToCharacter(Character trainer, bool loadingFromSave)
        {
            base.ApplyToCharacter(trainer, loadingFromSave);

            var trainertemplate = GameObject.Instantiate(Resources.Load<GameObject>("editor/templates/TrainerTemplate"));
            trainertemplate.transform.parent = trainer.transform;
            trainertemplate.transform.position = trainer.transform.position;

            // set Dialogue Actor name
            var trainerActor = trainertemplate.GetComponentInChildren<DialogueActor>();
            trainerActor.SetName(this.Name);

            // get "Trainer" component, and set the SkillTreeUID to our custom tree UID
            var trainerComp = trainertemplate.GetComponentInChildren<Trainer>();
            if (this.SkillTree != null)
                trainerComp.m_skillTreeUID = new UID(SkillTree.UID);
            else
                SL.LogWarning("Setting up an SL_CharacterTrainer (" + this.UID + ") but no SL_SkillTree has been created for it!");

            // setup dialogue tree
            var graphController = trainertemplate.GetComponentInChildren<DialogueTreeController>();
            var graph = graphController.graph;

            // the template comes with an empty ActorParameter, we can use that for our NPC actor.
            var actors = (graph as DialogueTree)._actorParameters;
            actors[0].actor = trainerActor;
            actors[0].name = this.Name;

            // setup the actual dialogue now
            var rootStatement = graph.allNodes[0] as StatementNodeExt;
            rootStatement.statement = new Statement(this.InitialDialogue);
            rootStatement.SetActorName(this.Name);

            // the template already has an action node for opening the Train menu. 
            // Let's grab that and change the trainer to our custom Trainer component (setup above).
            var openTrainer = graph.allNodes[1] as ActionNode;
            (openTrainer.action as TrainDialogueAction).Trainer = new BBParameter<Trainer>(trainerComp);

            // ===== finalize nodes =====
            graph.allNodes.Clear();
            // add the nodes we want to use
            graph.allNodes.Add(rootStatement);
            graph.allNodes.Add(openTrainer);
            graph.primeNode = rootStatement;
            graph.ConnectNodes(rootStatement, openTrainer);

            // set the trainer active
            trainer.gameObject.SetActive(true);
        }

        public override void SetStats(Character character)
        {
            //base.SetStats(character);
            var stats = character.GetComponent<CharacterStats>();
            if (stats)
                stats.enabled = false;
        }
    }
}

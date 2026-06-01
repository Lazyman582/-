using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static BehaviorTree.BehaviorTree;

namespace BehaviorTree
{
    [RequireComponent(typeof(Blackboard))]
    public class BehaviorTree : MonoBehaviour
    {
        private Node root ;

        public Node Root { get => root; set => root = value; }

        private Blackboard blackboard;

        private void Awake()
        {
            Onsetup();
            blackboard =GetComponent<Blackboard>();
        }

        private void Start()
        {
            blackboard = GetComponent<Blackboard>();
        }
        private void Update()
        {
            if(root == null)
            root?.Evaluate(gameObject.transform,blackboard);
        }


        protected virtual void Onsetup() { 
        
        
        
        }

    }

    public enum Staus
    {
        Failure = 0,
        succese,
        Running

    }
    public abstract class Node
    {

        private Node parent;
        protected List<Node> children = new List<Node>();
        private Staus staus;
      public Staus Staus {

            get => staus;
            protected set => staus = value;
        
        }

        public Staus Evaluate(Transform agent, Blackboard blackboard)
        {


            Debug.Log(message: $"{GetType().Name} - Entered...");
            staus = OnEvaluate(agent,blackboard);
            Debug.Log(message: $"{GetType().Name} - {staus}");
            Debug.Log(message: $"{GetType().Name} - Exited...");
            return staus;
        }

        protected abstract Staus OnEvaluate(Transform agent, Blackboard blackboard);

    }
}

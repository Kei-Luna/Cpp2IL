﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Cpp2IL.Core.Graphs;

public class AbstractControlFlowGraph<TInstruction, TNode> where TNode : InstructionGraphNode<TInstruction>, new()
{
        protected List<TInstruction> Instructions;

        protected TNode  EndNode;
    
        Dictionary<ulong, TInstruction> InstructionsByAddress;
        protected int idCounter = 0;

        protected AbstractControlFlowGraph(List<TInstruction> instructions)
        {
            if (instructions == null)
                throw new ArgumentNullException(nameof(instructions));


            var startNode = new TNode() {ID = idCounter++};
            EndNode = new TNode() {ID = idCounter++};
            Instructions = instructions;
            InstructionsByAddress = new Dictionary<ulong, TInstruction>();
            Root = startNode;
            nodeSet = new Collection<TNode>();
            
            foreach (var instruction in Instructions)
                InstructionsByAddress.Add(GetAddressOfInstruction(instruction), instruction);
            
        }

        protected virtual ulong GetAddressOfInstruction(TInstruction instruction)
        {
            throw new NotImplementedException();
        }

        protected TNode? SplitAndCreate(TNode target, int index)
        {
            if(index < 0 || index >= target.Instructions.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (index == 0)
                return null;

            var newNode = new TNode(){ID = idCounter++};
            
            var instructions = target.Instructions.GetRange(index, target.Instructions.Count - index);
            target.Instructions.RemoveRange(index, target.Instructions.Count - index);
            newNode.Instructions.AddRange(instructions);
            newNode.FlowControl = target.FlowControl;
            target.FlowControl = InstructionGraphNodeFlowControl.Continue;
            newNode.Successors = target.Successors;
            
            target.Successors = new ();

            return newNode;
        }
        

        public void Run(bool print = false)
        {
            AddNode(Root);
            if (Instructions.Count == 0)
            {
                AddNode(EndNode); 
                AddDirectedEdge(Root, EndNode);
                return;
            }
            BuildInitialGraph();
            AddNode(EndNode);
            SegmentGraph();
            ConstructConditions();
            if(print)
                Print();
            DetermineLocals();
        }

        protected virtual void DetermineLocals()
        {
            throw new NotImplementedException();
        }

        private void ConstructConditions()
        {
            foreach (var graphNode in Nodes)
            {
                if (graphNode.IsCondtionalBranch)
                {
                    graphNode.CheckCondition();
                }
            }
        }

        protected virtual void SegmentGraph()
        {
            throw new NotImplementedException();
        }

        protected virtual void BuildInitialGraph()
        {
            throw new NotImplementedException();
        }

        protected TNode? FindNodeByAddress(ulong address)
        {
            if (InstructionsByAddress.TryGetValue(address, out var instruction))
            {
                foreach (var node in Nodes)
                {
                    if (node.Instructions.Any(instr => GetAddressOfInstruction(instr) == address))
                    {
                        return node;
                    }
                }
            }
            return null;
        }
    public TNode Root { get; }
        
    private Collection<TNode> nodeSet;

    protected void AddDirectedEdge(TNode from, TNode to)
    {
        from.Successors.Add(to);
        to.Predecessors.Add(from);
    }

    protected void AddNode(TNode node) => nodeSet.Add(node);
    
        
    public void Print()
    {
        foreach (var node in nodeSet)
        {
            Console.WriteLine("=========================");
            Console.Write($"ID: {node.ID}, FC: {node.FlowControl}, Successors:{string.Join(",", node.Successors.Select(i => i.ID))}, Predecessors:{string.Join(",", node.Predecessors.Select(i => i.ID))}");
            if(node.IsCondtionalBranch)
                Console.Write($", Condition: {node.Condition?.ConditionString ?? "Null"}");
            if(node.Instructions.Count > 0)
                Console.Write($", Address {node.GetFormattedInstructionAddress(node.Instructions.First())}");
            Console.Write("\n");
            foreach (var v in node.Instructions)
            {
                Console.WriteLine(v.ToString());
            }
            Console.WriteLine();
        }
    }

    protected Collection<TNode> Nodes => nodeSet;

    public int Count => nodeSet.Count;
}
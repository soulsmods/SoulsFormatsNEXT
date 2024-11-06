using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoulsAssetPipeline.Audio.Wwise
{
    public static partial class WwiseObject
    {
        public class CAkDialogueEvent : WwiseObjectBase<CAkDialogueEvent>
        {
            //Normal
            public byte Probability;
            public int TreeDepth;
            //public WwiseObjectList<AkGameSync> Arguments = new WwiseObjectList<AkGameSync>(nameof(TreeDepth));
            public WwiseIDList ArgumentGroupIDs = new WwiseIDList(nameof(TreeDepth));
            public WwiseByteList ArgumentGroupTypes = new WwiseByteList(nameof(TreeDepth));
            public uint TreeDataSize;
            public byte Mode;
            // Custom

            public Node RootNode;

            // Normal
            public PropertyBundle Props = new PropertyBundle();
            public RangedPropertyBundle RangedProps = new RangedPropertyBundle();

            public abstract class NodeTarget
            {
                public abstract void ReadTarget(BinaryReaderEx br);
                public abstract void WriteTarget(BinaryWriterEx bw);
            }

            public class NodeTargetEnd : NodeTarget
            {
                public uint AudioNodeID;
                public override void ReadTarget(BinaryReaderEx br) => AudioNodeID = br.ReadUInt32();
                public override void WriteTarget(BinaryWriterEx bw) => bw.WriteUInt32(AudioNodeID);
            }

            public class NodeTargetChildren : NodeTarget
            {
                public ushort ChildrenIndex;
                public ushort ChildrenCount;

                public List<Node> Children = new List<Node>();

                public Node DefaultChild = null;

                public override void ReadTarget(BinaryReaderEx br)
                {
                    ChildrenIndex = br.ReadUInt16();
                    ChildrenCount = br.ReadUInt16();
                }

                public override void WriteTarget(BinaryWriterEx bw)
                {
                    bw.WriteUInt16(ChildrenIndex);
                    bw.WriteUInt16(ChildrenCount);
                }
            }

            public class Node : WwiseObjectBase<Node>
            {
                public uint ID;
                public ushort Weight;
                public ushort Probability;
                public NodeTarget Target;
                public uint GameVariableHash;

                public bool PassesProbabilityCheck(Random rand)
                {
                    return (rand.Next(0, 100) <= Probability);
                }

                public bool IsPassCheck(Dictionary<uint, uint> vars)
                {
                    if (vars.ContainsKey(GameVariableHash))
                    {
                        if (vars[GameVariableHash] == ID)
                        {
                            return true;
                        }
                    }

                    return false;
                }


                

                internal override bool CustomRead(BinaryReaderEx br, IWwiseObject parent)
                {
                    ReadField(br, nameof(ID));

                    var target = br.GetUInt32(br.Position);
                    //Shitass heuristic
                    var targetUpper = (target >> 16) & 0xFFFF;
                    var targetLower = (target) & 0xFFFF;
                    if (targetUpper < 1000 && targetLower < 1000)
                        Target = new NodeTargetChildren();
                    else
                        Target = new NodeTargetEnd();
                    Target.ReadTarget(br);

                    ReadField(br, nameof(Weight));
                    ReadField(br, nameof(Probability));
                    return true;
                }

                internal override bool CustomWrite(BinaryWriterEx bw, IWwiseObject parent)
                {
                    throw new NotImplementedException();
                }
            }

            internal override bool CustomRead(BinaryReaderEx br, IWwiseObject parent)
            {
                // Normal
                ReadField(br, nameof(Probability));
                ReadField(br, nameof(TreeDepth));
                //ReadField(br, nameof(Arguments));
                ReadField(br, nameof(ArgumentGroupIDs));
                ReadField(br, nameof(ArgumentGroupTypes));
                ReadField(br, nameof(TreeDataSize));
                ReadField(br, nameof(Mode));

                // Gets weird here
                var nodeCount = TreeDataSize / 0xC;

                // Alternate flat way of reading
                var flattenedNodes = new List<Node>();
                for (int i = 0; i < nodeCount; i++)
                {
                    var newChildNode = new Node();
                    newChildNode.Read(br, this);

                    if (newChildNode.Target is NodeTargetChildren asChildTarget && (asChildTarget.ChildrenIndex >= nodeCount || asChildTarget.ChildrenCount >= nodeCount))
                    {
                        newChildNode.Target = new NodeTargetEnd()
                        {
                            AudioNodeID = unchecked((uint)((asChildTarget.ChildrenCount) | (asChildTarget.ChildrenIndex << 16))),
                        };
                    }

                    flattenedNodes.Add(newChildNode);
                }

                for (int i = 0; i < flattenedNodes.Count; i++)
                {
                    if (flattenedNodes[i].Target is NodeTargetChildren asParent)
                    {
                        asParent.Children.Clear();
                        for (int j = 0; j < asParent.ChildrenCount; j++)
                        {
                            asParent.Children.Add(flattenedNodes[asParent.ChildrenIndex + j]);
                        }
                        asParent.DefaultChild = asParent.Children.FirstOrDefault(c => c.ID == 0);
                    }
                }
                RootNode = flattenedNodes[0];
                void RecursiveSetGameVarHash(List<Node> children, int depth)
                {
                    foreach (var c in children)
                    {
                        //c.GameVariableHash = Arguments[depth].Group;
                        c.GameVariableHash = ArgumentGroupIDs[depth];
                        if (c.Target is NodeTargetChildren asParent)
                            RecursiveSetGameVarHash(asParent.Children, depth + 1);
                    }
                }
                if (RootNode.Target is NodeTargetChildren asRootParent)
                {
                    RecursiveSetGameVarHash(asRootParent.Children, 0);
                }

                //List<Node> ParseTreeNode(int count, int curDepth)
                //{
                //    List<Node> childNodes = new List<Node>();

                //    for (int i = 0; i < count; i++)
                //    {
                //        var newChildNode = new Node();
                //        newChildNode.Read(br, this);

                //        if (newChildNode.Target is NodeTargetChildren asChildTarget && (asChildTarget.ChildrenIndex >= nodeCount || asChildTarget.ChildrenCount >= nodeCount))
                //        {
                //            newChildNode.Target = new NodeTargetEnd()
                //            {
                //                AudioNodeID = unchecked((uint)((asChildTarget.ChildrenCount) | (asChildTarget.ChildrenIndex << 16))),
                //            };
                //        }

                //        childNodes.Add(newChildNode);
                //    }

                //    foreach (var child in childNodes)
                //    {
                //        if (child.Target is NodeTargetChildren asChildrenTarget)
                //        {
                //            asChildrenTarget.Children = ParseTreeNode(asChildrenTarget.ChildrenCount, curDepth + 1);
                //            asChildrenTarget.DefaultChild = asChildrenTarget.Children.FirstOrDefault(x => x.ID == 0);
                //        }
                //    }

                //    return childNodes;
                    
                //}

                ////first node
                //var root = ParseTreeNode(count: 1, 0);
                //RootNode = root[0];

                // Normal
                return true;
            }

            internal override bool CustomWrite(BinaryWriterEx bw, IWwiseObject parent)
            {
                throw new NotImplementedException();
            }
        }
    }
}

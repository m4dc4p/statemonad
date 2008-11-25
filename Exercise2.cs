using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StateMonad
{
    namespace Exercise2
    {
        // Exercise 2: go from labeling a tree to doing a constrained
        // container computation, as in WPF. Give everything a
        // bounding box, and size subtrees to fit inside their
        // parents, recursively.

        /// <summary>
        /// A container to fit nodes within. Assume nodes fill their
        /// container.
        /// </summary>
        public class Size
        {
            public Size(int w, int h) { Height = h; Width = w; }

            public int Width { get; private set; }
            public int Height { get; private set; }

            public override string ToString()
            {
                return Width.ToString() + ", " + Height.ToString();
            }
        }

        public class Point
        {
            public Point(int x, int y) { X = x; Y = y; }

            public int X { get; private set; }
            public int Y { get; private set; }

            public override string ToString()
            {
                return X.ToString() + ", " + Y.ToString();
            }
        }

        /// <summary>
        /// Given root a tree and a container size, find the rectangles for each
        /// node in the tree such that all nodes will fit in the container. Only leafs
        /// actually get sizes - branches represent depth only. It is assumed that leaves
        /// will fill all available space.
        /// </summary>
        public class ConstrainTree
        {
            public delegate Exercise1.SM<Size, Size> Updater();

            public static Exercise1.SM<Size, Size> RightUpdateState()
            {
                return new Exercise1.SM<Size, Size>
                {
                    s2scp = (size => new Exercise1.Scp<Size, Size>
                    {
                        label = new Size(size.Width * 2, size.Height * 2),
                        lcpContents = size
                    })
                };
            }

            public static Exercise1.SM<Size, Size> LeftUpdateState()
            {
                return new Exercise1.SM<Size, Size>
                {
                    s2scp = (size => new Exercise1.Scp<Size, Size>
                    {
                        label = new Size(size.Width / 2, size.Height / 2),
                        lcpContents = new Size(size.Width / 2, size.Height / 2)
                    })
                };
            }

            public static Exercise1.SM<Size, Program.Tr<Exercise1.Scp<Size, Size>>> Mk<A>(Program.Tr<A> tree, Updater update)
            {
                if (tree is Program.Lf<A>)
                {
                    var leaf = (Program.Lf<A>)tree;
                    return Exercise1.SM<Size, Size>.bind<Program.Tr<Exercise1.Scp<Size, Size>>>(update(),
                        (contents1 =>
                            Exercise1.SM<Size, Program.Tr<Exercise1.Scp<Size, Size>>>.@return(
                            new Program.Lf<Exercise1.Scp<Size, Size>>
                            {
                                contents = new Exercise1.Scp<Size, Size>
                                {
                                    label = contents1,
                                    lcpContents = contents1
                                }
                            })));
                }
                else
                {
                    var branch = (Program.Br<A>)tree;
                    var left = branch.left;
                    var right = branch.right;

                    return Exercise1.SM<Size, Program.Tr<Exercise1.Scp<Size, Size>>>.bind<Program.Tr<Exercise1.Scp<Size, Size>>>(
                        Mk<A>(left, new Updater(LeftUpdateState)),
                        (newLeft => Exercise1.SM<Size, Program.Tr<Exercise1.Scp<Size, Size>>>.bind<Program.Tr<Exercise1.Scp<Size, Size>>>(
                            Mk<A>(right, new Updater(RightUpdateState)),
                            (newRight => Exercise1.SM<Size, Program.Tr<Exercise1.Scp<Size, Size>>>.@return(
                                new Program.Br<Exercise1.Scp<Size, Size>>
                                {
                                    left = newLeft,
                                    right = newRight
                                }
                                ))
                                )));
                }
            }

            /// <summary>
            /// After traversal the width and height on each node will represent the maximum
            /// bounding box size necessary to cover all children of the node, assuming a binary
            /// tree laid out "down". A bounding box of 1 x 1 will over the node itself.
            /// </summary>
            /// <typeparam name="Contents1"></typeparam>
            /// <param name="tree"></param>
            /// <returns></returns>
            public static Program.Tr<Exercise1.Scp<Size, Size>> Bound<A>(Program.Tr<A> tree, Size containerSize)
            {
                return Mk<A>(tree, new Updater(LeftUpdateState)).s2scp(containerSize).lcpContents;
            }
        }

    }
}

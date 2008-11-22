using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StateMonad
{
    namespace Exercise1
    {
        // Exercise 1: generalize over the type of the state, from int
        // to <S>, say, so that the SM type can handle any kind of
        // state object. Start with Scp<T> --> Scp<S, T>, from
        // "label-content pair" to "state-content pair".

        public class Scp<State, Contents> // State-Content Pair
        {
            public State label { get; set; }
            public Contents lcpContents { get; set; } // New name; don't confuse
            // with the old "contents"
            public override string ToString()
            {
                return String.Format("Label: {0}, Contents: {1}", label, lcpContents);
            }
        }

        public delegate Scp<State, Contents> S2Scp<State, Contents>(State state);

        public delegate SM<State, Contents2> Maker<State, Contents1, Contents2>(Contents1 input);

        public class SM<State, Contents>
        {
            public S2Scp<State, Contents> s2scp { get; set; }

            public static SM<State, Contents> @return(Contents contents)
            {
                return new SM<State, Contents>
                {
                    s2scp = (st => new Scp<State, Contents>
                    {
                        label = st,
                        lcpContents = contents
                    })
                };
            }

            public static SM<State, Contents2> @bind<Contents2>(SM<State, Contents> inputMonad, Maker<State, Contents, Contents2> inputMaker)
            {
                return new SM<State, Contents2>
                {
                    // The new instance of the state monad is a
                    // function from state to state-contents pair,
                    // here realized as a C# lambda expression:

                    s2scp = (st0 =>
                    {
                        // Deconstruct the result of calling the input
                        // monad on the state parameter (done by
                        // pattern-matching in Haskell, by hand here):

                        var lcp1 = inputMonad.s2scp(st0);
                        var state1 = lcp1.label;
                        var contents1 = lcp1.lcpContents;

                        // Call the input maker on the contents from
                        // above and apply the resulting monad
                        // instance on the state from above:

                        return inputMaker(contents1).s2scp(state1);
                    })
                };
            }
        }

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

            public override string  ToString()
            {
                return X.ToString() + ", " + Y.ToString();
            }
        }

        public class Rect
        {
            public Rect(Point upperLeft, Point lowerRight)
            {
                UpperLeft = upperLeft;
                LowerRight = lowerRight;
            }

            public Point UpperLeft { get; private set; }
            public Point LowerRight { get; private set; }

            public override string ToString()
            {
                return "((" + UpperLeft.ToString() + "), (" + LowerRight.ToString() + "))";
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
            public static SM<Size, Rect> UpdateState()
            {
                return new SM<Size, Rect>
                {
                    s2scp = (size => new Scp<Size, Rect>
                    {
                        label = new Size(size.Width / 2, size.Height / 2),
                        lcpContents = new Rect(new Point(0, 0), new Point(size.Width, size.Height)) }) 
                    };
            }

            public static SM<Size, Program.Tr<Scp<Size, Rect>>> Mk<A>(Program.Tr<A> start)
            {
                if (start is Program.Lf<A>)
                {
                    var leaf = (Program.Lf<A>)start;
                    return SM<Size, Program.Tr<Scp<Size, Rect>>>.bind<Scp<Size, Rect>>
                        (UpdateState(),
                        ((contents1) =>
                            SM<Size, Program.Tr<Scp<Size, Rect>>>.@return(
                            new Program.Lf<Scp<Size, Rect>>
                            {
                                contents = new Scp<Size, Rect>
                                    {
                                        label = new Size(0, 0),
                                        lcpContents = contents1
                                    }
                            })));
                }
                else
                {
                    var branch = (Program.Br<A>)start;
                    var left = branch.left;
                    var right = branch.right;

                    return SM<Size, Program.Tr<Scp<Size, Rect>>>.bind(
                        Mk<Size, Program.Tr<A>>(left),
                        (newLeft => SM<Size, Program.Tr<Scp<Size, Rect>>>.bind<Size, Rect, Rect>(
                            (newRight => SM<MaxBBox, Program.Tr<Scp<Size, rect>>>.@return(
                                new Program.Br<Scp<Size, Rect>> {
                                    left = newLeft,
                                    right = newRight }
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
            public static Program.Tr<Scp<Size, Rect>> Bound<A>(Program.Tr<A> tree, Size containerSize)
            {
                return Mk<Size, A>(tree).s2scp(containerSize).lcpContents;
            }
        }

    }
}

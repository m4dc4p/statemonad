using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StateMonad
{
    namespace Exercise3
    {
        // Exercise 3: promote @return and @bind into an abstract
        // class "M" and make "SM" a subclass of that.

        /**
         * From Haskell Prelude
         * 
         * class Monad m where
         *   (>>=) :: m a -> (a -> m b) -> m b -- bind
         *   return :: a -> m a
         * 
         * 
         */
        
        public abstract class M<Contents1>
        {
            public delegate M<Contents2> Maker<Contents2>(Contents1 c);

            public abstract M<Contents2> @bind<Contents2>(M<Contents1> input, Maker<Contents2> maker);

            public abstract M<Contents1> @return(Contents1 contents);
        }

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

        public class SM<State, Contents1> : M<Contents1>
        {
            public S2Scp<State, Contents1> s2scp { get; set; }

            public override M<Contents2> bind<Contents2>(M<Contents1> input, Maker<Contents2> maker)
            {
                // These casts are UGLY but I can't seem to get
                // rid of them ...
                return (M<Contents2>) new SM<State, Contents2>
                {
                    // The new instance of the state monad is a
                    // function from state to state-contents pair,
                    // here realized as a C# lambda expression:

                    s2scp = (st0 =>
                    {
                        // Deconstruct the result of calling the input
                        // monad on the state parameter (done by
                        // pattern-matching in Haskell, by hand here):

                        Scp<State, Contents1> lcp1 = ((SM<State, Contents1>) input).s2scp(st0);
                        State state1 = lcp1.label;
                        Contents1 contents1 = lcp1.lcpContents;

                        // Call the input maker on the contents from
                        // above and apply the resulting monad
                        // instance on the state from above:
                        var m = maker(contents1);
                        var r = ((SM<State, Contents2>)m).s2scp(state1);
                        return r;
                    })
                };
            }

            public override M<Contents1> @return(Contents1 contents)
            {
                return new SM<State, Contents1>
                {
                    s2scp = (st => new Scp<State, Contents1>
                    {
                        label = st,
                        lcpContents = contents
                    })
                };
            }
        }

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

        public class ConstrainTree
        {
            public delegate Exercise3.SM<Size, Size> Updater();

            public static Exercise3.SM<Size, Size> RightUpdateState()
            {
                return new Exercise3.SM<Size, Size>
                {
                    s2scp = (size => new Exercise3.Scp<Size, Size>
                    {
                        label = new Size(size.Width * 2, size.Height * 2),
                        lcpContents = size
                    })
                };
            }

            public static Exercise3.SM<Size, Size> LeftUpdateState()
            {
                return new Exercise3.SM<Size, Size>
                {
                    s2scp = (size => new Exercise3.Scp<Size, Size>
                    {
                        label = new Size(size.Width / 2, size.Height / 2),
                        lcpContents = new Size(size.Width / 2, size.Height / 2)
                    })
                };
            }

            public static Exercise3.SM<Size, Program.Tr<Exercise3.Scp<Size, Size>>> Mk<A>(Program.Tr<A> tree, Updater update)
            {
                if (tree is Program.Lf<A>)
                {
                    var leaf = (Program.Lf<A>)tree;
                    return (Exercise3.SM<Size, Program.Tr<Exercise3.Scp<Size, Size>>>) 
                        (new Exercise3.SM<Size, Size>()).bind<Program.Tr<Exercise3.Scp<Size, Size>>>(update(),
                        (contents1 =>
                            (new Exercise3.SM<Size, Program.Tr<Exercise3.Scp<Size, Size>>>()).@return(
                            new Program.Lf<Exercise3.Scp<Size, Size>>
                            {
                                contents = new Exercise3.Scp<Size, Size>
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

                    return (Exercise3.SM<Size, Program.Tr<Exercise3.Scp<Size, Size>>>)
                        (new Exercise3.SM<Size, Program.Tr<Exercise3.Scp<Size, Size>>>()).bind<Program.Tr<Exercise3.Scp<Size, Size>>>(
                        Mk<A>(left, new Updater(LeftUpdateState)),
                        (newLeft => (new Exercise3.SM<Size, Program.Tr<Exercise3.Scp<Size, Size>>>()).bind<Program.Tr<Exercise3.Scp<Size, Size>>>(
                            Mk<A>(right, new Updater(RightUpdateState)),
                            (newRight => (new Exercise3.SM<Size, Program.Tr<Exercise3.Scp<Size, Size>>>()).@return(
                                new Program.Br<Exercise3.Scp<Size, Size>>
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
            public static Program.Tr<Exercise3.Scp<Size, Size>> Bound<A>(Program.Tr<A> tree, Size containerSize)
            {
                return Mk<A>(tree, new Updater(LeftUpdateState)).s2scp(containerSize).lcpContents;
            }
        }

    }
}
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
    }
}

namespace Wfc.Overlap {
    /// <summary>
    /// Index, cache of possible adjacent patterns, constraint propagator for the overalpping model
    /// </summary>
    /// <remark>
    /// The entire local similarity constraint of achieved by just ensuring adjacent overlapping patterns
    /// </remark>
    public struct AdjacencyRule {
        /// <summary>
        /// <prarag>cache :: (PatternId, PatternId, Direction) -> bool</parag>
        /// <prarag>cache[specialIndex(from, to), direction] -> isLegal where to >= from</parag>
        /// </summary>
        RectArray<bool> cache;
        int nPatterns;

        public AdjacencyRule(PatternStorage patterns, Map source) {
            int nPatterns = patterns.buffer.Count;
            int nOverlappingPatterns = (nPatterns + 1) * (nPatterns) / 2;

            this.nPatterns = nPatterns;
            this.cache = new RectArray<bool>(4, nOverlappingPatterns);

            int N = patterns.N;
            var directions = new [] { OverlappingDirection.N, OverlappingDirection.E, OverlappingDirection.S, OverlappingDirection.W };
            for (int from = 0; from < nPatterns; from++) {
                for (int to = from; to < nPatterns; to++) {
                    for (int d = 0; d < 4; d++) {
                        var dir = (OverlappingDirection) d;
                        bool isLegal = AdjacencyRule.testCompatibility(from, to, dir, patterns, source);
                        this.cache.add(isLegal);
                    }
                }
            }
        }

        static bool testCompatibility(int from, int to, OverlappingDirection dir, PatternStorage patterns, Map source) {
            int N = patterns.N;
            var fromPattern = patterns.buffer[from];
            var toPattern = patterns.buffer[to];
            for (int row = 0; row < N - 1; row++) {
                for (int col = 0; col < N; col++) {
                    // consider `from` is down and `to` is up (direction = North)
                    // get local positions in each pattern
                    var down = new Vec2(col, row);
                    var up = new Vec2(col, row + 1);
                    // rotate them to get actual positions to the left-up corners of the patterns
                    down = dir.applyAsRotation(down, N);
                    up = dir.applyAsRotation(up, N);
                    // convert them (local positions) to global positions (position in the source)
                    down = fromPattern.localPosToSourcePos(down, N);
                    up = toPattern.localPosToSourcePos(up, N);
                    // test the equaility
                    if (source[down.x, down.y] != source[up.x, up.y]) return false;
                }
            }
            return true;
        }

        /// <summary>Converts two integers into a one dimensional index</summary>
        int index(int from, int to) {
            //       to
            //     0123
            // f 0 0123
            // r 1  456
            // o 2   78
            // m 3    9
            int top = this.nPatterns;
            int bottom = nPatterns - from;
            return (top + bottom) * from / 2 + (to - from);
        }

        /// <remark>Is compaible</remark>
        public bool isLegal(PatternId from_, PatternId to_, OverlappingDirection direction) {
            int i = from_.asIndex;
            int j = to_.asIndex;
            if (i > j) {
                // swap them
                i = i + j; // a + b
                j = i - j; // (a + b) - b (=a)
                i = i - j; // (a + b) - a (=b)
            }

            return this.cache[this.index(i, j), (int) direction];
        }

        public bool isLegalRaw(PatternId from, PatternId to, OverlappingDirection d) {
            return this.cache[this.index(from.asIndex, to.asIndex), (int) d];
        }
    }
}
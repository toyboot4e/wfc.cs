namespace Wfc.Overlap {
    /// <summary>Index, cache of possible overlapping patterns</summary>
    /// <remark>
    /// The local similarity constraint is achieved by just ensuring adjacent overlapping patterns are legal
    /// </remark>
    public struct AdjacencyRule {
        RectArray<bool> cache;
        public int nPatterns;

        int index(int from, int to) {
            //       to
            //     0123
            // f 0 0123
            // r 1  456
            // o 2   78
            // m 3    9 (symmetric parts are not cached)
            //     (top + bottom) * height / 2 + remaining
            return (this.nPatterns + this.nPatterns - (from - 1)) * from / 2 + (to - from);
        }

        /// <summary>Creates an <c>AdjacencyRule</c> for the overlapping model</summary>
        public AdjacencyRule(PatternStorage patterns, Map source) {
            int nPatterns = patterns.len;
            this.nPatterns = nPatterns;

            { // not count symmetric combinations
                int nOverlappingPatterns = (nPatterns + 1) * (nPatterns) / 2;
                this.cache = new RectArray<bool>(4, nOverlappingPatterns);
            }

            for (int from = 0; from < nPatterns; from++) {
                for (int to = from; to < nPatterns; to++) {
                    for (int d = 0; d < 4; d++) {
                        var dir = (OverlappingDirection) d;
                        bool canOverlap = AdjacencyRule.testCompatibility(from, dir, to, patterns, source);
                        this.cache.add(canOverlap);
                    }
                }
            }
        }

        /// <summary>Used to create cache for the overlapping model</summary>
        static bool testCompatibility(int from, OverlappingDirection dir, int to, PatternStorage patterns, Map source) {
            int N = patterns.N; // patterns has size of NxN
            var fromPattern = patterns[from];
            var toPattern = patterns[to];
            // TODO: use row/col vector
            for (int row = 0; row < N - 1; row++) {
                for (int col = 0; col < N; col++) {
                    // consider `from` is down and `to` is up (direction = North)
                    // get local positions in each pattern
                    var downLocal = new Vec2(col, row);
                    var upLocal = new Vec2(col, row + 1);
                    // rotate them to get actual local positions (relative one to the left-up corners of the patterns)
                    downLocal = dir.applyAsRotation(downLocal, N);
                    upLocal = dir.applyAsRotation(upLocal, N);
                    // convert them (local positions) to global positions (position in the source)
                    var downGlobal = fromPattern.localPosToSourcePos(downLocal, N);
                    var upGlobal = toPattern.localPosToSourcePos(upLocal, N);
                    // test the equaility
                    if (source[downGlobal.x, downGlobal.y] != source[upGlobal.x, upGlobal.y]) return false;
                }
            }
            return true;
        }

        public bool canOverlap(PatternId from_, OverlappingDirection direction, PatternId to_) {
            int i = from_.asIndex;
            int j = to_.asIndex;
            if (i > j) {
                // swap the the patterns so that i <= j
                i = i + j; // a + b
                j = i - j; // (a + b) - b (=a)
                i = i - j; // (a + b) - a (=b)
                // and invert the direction
                direction = direction.opposite();
            }

            return this.cache[(int) direction, this.index(i, j)];
        }

        /// <remark>Can overlap</remark>
        public bool canOverlapUnsafe(PatternId from, OverlappingDirection d, PatternId to) {
            return this.cache[(int) d, this.index(from.asIndex, to.asIndex)];
        }
    }
}
namespace Wfc.Overlap {
    /// <remark>Used as an index internally</summary>
    public enum OverlapDirection {
        N = 0, // original
        E = 1, // rotate 90
        S = 2, // rotate 180 (flip y also works)
        W = 3, // rotate 270
    }

    public static class DirectionExt {
        public static Vec2 applyAsRotation(this OverlapDirection self, Vec2 v, int N) {
            return PatternVariantionExt.applyInt((int) self, N - 1, v);
        }
    }

    /// <summary>Constraint propagator, index</summary>
    /// <remark>
    /// The local similarity constraint is achieved by only ensuring adjacent patterns
    /// </remark>
    public struct AdjacencyRule {
        /// <summary>
        /// :: (int, int, Direction) -> bool
        ///
        /// cache[from, to, direction] -> isLegal where to >= from
        /// </summary>
        Array2D<bool> cache;
        int nPatterns;

        public static AdjacencyRule build(PatternStorage patterns, Map source) => new AdjacencyRule(patterns, source);

        AdjacencyRule(PatternStorage patterns, Map source) {
            int nPatterns = patterns.buffer.Count;
            int nOverlappingPatterns = (nPatterns + 1) * (nPatterns) / 2;

            this.nPatterns = nPatterns;
            this.cache = new Array2D<bool>(4, nOverlappingPatterns);

            int N = patterns.N;
            var directions = new [] { OverlapDirection.N, OverlapDirection.E, OverlapDirection.S, OverlapDirection.W };
            for (int from = 0; from < nPatterns; from++) {
                for (int to = from; to < nPatterns; to++) {
                    for (int d = 0; d < 4; d++) {
                        var direction = (OverlapDirection) d;
                        bool isLegal = testCompatibility(from, to, direction, patterns, source);
                        this.cache.add(isLegal);
                    }
                }
            }
        }

        static bool testCompatibility(int from, int to, OverlapDirection direction, PatternStorage patterns, Map source) {
            int N = patterns.N;
            var fromPattern = patterns.buffer[from];
            var toPattern = patterns.buffer[to];
            for (int row = 0; row < N; row++) {
                for (int col = 1; col < N; col++) {
                    // consider `from` is left and `to` is right (direction = East)
                    // get local positions in each pattern
                    var left = new Vec2(col, row);
                    var right = new Vec2(col - 1, row);
                    // now rotate them to get actual positions in the pattern
                    left = direction.applyAsRotation(left, N);
                    right = direction.applyAsRotation(right, N);
                    // convert local positions to global positions (position in the source)
                    left = fromPattern.localPosToSourcePos(left, N);
                    right = fromPattern.localPosToSourcePos(right, N);
                    // test the equaility
                    if (source[left.x, left.y] != source[right.x, right.y]) return false;
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
        public bool isLegal(PatternId fromId, PatternId toId, OverlapDirection direction) {
            int from = fromId.asInt;
            int to = toId.asInt;
            if (from > to) {
                // swap them (e.g. from = 3, to = 1)
                from += to; // 3 + 1 (=4)
                to = from - to; // 4 - 1 (=3)
                from -= to; // 4 - 3 (=1)
            }

            return this.cache.get(this.index(from, to), (int) direction);
        }

        public void update() {
            //
        }
    }
}
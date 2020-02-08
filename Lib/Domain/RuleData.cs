namespace Wfc {
    // TODO: use RuleDataBuilder so that fields are hidden
    /// <summary>Compatibilities of patterns. Built elsewhere</summary>
    public struct RuleData {
        public Grid2D<bool> cache; // continuous in direction, toPattern, then fromPattern
        public int nPatterns;

        bool this[int from, int dir, int to] {
            get {
                //       to
                //     0123
                // f 0 0123
                // r 1  456
                // o 2   78
                // m 3    9 (from <= to; symmetric parts are not cached)
                //     (top + bottom) * height / 2 + remaining
                int index = (this.nPatterns + this.nPatterns - (from - 1)) * from / 2 + (to - from);
                return this.cache[dir, index];
            }
        }

        public bool isLegal(PatternId from_, Dir4 dir, PatternId to_) {
            int i = from_.asIndex;
            int j = to_.asIndex;
            if (i > j) {
                // swap the the patterns so that i <= j
                i = i + j; // a + b
                j = i - j; // (a + b) - b (=a)
                i = i - j; // (a + b) - a (=b)
                // and invert the direction
                dir = dir.opposite();
            }

            return this[i, (int) dir, j];
        }

        /// <remark>Ensure from <= to</remark>
        public bool isLegalUnsafe(PatternId from, Dir4 d, PatternId to) {
            return this[from.asIndex, (int) d, to.asIndex];
        }

        public static PatternStorage extractEveryPattern(ref Map source, int N, PatternVariation[] variations) {
            var patterns = new PatternStorage(source, N);
            var nVariations = variations.Length;

            // TODO: handling periodic input
            for (int y = 0; y < source.height - N + 1; y++) {
                for (int x = 0; x < source.width - N + 1; x++) {
                    for (int i = 0; i < nVariations; i++) {
                        patterns.store(x, y, variations[i]);
                    }
                }
            }

            return patterns;
        }

        public static PatternStorage extractEveryChunk(ref Map source, int N, PatternVariation[] variations) {
            if (source.width % N != 0 || source.height % N != 0) {
                throw new System.Exception($"source size ({source.width}, {source.height}) must be dividable with N={N}");
            }

            var patterns = new PatternStorage(source, N);
            var nVariations = variations.Length;
            var gridSize = new Vec2i(source.width, source.height) / N;

            for (int y = 0; y < gridSize.y; y++) {
                for (int x = 0; x < gridSize.x; x++) {
                    for (int i = 0; i < nVariations; i++) {
                        patterns.store(x * N, y * N, variations[i]);
                    }
                }
            }

            return patterns;
        }
    }
}
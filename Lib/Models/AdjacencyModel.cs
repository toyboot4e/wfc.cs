namespace Wfc {
    public static class AdjacencyModel {
        /// <summary>Used to create cache for the overlapping model</summary>
        public static bool testCompatibility(int from, Dir4 dir, int to, PatternStorage patterns, Map source) {
            int N = patterns.N; // patterns have size of NxN
            var fromPattern = patterns[from];
            var toPattern = patterns[to];
            // TODO: use row/col vector for performance

            int row = 0;
            bool anyExitDown = false;
            bool anyExitUp = false;
            for (int col = 0; col < N; col++) {
                // Now, consider a concrete situation: `from` is down and `to` is up (direction = North)
                // get local positions in each pattern
                var downLocal = new Vec2i(col, row);
                var upLocal = new Vec2i(col, row + 1);
                // apply the direction
                downLocal = dir.applyAsRotation(downLocal, N);
                upLocal = dir.applyAsRotation(upLocal, N);
                // convert them (local positions) into global positions (position in the source)
                var downGlobal = fromPattern.localPosToSourcePos(downLocal, N);
                var upGlobal = toPattern.localPosToSourcePos(upLocal, N);
                // check compatibilities
                bool down = source[downGlobal.x, downGlobal.y] == Tile.Floor;
                bool up = source[downGlobal.x, downGlobal.y] == Tile.Floor;

                if (down && up) return true;
                anyExitDown &= down;
                anyExitUp &= up;
            }

            // if one of them has no exits, we enable the pattern
            return !anyExitDown && !anyExitUp;
        }

        /// <summary>`source` size must be dividable by `N`</summary>
        public static Model create(ref Map source, int N, Vec2i outputSize) {
            if (outputSize.x % N != 0 || outputSize.y % N != 0) {
                throw new System.Exception($"output size {outputSize} must be dividable with N={N}");
            }

            var gridSize = outputSize / N;
            var patterns = RuleData.extractEveryChunk(ref source, N, PatternUtil.variations);
            var rule = AdjacencyModel.buildRule(patterns, ref source);
            return new Model(gridSize, patterns, rule);
        }

        /// <summary>Creates an <c>AdjacencyRule</c> for the adjacency model</summary>
        public static RuleData buildRule(PatternStorage patterns, ref Map source) {
            var rule = new RuleData();

            int nPatterns = patterns.len;
            rule.nPatterns = nPatterns;

            { // do not count symmetric combinations
                int nCombinations = (nPatterns + 1) * (nPatterns) / 2;
                rule.cache = new Grid2D<bool>(4, nCombinations);
            }

            for (int from = 0; from < nPatterns; from++) {
                for (int to = from; to < nPatterns; to++) {
                    for (int d = 0; d < 4; d++) {
                        var dir = (Dir4) d;
                        bool canBeAdjacenct = AdjacencyModel.testCompatibility(from, dir, to, patterns, source);
                        rule.cache.add(canBeAdjacenct);
                    }
                }
            }

            return rule;
        }
    }
}
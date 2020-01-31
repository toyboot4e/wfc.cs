namespace Wfc {
    public static class AdjacencyModel {
        public static Model create(ref Map source, int N, Vec2i outputSize) {
            if (outputSize.x % N != 0 || outputSize.y % N != 0) {
                throw new System.Exception($"output size {outputSize} is indivisible by N={N}");
            }
            var gridSize = outputSize / N;
            var patterns = RuleData.extractPatterns(ref source, N);
            var rule = AdjacencyModel.buildRule(patterns, ref source);
            return new Model(gridSize, patterns, rule);
        }

        /// <summary>Creates an <c>AdjacencyRule</c> for the overlapping model</summary>
        public static RuleData buildRule(PatternStorage patterns, ref Map source) {
            var rule = new RuleData();

            int nPatterns = patterns.len;
            rule.nPatterns = nPatterns;

            { // not count symmetric combinations
                int nOverlappingPatterns = (nPatterns + 1) * (nPatterns) / 2;
                rule.cache = new Grid2D<bool>(4, nOverlappingPatterns);
            }

            for (int from = 0; from < nPatterns; from++) {
                for (int to = from; to < nPatterns; to++) {
                    for (int d = 0; d < 4; d++) {
                        var dir = (Dir4) d;
                        bool canOverlap = AdjacencyModel.testCompatibility(from, dir, to, patterns, source);
                        rule.cache.add(canOverlap);
                    }
                }
            }

            return rule;
        }

        /// <summary>Used to create cache for the overlapping model</summary>
        public static bool testCompatibility(int from, Dir4 dir, int to, PatternStorage patterns, Map source) {
            return true;
        }
    }
}
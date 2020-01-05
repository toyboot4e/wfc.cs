using System.Linq;

namespace Wfc.Overlap {
    /// <summary>States of output map</summary>
    /// <remark>Wave</remark>
    public class State {
        /// <summary>(x, y, direction) -> isCompatible</summary>
        /// <remark>A pattern is compatible for a cell if it has more than one enabler in every direction</remark>
        CuboidArray<bool> compatibilities;
        /// <summary>(specialIndex(int, int), PatternId, OverlappingDirection) -> bool</summary>
        public EnablerCounter enablerCounts;
        public RectArray<EntropyCacheData> entropies;

        /// <summary>Util</summary>
        public Vec2 outputSize;

        public State(int width, int height, PatternStorage patterns, ref AdjacencyRule rule) {
            this.outputSize = new Vec2(width, height);
            int nPatterns = patterns.len;
            this.compatibilities = new CuboidArray<bool>(width, height, nPatterns);
            this.enablerCounts = EnablerCounter.initial(width, height, patterns, ref rule);
            this.entropies = new RectArray<EntropyCacheData>(width, height);

            var cache = EntropyCacheData.fromPatterns(patterns);
            for (int i = 0; i < width * height; i++) {
                this.entropies.add(cache);
                for (int j = 0; j < nPatterns; j++) {
                    this.compatibilities.add(true);
                }
            }
        }

        public bool isCompatible(int x, int y, PatternId id) => this.compatibilities[x, y, id.asIndex];

        /// <summary>Set a flag that the cell is locked into a pattern</summary>
        public void onDecidePattern(int x, int y) {
            var newCache = this.entropies[x, y];
            newCache.isDecided = true;
            this.entropies[x, y] = newCache;
        }

        public void removePattern(int x, int y, PatternId id_) {
            this.compatibilities[x, y, id_.asIndex] = false;
        }

        /// <remark>Never forget to check for contradiction after calling this</remark>
        public void removePatternUpdatingEntropy(int x, int y, PatternId id_, PatternStorage patterns) {
            var id = id_.asIndex;
            this.compatibilities[x, y, id] = false;
            var weight = patterns[id].weight;

            var cache = this.entropies[x, y];
            cache.onReduce(weight);
            this.entropies[x, y] = cache;
        }

        #region output
        public PatternId? patternIdAt(int x, int y, int nPatterns) {
            var w = this.outputSize.x;
            var h = this.outputSize.y;
            if (!this.entropies[x, y].isDecided) {
                return null;
            }
            var offsetOfIndex = nPatterns * (x + w * y);
            var index = this.compatibilities.items.FindIndex(offsetOfIndex, nPatterns, x => x);
            if (index == -1) {
                System.Console.WriteLine($"patternAt(): ERROR: collapsed but not found at ({x}, {y})");
                return null;
            }
            return new PatternId(index - offsetOfIndex);
        }

        public Map getOutput(int outputW, int outputH, Map source, int N, PatternStorage patterns) {
            int nPatterns = patterns.len;
            var map = new Map(outputW, outputH);
            System.Console.WriteLine($"num of remaining legal patterns: {this.compatibilities.items.Where(x => x).Count()}");
            for (int i = 0; i < outputW * outputH; i++) {
                int x = i % outputW;
                int y = i / outputW;
                var patternId = this.patternIdAt(x, y, nPatterns);
                if (patternId == null) {
                    map.tiles.add(Tile.None);
                    continue;
                }
                var pattern = patterns[((PatternId) patternId).asIndex];
                var sourcePos = pattern.localPosToSourcePos(new Vec2(0, 0), N);
                var tile = source[sourcePos.x, sourcePos.y];
                map.tiles.add(tile);
            }
            return map;
        }
        #endregion
    }
}
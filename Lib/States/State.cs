using System.Linq;

namespace Wfc.Overlap {
    /// <summary>States of output map</summary>
    /// <remark>Wave</remark>
    public class State {
        /// <summary>Still legal / compatible to neighbors. Enabled by neighbors. isLegal :: (int, int, PatternId) -> bool.</summary>
        /// <remark>A pattern is legal if it has more than one enabler in every direction</remark>
        public CuboidArray<bool> legalities;
        /// <remark>enablerCounts :: (specialIndex(int, int), PatternId, OverlappingDirection) -> bool</remark>
        public EnablerCounter enablerCounts;
        public RectArray<EntropyCacheData> entropies;

        public State(int width, int height, PatternStorage patterns, AdjacencyRule rule) {
            int nPatterns = patterns.buffer.Count;
            this.legalities = new CuboidArray<bool>(width, height, nPatterns);
            this.enablerCounts = EnablerCounter.initial(width, height, patterns, rule);
            this.entropies = new RectArray<EntropyCacheData>(width, height);

            var cache = EntropyCacheData.fromPatterns(patterns);
            for (int i = 0; i < width * height; i++) {
                this.entropies.add(cache);
                for (int j = 0; j < nPatterns; j++) {
                    this.legalities.add(true);
                }
            }
        }

        public Map getOutput(int w, int h, Map source, int N, PatternStorage patterns) {
            int nPatterns = patterns.buffer.Count;
            var map = new Map(w, h);
            for (int i = 0; i < w * h; i++) {
                int x = i % w;
                int y = i / w;
                var tile = Tile.None;
                if (this.entropies[x, y].isDecided) {
                    var pattern = patterns.buffer[this.legalities.items.FindIndex(0, nPatterns, (_) => true)];
                    var sourcePos = pattern.localPosToSourcePos(new Vec2(0, 0), N);
                    tile = source[sourcePos.x, sourcePos.y];
                }
                map.tiles.add(tile); //patterns.buffer[i].tileAt(x, y, N, source));
            }
            return map;
        }

        public void removePattern(int x, int y, PatternId id_) {
            this.legalities[x, y, id_.asIndex] = false;
        }

        /// <remark>Never forget to check for contradiction after calling this</remark>
        public void removePatternUpdatingEntropy(int x, int y, PatternId id_, PatternStorage patterns) {
            var id = id_.asIndex;
            this.legalities[x, y, id] = false;
            var weight = patterns.buffer[id].weight;

            var cache = this.entropies[x, y];
            cache.onReduce(weight);
            this.entropies[x, y] = cache;
        }
    }
}
using System.Linq;

namespace Wfc.Overlap {
    /// <summary>States of output map</summary>
    /// <remark>Wave</remark>
    public class State {
        /// <summary>isLegal :: (int, int, PatternId) -> bool.</summary>
        /// <remark>A pattern is legal if it has more than one enabler in every direction</remark>
        CuboidArray<bool> legalities;
        /// <remark>enablerCounts :: (specialIndex(int, int), PatternId, OverlappingDirection) -> bool</remark>
        public EnablerCounter enablerCounts;
        public RectArray<EntropyCacheData> entropies;

        public State(int width, int height, PatternStorage patterns, AdjacencyRule rule) {
            int nPatterns = patterns.len;
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

        public Map getOutput(int outputW, int outputH, Map source, int N, PatternStorage patterns) {
            int nPatterns = patterns.len;
            var map = new Map(outputW, outputH);
            System.Console.WriteLine($"num of remaining legal patterns: {this.legalities.items.Where(x => x).Count()}");
            for (int i = 0; i < outputW * outputH; i++) {
                int x = i % outputW;
                int y = i / outputW;
                var tile = Tile.None;
                if (this.entropies[x, y].isDecided) {
                    var offsetIndex = nPatterns * (x + y * outputW);
                    var index = this.legalities.items.FindIndex(offsetIndex, nPatterns, (x) => x);
                    if (index == -1) {
                        System.Console.WriteLine($"ERROR: collapsed but not found at ({x}, {y})");
                        map.tiles.add(Tile.None);
                        continue;
                    }
                    index -= offsetIndex;
                    // System.Console.Write($"{index} ");
                    var pattern = patterns[index];
                    var sourcePos = pattern.localPosToSourcePos(new Vec2(0, 0), N);
                    tile = source[sourcePos.x, sourcePos.y];
                } else {
                    // System.Console.WriteLine($"Not collapsed: {x}, {y}");
                }
                map.tiles.add(tile); //patterns.buffer[i].tileAt(x, y, N, source));
            }
            return map;
        }

        public bool isLegal(int x, int y, PatternId id) => this.legalities[x, y, id.asIndex];

        public void removePattern(int x, int y, PatternId id_) {
            this.legalities[x, y, id_.asIndex] = false;
        }

        /// <remark>Never forget to check for contradiction after calling this</remark>
        public void removePatternUpdatingEntropy(int x, int y, PatternId id_, PatternStorage patterns) {
            var id = id_.asIndex;
            this.legalities[x, y, id] = false;
            var weight = patterns[id].weight;

            var cache = this.entropies[x, y];
            cache.onReduce(weight);
            this.entropies[x, y] = cache;
        }
    }
}
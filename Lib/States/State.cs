using System.Linq;

namespace Wfc.Overlap {
    /// <remark>Wave. Grid of states and caches for each cell</remark>
    public class State {
        public EnablerCounter enablerCounts;
        CuboidArray<bool> possibilities;
        public RectArray<EntropyCacheForCell> entropies;

        /// <summary>Just for utility</summary>
        public Vec2 outputSize;

        public State(int width, int height, PatternStorage patterns, ref Rule rule) {
            this.outputSize = new Vec2(width, height);
            int nPatterns = patterns.len;
            this.possibilities = new CuboidArray<bool>(width, height, nPatterns);
            this.enablerCounts = EnablerCounter.initial(width, height, patterns, ref rule);
            this.entropies = new RectArray<EntropyCacheForCell>(width, height);

            var initialEntropyCache = EntropyCacheForCell.initial(patterns);
            for (int i = 0; i < width * height; i++) {
                this.entropies.add(initialEntropyCache);
                for (int j = 0; j < nPatterns; j++) {
                    this.possibilities.add(true);
                }
            }
        }

        /// <summary>Is the pattern still compaible</summary>
        public bool isPossible(int x, int y, PatternId id) => this.possibilities[x, y, id.asIndex];

        /// <summary>Set a flag</summary>
        public void onLockinPattern(int x, int y, int weight) {
            var newCache = this.entropies[x, y];
            newCache.isDecided = true;
            newCache.totalWeight = weight; // update the total weight so that contradiction can be detected on a propagation
            this.entropies[x, y] = newCache;
        }

        public void removePattern(int x, int y, PatternId id) {
            this.possibilities[x, y, id.asIndex] = false;
        }

        /// <remark>Returns if the cell is contradicted</remark>
        public bool removePatternUpdatingEntropy(int x, int y, PatternId id, PatternStorage patterns) {
            this.possibilities[x, y, id.asIndex] = false;

            var cache = this.entropies[x, y];
            cache.reduceWeight(patterns[id.asIndex].weight);
            this.entropies[x, y] = cache;

            return cache.totalWeight == 0;
        }

        #region output
        public PatternId? patternIdAt(int x, int y, int nPatterns) {
            if (!this.entropies[x, y].isDecided) return null;

            var w = this.outputSize.x;
            var h = this.outputSize.y;

            var offsetOfIndex = nPatterns * (x + w * y);
            var index = this.possibilities.items.FindIndex(offsetOfIndex, nPatterns, x => x);
            if (index == -1) {
                System.Console.WriteLine($"patternAt(): ERROR: \"collapsed\" but no pattern found at ({x}, {y})");
                return null;
            }
            return new PatternId(index - offsetOfIndex);
        }

        public Map getOutput(int outputW, int outputH, Map source, int N, PatternStorage patterns) {
            int nPatterns = patterns.len;
            var map = new Map(outputW, outputH);
            for (int i = 0; i < outputW * outputH; i++) {
                int x = i % outputW;
                int y = i / outputW;
                var patternId = this.patternIdAt(x, y, nPatterns);
                if (patternId == null) {
                    map.tiles.add(Tile.None);
                    continue;
                }
                var pattern = patterns[((PatternId) patternId).asIndex];

                // left-up corner of the pattern is used for the output
                var sourcePos = pattern.localPosToSourcePos(new Vec2(0, 0), N);
                var tile = source[sourcePos.x, sourcePos.y];
                map.tiles.add(tile);
            }

            // TODO: debug print
            // it must be same as `outputW * outputH`
            // System.Console.WriteLine($"num of compatible patterns: {this.possibilities.items.Where(x => x).Count()}");
            return map;
        }

        public void printAvaiablePatternCounts(Vec2 outputSize, int nPatterns) {
            for (int y = 0; y < outputSize.y; y++) {
                for (int x = 0; x < outputSize.x; x++) {

                    int n = 0;
                    for (int i = 0; i < nPatterns; i++) {
                        if (this.possibilities[x, y, i]) n += 1;
                    }

                    System.Console.Write($"{n,2} ");
                }
                System.Console.WriteLine("");
            }
        }
        #endregion
    }
}
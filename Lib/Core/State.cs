using System.Linq;

namespace Wfc {
    /// <remark>Wave. Grid of states and caches for each cell</remark>
    public class State {
        /// <summary>Size of the grid the <c>State</c> manages</summary>
        public Vec2i gridSize;
        /// <summary>Remaning possible patterns per cell</summary>
        Grid3D<bool> possibilities;
        /// <summary>Cache of the entropy heuristics</summary>
        public Grid2D<EntropyCacheForCell> entropies;
        /// <summary>Cache to propagate compatibility constraints</summary>
        public EnablerCounter enablerCounts;

        public State(int width, int height, PatternStorage patterns, ref RuleData rule) {
            int nPatterns = patterns.len;
            this.gridSize = new Vec2i(width, height);

            this.possibilities = new Grid3D<bool>(width, height, nPatterns);
            this.enablerCounts = EnablerCounter.initial(width, height, patterns, ref rule);
            this.entropies = new Grid2D<EntropyCacheForCell>(width, height);

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
        public void solveCellWithPattern(int x, int y, int weight) {
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

            var w = this.gridSize.x;
            var h = this.gridSize.y;

            var offsetOfIndex = nPatterns * (x + w * y);
            var index = this.possibilities.items.FindIndex(offsetOfIndex, nPatterns, x => x);
            if (index == -1) {
                System.Console.WriteLine($"patternAt(): ERROR: \"collapsed\" but no pattern found at ({x}, {y})");
                return null;
            }
            return new PatternId(index - offsetOfIndex);
        }

        public void printAvaiablePatternCounts(int nPatterns) {
            for (int y = 0; y < this.gridSize.y; y++) {
                for (int x = 0; x < this.gridSize.x; x++) {

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
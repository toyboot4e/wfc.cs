using System;

namespace Wfc.Overlap {
    /// <summary>Heap of cells used to pick up one with least entropy</summary>
    /// <remark>Add some noise to make random choices</remark>
    public struct CellHeap {
        MinHeap<PosWithEntropy> buffer;

        public CellHeap(int capacity) {
            this.buffer = new MinHeap<PosWithEntropy>(capacity);
        }

        public bool hasAnyElement() {
            return this.buffer.xs.Count > 0;
        }

        public void add(int x, int y, double entropy) {
            this.buffer.add(new PosWithEntropy(x, y, entropy));
        }

        public PosWithEntropy pop() {
            return this.buffer.pop();
        }

        // TODO: is this fast? (boxing not happening?)
        ///<renark>Never use it without noise</remark>
        public struct PosWithEntropy : System.IComparable<PosWithEntropy> {
            public int x;
            public int y;
            public double entropy;

            public PosWithEntropy(int x, int y, double entropy) {
                this.x = x;
                this.y = y;
                this.entropy = entropy;
            }

            public int CompareTo(PosWithEntropy other) {
                return this.entropy - other.entropy > 0 ? 1 : -1;
            }
        }
    }

    // TODO: consider making it a struct (indexer doesn't require copy, does it?)
    /// <summary>(int, int, PatternId, OverlappingDirection) -> int</summary>
    public class EnablerCounter {
        int width;
        CuboidArray<int> counts;

        EnablerCounter(int width, int height, int nPatterns) {
            this.width = width;
            this.counts = new CuboidArray<int>(width, height, 4 * nPatterns);
        }

        public int this[int x, int y, PatternId id, OverlappingDirection dir] {
            get => this.counts[x, y, (int) dir + 4 * id.asIndex];
            set => this.counts[x, y, (int) dir + 4 * id.asIndex] = value;
        }

        /// <summary>Returns if the pattern get disabled</summary>
        public bool decrement(int x, int y, PatternId id, OverlappingDirection direction) {
            bool isPatternAlreadyDisabled = this.isPatternDisabled(x, y, id);
            this[x, y, id, direction] -= 1;
            return !isPatternAlreadyDisabled && this[x, y, id, direction] == 0;
        }

        /// <summary>
        /// No enabler in any direction
        /// </summary>
        bool isPatternDisabled(int x, int y, PatternId id) {
            for (int d = 0; d < 4; d++) {
                if (this[x, y, id, (OverlappingDirection) d] == 0) return true;
            }
            return false;
        }

        public static EnablerCounter initial(int width, int height, PatternStorage patterns, ref AdjacencyRule rule) {
            int nPatterns = patterns.len;
            var self = new EnablerCounter(width, height, nPatterns);

            // fill the buffer
            for (int i = 0; i < width * height * 4 * nPatterns; i++) {
                self.counts.add(0);
            }

            // store initial enabler counts at (0, 0):
            for (int id_ = 0; id_ < nPatterns; id_++) {
                var id = new PatternId(id_);
                // sum up enablers for the pattern in each direction
                for (int otherId = 0; otherId < nPatterns; otherId++) {
                    for (int d = 0; d < 4; d++) {
                        var dir = (OverlappingDirection) d;
                        if (rule.canOverlap(id, dir, new PatternId(otherId))) {
                            self[0, 0, id, dir] += 1;
                        }
                    }
                }
            }

            // copy it to other cells
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    for (int id_ = 0; id_ < nPatterns; id_++) {
                        var id = new PatternId(id_);
                        for (int d = 0; d < 4; d++) {
                            var dir = (OverlappingDirection) d;
                            self[x, y, id, dir] = self[0, 0, id, dir];
                        }
                    }
                }
            }

            return self;
        }
    }

    // TODO: use float
    public struct EntropyCacheForCell {
        /// <remark>Is collapsed</summary>
        public bool isDecided;
        /// <summary>
        /// Sum of weights of remaining patterns. Used to calculate entropy / check if a cell is contradicted
        /// </summary>
        /// <remark>Must be tracked even when a cell is locked into a pattern</remark>
        public int totalWeight;
        // TODO: remove entropy
        /// <summary>sum [weight_i * log_2(weight_i)]</summary>
        double cachedExpr;

        public static EntropyCacheForCell initial(PatternStorage patterns) {
            int nPatterns = patterns.len;

            int totalWeight = 0;
            double entropyCache = 0;

            for (int i = 0; i < nPatterns; i++) {
                var weight = patterns[i].weight;
                totalWeight += weight;
                entropyCache += weight * Math.Log2(weight);
            }

            return new EntropyCacheForCell {
                isDecided = false,
                    totalWeight = totalWeight,
                    cachedExpr = entropyCache,
            };
        }

        /// <summray>Information entropy of a cell with noise for random picking</summary>
        /// <remark>
        /// S(X) = -sum [p_i * log_2(p_i)] in statics.
        /// S(X) = log_2(totalWeight) - sum [weight_i * log_2(weight_i)] / totalWeight.
        /// </remark>
        public double entropyWithNoise() {
            return Math.Log2(this.totalWeight) - this.cachedExpr / this.totalWeight + noise();
        }

        static Random random = new Random();
        double noise() => random.NextDouble() * 1E-6;

        /// <summary>Used to updates caches for entropy (which is for random picking)</summary>
        public void reduceWeight(int weight) {
            this.totalWeight -= weight;
            this.cachedExpr -= (double) weight * (double) Math.Log(this.totalWeight, 2);
        }
    }
}
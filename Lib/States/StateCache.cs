using System;

namespace Wfc.Overlap {
    /// <summary>Used to pick up a cell with least entropy</summary>
    /// <remark>Add some noise to make choices random</remark>
    public class Heap {
        MinHeap<PosWithEntropy> buffer;

        public Heap(int capacity) {
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

        // TODO: is this fast in MinMap?
        public struct PosWithEntropy : System.IComparable<PosWithEntropy> {
            public int x;
            public int y;
            public double entropy;

            public PosWithEntropy(int x, int y, double entropy) {
                this.x = x;
                this.y = y;
                this.entropy = entropy;
            }

            // FIXME: never use it without noise
            public int CompareTo(PosWithEntropy other) {
                return this.entropy - other.entropy > 0 ? 1 : -1;
            }
        }
    }

    public class EnablerCounter {
        // TODO: stackalloc or better int -> enum conversion
        static OverlappingDirection[] dirs = new [] { OverlappingDirection.N, OverlappingDirection.E, OverlappingDirection.S, OverlappingDirection.W };

        int width;
        CuboidArray<int> counts;

        EnablerCounter(int width, int height, int nPatterns) {
            this.width = width;
            this.counts = new CuboidArray<int>(width, height, nPatterns);
        }

        int index(PatternId id, OverlappingDirection dir) {
            return (int) dir + 4 * id.asIndex;
        }

        public int this[int x, int y, PatternId id, OverlappingDirection dir] {
            get => this.counts[x, y, this.index(id, dir)];
            set => this.counts[x, y, this.index(id, dir)] = value;
        }

        public void decrement(int x, int y, PatternId id, OverlappingDirection dir) {
            this[x, y, id, dir] -= 1;
        }

        public bool anyZeroEnablerFor(int x, int y, PatternId id) {
            for (int d = 0; d < 4; d++) {
                if (this[x, y, id, (OverlappingDirection) d] == 0) return true;
            }
            return false;
        }

        public static EnablerCounter initial(int width, int height, PatternStorage patterns, AdjacencyRule rule) {
            int nPatterns = patterns.len;
            var self = new EnablerCounter(width, height, nPatterns);

            // TODO: make it no need to add
            for (int i = 0; i < width * height * 4 * nPatterns; i++) {
                self.counts.add(0);
            }

            // first, let's count enablers in (0, 0) with the adjacency rule:
            for (int id = 0; id < nPatterns; id++) {
                // sum up enablers for each adjacency rule with direction
                for (int otherId = id; otherId < nPatterns; otherId++) {
                    for (int d = 0; d < 4; d++) {
                        int index = d + 4 * id;
                        if (rule.isLegalSafe(new PatternId(id), new PatternId(otherId), dirs[d])) {
                            self.counts[0, 0, index] += 1;
                        }
                    }
                }
            }

            // copy it to other cells
            // TODO: more performance with indexing
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    for (int id = 0; id < nPatterns; id++) {
                        for (int d = 0; d < 4; d++) {
                            int index = d + 4 * id;
                            self.counts[x, y, index] = self.counts[0, 0, index];
                        }
                    }
                }
            }

            return self;
        }
    }

    // TODO: use float
    // TODO: export entropy expression
    public struct EntropyCacheData {
        /// <remark>Is this cell collapsed</summary>
        public bool isDecided;
        /// <summary>Sum of weights of remaining patterns</summary>
        public int totalWeight;
        // TODO: remove entropy
        /// <summary>sum [weight_i * log(weight_i)]</summary>
        double cachedExpr;
        static Random random = new Random();

        /// <summary>Creates initial <c>EntropyCache</c> for the <c>patterns</c></summary>
        public static EntropyCacheData fromPatterns(PatternStorage patterns) {
            int nPatterns = patterns.len;

            int totalWeight = 0;
            double entropyCache = 0;

            for (int i = 0; i < nPatterns; i++) {
                var weight = patterns[i].weight;
                totalWeight += weight;
                entropyCache += weight * Math.Log(weight, 2);
            }

            return new EntropyCacheData {
                totalWeight = totalWeight,
                    cachedExpr = entropyCache,
            };
        }

        /// <summray>Information entropy of a cell</summary>
        /// <remark>
        /// S(X) = -sum [p_i * log(p_i)] in statics
        /// S(X) = log(totalWeight) - sum [weight_i * log(weight_i)] / totalWeight
        /// where base of log = 2
        /// </remark>
        public double entropyWithNoise() {
            return Math.Log(this.totalWeight, 2) - this.cachedExpr / this.totalWeight + random.NextDouble() * 1E-6;
        }

        /// <summary>Updates self (<c>EntropyCacheData</c>)</summary>
        public void onReduce(int weight) {
            this.totalWeight -= weight;
            this.cachedExpr -= (double) weight * (double) Math.Log(this.totalWeight, 2);
        }
    }
}
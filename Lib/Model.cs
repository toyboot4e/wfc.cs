using System.Collections.Generic;
using System.Diagnostics;

namespace Wfc.Overlap {
    /// <summary>
    /// Forces the local similarity constraint: any NxN pattern in the <c>output</c>
    /// can be found in the <c>source</c>. Patterns are extracted from the <c>source</c>
    /// considering their variants (flippings and rotations).
    /// </summary>
    public class Model {
        public Input input;
        public PatternStorage patterns;
        public AdjacencyRule rule;

        public State state;
        public Heap heap;
        public Stack<TileRemoval> removals;
        public int nRemainings;
        System.Random random = new System.Random();

        public struct Input {
            public Map source;
            public int N;
            public Vec2 outputSize;
        }

        public Model(Input input) {
            Debug.Assert(input.N >= 2, $"each pattern must be greater than or equal to  2x2 (N = {input.N})");
            this.input = input;
            var size = input.outputSize;
            this.patterns = Model.extractPatterns(input.source, input.N);
            this.rule = new AdjacencyRule(this.patterns, input.source);
            this.state = new State(size.x, size.y, this.patterns, this.rule);
            // TODO: initial heap elements
            this.heap = new Heap(10);
            for (int y = 0; y < size.y; y++) {
                for (int x = 0; x < size.x; x++) {
                    heap.add(x, y, this.state.entropies[x, y].entropyWithNoise());
                }
            }
            this.removals = new Stack<TileRemoval>(10);
            // TODO: track the number of remainings
            this.nRemainings = size.area;
        }

        public struct TileRemoval {
            public int x;
            public int y;
            /// <summary>The cell is locked in to this <c>PatternId</c></summary>
            public PatternId id;

            public TileRemoval(int x, int y, PatternId id) {
                this.x = x;
                this.y = y;
                this.id = id;
            }
        }

        /// <summary>
        /// Extracts every NxN pattern in the source considering their rotations and flippings
        /// </summary>
        static PatternStorage extractPatterns(Map source, int N) {
            var patterns = new PatternStorage(source, N);
            var variations = PatternUtil.variations; // TODO: use fixed or stackalloc
            var nVariations = variations.Length;

            for (int y = 0; y < source.height - N + 1; y++) {
                for (int x = 0; x < source.width - N + 1; x++) {
                    for (int i = 0; i < nVariations; i++) {
                        patterns.add(x, y, variations[i]);
                    }
                }
            }

            return patterns;
        }

        /// <summary>
        /// Tries to solve the constraint satisfication problem with the observe-propagate loop
        /// </summary>
        public void run() {
            while (true) {
                switch (this.advance()) {
                    case AdvanceStatus.Continue:
                        continue;
                    case AdvanceStatus.Success:
                        System.Console.WriteLine("SUCCESS");
                        return;
                    case AdvanceStatus.Fail:
                        System.Console.WriteLine("FAIL");
                        return;
                }
            }
        }

        public enum AdvanceStatus {
            /// <summary>Every cell is filled in respect of the <c>AdjacencyRule</c></summary>
            Success,
            /// <summary>A contradiction is reached, where any cell has no possible pattern</summary>
            Fail,
            /// <summary>Just in proress</summary>
            Continue,
        }

        /// <summary>
        /// Advances the <c>State</c>: select next cell, decide a pattern for it, and propagate the change
        /// </summary>
        public AdvanceStatus advance() {
            var(pos, isOnContradiction) = selectNextCell(this);
            if (isOnContradiction) return AdvanceStatus.Fail;
            var id = selectPatternInCell(this, pos.x, pos.y, this.state.entropies[pos.x, pos.y].totalWeight);
            decidePatternForCell(this, pos.x, pos.y, id);
            return this.afterDecide();

            // * local functions *

            /// <summary>
            /// Select one of the cells with least total weight. Cell with least uncernity
            /// </summary>
            /// <remark>The intent is to minimizing the risk of contradiction</summary>
            static(Vec2, bool) selectNextCell(Model self) {
                while (self.heap.anyElement()) {
                    var cell = self.heap.pop();
                    if (self.state.entropies[cell.x, cell.y].isDecided) continue;
                    return (new Vec2(cell.x, cell.y), true);
                }
                System.Console.WriteLine("Unreachable. The heap is empty, but there are still remaining cells to decide their patterns.");
                return (new Vec2(-1, -1), false);
            }

            /// <summary>Choose a random pattern in a cell in respect of weights of patterns</summary>
            static PatternId selectPatternInCell(Model self, int x, int y, int totalWeight) {
                int random = self.random.Next(0, totalWeight); // [0, totalWeight)

                int weight = 0;
                int n = self.patterns.buffer.Count;
                for (int index = 0; index < n; index++) {
                    if (!self.state.legalities[x, y, index]) continue;
                    weight += self.patterns.buffer[index].weight;
                    if (weight > random) return new PatternId(index);
                }

                System.Console.WriteLine("EEEEEEEEEEEEEEEEERRRRRRRRRRRRRRRROOOOOOOOOOOOORRRR");
                return new PatternId(-1);
            }

            /// <remark>Collapse/lockin</remark>
            static void decidePatternForCell(Model self, int x, int y, PatternId id) {
                // TODO: maybe use Span<T> to modify a struct in a List<T>?
                var index = self.state.entropies.index(x, y);

                {
                    var newCache = self.state.entropies.items[index];
                    newCache.isDecided = true;
                    self.state.entropies.items[index] = newCache;
                }

                // remove all other possibilities
                var n = self.patterns.buffer.Count;
                for (int i = 0; i < n; i++) {
                    if (self.state.legalities[x, y, i] == false || i == index) continue;
                    self.state.removePattern(x, y, id); // no need to update the entropy, which if for randomly selecting undecided cell
                    self.removals.Push(new TileRemoval(x, y, new PatternId(i)));
                }
            }
        }

        // TODO: use fixed, stackalloc or int to enum
        static OverlappingDirection[] dirs = new [] { OverlappingDirection.N, OverlappingDirection.E, OverlappingDirection.S, OverlappingDirection.W };
        static(int, int) [] dirVecs = new [] {
            (0, -1), (1, 0), (0, 1), (-1, 0)
        };

        /// <summary>Propates the <c>AdjacencyRule</c>.</summary>
        AdvanceStatus afterDecide() {
            int nPatterns = this.patterns.buffer.Count;
            while (this.removals.Count > 0) {
                var removal = this.removals.Pop();
                // for each neighbor (= for each tiles in the direction) where it is compatible with the removed pattern
                for (int dirIndex = 0; dirIndex < 4; dirIndex++) {
                    var dir = dirs[dirIndex];
                    int x = removal.x + dirVecs[dirIndex].Item1;
                    int y = removal.y + dirVecs[dirIndex].Item2;
                    for (int other = 0; other < nPatterns; other++) {
                        var otherId = new PatternId(other);
                        if (!this.rule.isLegal(removal.id, otherId, dir)) continue;

                        int nEnablers = this.state.enablerCounts[x, y, otherId, dir];
                        if (nEnablers != 1) {
                            this.state.enablerCounts.decrement(x, y, otherId, dir);
                            continue;
                        }

                        // TODO: what's this?
                        if (this.state.enablerCounts.anyZeroEnablers(x, y, removal.id)) continue;

                        this.state.removePatternUpdatingEntropy(x, y, otherId, this.patterns);
                        if (this.state.entropies[x, y].totalWeight == 0) {
                            return AdvanceStatus.Fail; // contradiction
                        }

                        this.heap.add(x, y, this.state.entropies[x, y].entropyWithNoise());
                        this.removals.Push(new TileRemoval(x, y, otherId));

                        //
                        this.state.enablerCounts.decrement(x, y, otherId, dir);
                    }
                }
            }
            return AdvanceStatus.Continue;
        }
    }
}
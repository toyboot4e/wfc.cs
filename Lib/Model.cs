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
        Heap heap;
        Stack<TileRemoval> removals;
        int nRemainings;
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
            // TODO: proper initial heap elements
            this.heap = new Heap(size.area * 2);
            // store all of the cells so that they every cell will be selected
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

        // TODO: periodic output
        bool isOnBoundary(int x, int y) {
            var size = this.input.outputSize;
            return x < 0 || x >= size.x || y < 0 || y >= size.y;
        }

        /// <summary>
        /// Tries to solve the constraint satisfication problem with the observe-propagate loop
        /// </summary>
        public void run() {
            while (this.nRemainings > 0) {
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
        /// Decides a cell (select next cell, decide a pattern for it, and propagate the change)
        /// </summary>
        public AdvanceStatus advance() {
            var(pos, isOnContradiction) = selectNextCell(this);
            if (isOnContradiction) return AdvanceStatus.Fail;
            var id = selectPatternForCell(this, pos.x, pos.y);
            decidePatternForCell(this, pos.x, pos.y, id);
            return this.afterDecide();

            // * local functions *

            /// <summary>
            /// Select one of the cells with least total weight. Cell with least uncernity.
            /// Returns (pos, isOnContradiction)
            /// </summary>
            /// <remark>The intent is to minimize the risk of contradiction</summary>
            static(Vec2, bool) selectNextCell(Model self) {
                while (self.heap.hasAnyElement()) {
                    var cell = self.heap.pop();
                    if (self.state.entropies[cell.x, cell.y].isDecided) continue;
                    return (new Vec2(cell.x, cell.y), false);
                }
                System.Console.WriteLine("Unreachable. The heap is empty, but there are remaining cells undecided.");
                return (new Vec2(-1, -1), true);
            }

            /// <summary>Randomly choose a possible pattern for a cell in respect of weights of patterns</summary>
            static PatternId selectPatternForCell(Model self, int x, int y) {
                var totalWeight = self.state.entropies[x, y].totalWeight;
                int random = self.random.Next(0, totalWeight); // [0, totalWeight) < totalWeight

                int nPatterns = self.patterns.len;
                int sumWeight = 0;
                for (int id = 0; id < nPatterns; id++) {
                    if (self.state.legalities[x, y, id] == false) continue;
                    sumWeight += self.patterns[id].weight;
                    if (sumWeight > random) return new PatternId(id);
                }

                System.Console.WriteLine("EEEEEEEEEEEEEEEEERRRRRRRRRRRRRRRROOOOOOOOOOOOORRRR");
                return new PatternId(-1);
            }

            /// <summary>Lockin a cell into a <c>Pattern</c>. Collapse</summary>
            /// <remark>
            /// Every pattern is decided through this method, even when possible cells for became only one.
            /// <c>nRemainings</c> is followed with this method.
            /// </remark>
            static void decidePatternForCell(Model self, int x, int y, PatternId id) {
                // TODO: maybe use Span<T> to modify a struct in a List<T>?
                { // state that we've decided the cell (without updating the entropy cache)
                    var newCache = self.state.entropies[x, y];
                    newCache.isDecided = true;
                    self.state.entropies[x, y] = newCache;
                }
                self.nRemainings -= 1;
                // System.Console.Write($"{id.asIndex} ");

                // remove all other possible patterns for the cell
                var nPatterns = self.patterns.len;
                for (int i = 0; i < nPatterns; i++) {
                    // skip illegal patterns and the pattern locked into
                    if (self.state.legalities[x, y, i] == false || i == id.asIndex) continue;
                    // remove the pattern from the legality distribution
                    self.state.removePattern(x, y, new PatternId(i));
                    // stack the removal so that we can later propagate the local similarity constraint (following AdjacencyRule)
                    self.removals.Push(new TileRemoval(x, y, new PatternId(i)));
                }
            }
        }

        // TODO: use fixed, stackalloc or int to enum
        static OverlappingDirection[] dirs = new [] { OverlappingDirection.N, OverlappingDirection.E, OverlappingDirection.S, OverlappingDirection.W };
        static(int, int) [] dirVecs = new [] {
            (0, -1), (1, 0), (0, 1), (-1, 0)
        };

        /// <summary>
        /// Propates the <c>AdjacencyRule</c> to neighbors of the decided cell in each direction
        /// </summary>
        AdvanceStatus afterDecide() {
            // System.Console.WriteLine($"=== propagate ===");
            int nPatterns = this.patterns.len;
            while (this.removals.Count > 0) {
                var removal = this.removals.Pop();
                // System.Console.WriteLine($"removal found: {removal.x}, {removal.y}, {removal.id.asIndex}");
                // for each neighbor (= for each cells in the direction)
                for (int dirIndex = 0; dirIndex < 4; dirIndex++) {
                    var dirFromNeighbor = dirs[dirIndex].opposite();
                    int neighborX = removal.x + dirVecs[dirIndex].Item1;
                    int neighborY = removal.y + dirVecs[dirIndex].Item2;

                    // TODO: boundary check / periodicity
                    if (this.isOnBoundary(neighborX, neighborY)) continue;

                    for (int i = 0; i < nPatterns; i++) {
                        var neighborId = new PatternId(i);
                        // just scan through legal patterns in the neighbor
                        if (!this.rule.isLegalSafe(neighborId, removal.id, dirFromNeighbor)) continue;

                        // TODO: FIXME
                        // if the possibility is already removed, just skip the neighbor
                        if (this.state.legalities[neighborX, neighborY, neighborId.asIndex] == false) continue;
                        if (this.state.entropies[neighborX, neighborY].isDecided) continue;

                        int nEnablers = this.state.enablerCounts[neighborX, neighborY, neighborId, dirFromNeighbor];
                        // TODO: what's this?
                        if (nEnablers == 1 && !this.state.enablerCounts.anyZeroEnablerFor(neighborX, neighborY, neighborId)) {
                            // System.Console.WriteLine($"neighbor: {neighborX}, {neighborY}, {neighborId.asIndex}");
                            // finally the pattern is not compatible
                            this.state.removePatternUpdatingEntropy(neighborX, neighborY, neighborId, this.patterns);
                            if (this.state.entropies[neighborX, neighborY].totalWeight == 0) {
                                return AdvanceStatus.Fail; // contradiction
                            }
                            // update heap so that this cell is easier to choose next time
                            this.heap.add(neighborX, neighborY, this.state.entropies[neighborX, neighborY].entropyWithNoise());
                            // let it be propagated
                            this.removals.Push(new TileRemoval(neighborX, neighborY, neighborId));
                        }

                        this.state.enablerCounts.decrement(neighborX, neighborY, neighborId, dirFromNeighbor);
                    }
                }
            }
            return AdvanceStatus.Continue;
        }
    }
}
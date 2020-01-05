using System.Collections.Generic;
using System.Diagnostics;

namespace Wfc.Overlap {
    /// <summary>The top of the wave function collapse algorithm</summary>
    public class WfcContext {
        public readonly Model model;
        public readonly State state;
        public readonly System.Random random = new System.Random();

        public WfcContext(Map source, int N, Vec2 outputSize) {
            Debug.Assert(N >= 2, $"each pattern must be greater than or equal to 2x2 (N = {N})");
            this.model = new Model(source, N, outputSize);
            this.state = new State(outputSize.x, outputSize.y, this.model.patterns, ref this.model.rule);
        }

        /// <summary>Returns if succeeded</summary>
        public bool run() {
            var observer = new Observer(this.model.input.outputSize, this.state);
            while (true) {
                switch (observer.advance(this)) {
                    case AdvanceStatus.Continue:
                        continue;
                    case AdvanceStatus.Success:
                        System.Console.WriteLine("SUCCESS");
                        return true;
                    case AdvanceStatus.Fail:
                        System.Console.WriteLine("FAIL");
                        return false;
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
    }

    /// <summary>
    /// Creates input for the wave function collapse algorithm (overlappin model)
    /// </summary>
    public class Model {
        public Input input;
        public PatternStorage patterns;
        public AdjacencyRule rule;

        /// <summary>Original input from a user</summary>
        public struct Input {
            public Map source;
            public int N;
            public Vec2 outputSize;

            // TODO: handling periodic output
            public bool isOnBoundary(int x, int y) {
                var size = this.outputSize;
                return x < 0 || x >= size.x || y < 0 || y >= size.y;
            }
        }

        public Model(Map source, int N, Vec2 outputSize) {
            this.input = new Input() {
                source = source,
                N = N,
                outputSize = outputSize,
            };
            var size = input.outputSize;
            this.patterns = Model.extractPatterns(input.source, input.N);
            this.rule = new AdjacencyRule(this.patterns, input.source);
        }

        /// <summary>
        /// Extracts every NxN pattern in the <c>source</c> considering their variants (rotations and flippings)
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
    }

    public class Observer {
        Heap heap;
        int nRemainings;
        Propagator propagator;

        public Observer(Vec2 outputSize, State state) {
            this.heap = new Heap(outputSize.area);

            for (int y = 0; y < outputSize.y; y++) {
                for (int x = 0; x < outputSize.x; x++) {
                    this.heap.add(x, y, state.entropies[x, y].entropyWithNoise());
                }
            }

            this.nRemainings = outputSize.area;
            this.propagator = new Propagator();
        }

        public void updateHeap(int x, int y, double entropy) {
            this.heap.add(x, y, entropy);
        }

        public WfcContext.AdvanceStatus advance(WfcContext cx) {
            if (this.nRemainings <= 0) {
                this.propagator.propagateAllRemovals(cx, this);
                return WfcContext.AdvanceStatus.Success;
            }

            var(pos, isOnContradiction) = Observer.selectNextCell(ref this.heap, cx.state);
            if (isOnContradiction) return WfcContext.AdvanceStatus.Fail;
            var id = selectPatternForCell(pos.x, pos.y, cx.state, cx.model.patterns, cx.random);
            this.decidePatternForCell(pos.x, pos.y, id, cx.state, cx.model.patterns, this.propagator);
            return this.propagator.propagateAllRemovals(cx, this);
        }

        /// <summary>
        /// Select one of the cells with least total weight. Cell with least uncernity.
        /// Returns (pos, isOnContradiction)
        /// </summary>
        /// <remark>The intent is to minimize the risk of contradiction</summary>
        static(Vec2, bool) selectNextCell(ref Heap heap, State state) {
            while (heap.hasAnyElement()) {
                var cell = heap.pop();
                if (state.entropies[cell.x, cell.y].isDecided) continue;
                return (new Vec2(cell.x, cell.y), false);
            }
            System.Console.WriteLine("Unreachable. The heap is empty, but there are remaining cells");
            return (new Vec2(-1, -1), true);
        }

        /// <summary>Choose a possible pattern for an unlocked cell randomly in respect of weights of patterns</summary>
        static PatternId selectPatternForCell(int x, int y, State state, PatternStorage patterns, System.Random rnd) {
            var totalWeight = state.entropies[x, y].totalWeight;
            int random = rnd.Next(0, totalWeight); // [0, totalWeight) < totalWeight

            int nPatterns = patterns.len;
            int sumWeight = 0;
            for (int id = 0; id < nPatterns; id++) {
                if (!state.isCompatible(x, y, new PatternId(id))) continue;
                sumWeight += patterns[id].weight;
                if (sumWeight > random) return new PatternId(id);
            }

            System.Console.WriteLine("ERROR: tried to select a pattern for a contradicted cell");
            return new PatternId(-1);
        }

        /// <summary>Lockin a cell into a <c>Pattern</c>. Collapse, observe</summary>
        /// <remark>Every pattern is decided through this method</remark>
        void decidePatternForCell(int x, int y, PatternId id, State state, PatternStorage patterns, Propagator propagator) {
            state.onDecidePattern(x, y);
            this.nRemainings -= 1;

            // remove all other possible patterns for the cell
            var nPatterns = patterns.len;
            for (int i = 0; i < nPatterns; i++) {
                // skip illegal patterns and the pattern locked into
                if (state.isCompatible(x, y, new PatternId(i)) == false || i == id.asIndex) continue;
                // remove the pattern from the legality distribution
                // (without updating weight, entropy and enabler counts)
                state.removePattern(x, y, new PatternId(i));
                propagator.addRemoval(x, y, new PatternId(i));
            }
        }
    }

    public class Propagator {
        /// <summary>FIFO</summary>
        Queue<TileRemoval> removals;

        public Propagator() {
            this.removals = new Queue<TileRemoval>(10);
        }

        struct TileRemoval {
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

        public void addRemoval(int x, int y, PatternId id) {
            this.removals.Enqueue(new TileRemoval(x, y, id));
        }

        public WfcContext.AdvanceStatus propagateAllRemovals(WfcContext cx, Observer observer) {
            while (this.removals.Count > 0) {
                var removal = this.removals.Dequeue();
                var status = this.propagateRemoval(removal, cx, observer);
                if (status != WfcContext.AdvanceStatus.Continue) return status;
            }
            return WfcContext.AdvanceStatus.Continue;
        }

        // TODO: use fixed, stackalloc or int to enum
        static OverlappingDirection[] dirs = new [] {
            OverlappingDirection.N, OverlappingDirection.E, OverlappingDirection.S, OverlappingDirection.W
        };
        static(int, int) [] dirVecs = new [] {
            // N, E, S, W
            (0, -1), (1, 0), (0, 1), (-1, 0)
        };

        WfcContext.AdvanceStatus propagateRemoval(TileRemoval removal, WfcContext cx, Observer observer) {
            int nPatterns = cx.model.patterns.len;
            for (int dirIndex = 0; dirIndex < 4; dirIndex++) {
                int neighborX = removal.x + dirVecs[dirIndex].Item1;
                int neighborY = removal.y + dirVecs[dirIndex].Item2;
                if (cx.model.input.isOnBoundary(neighborX, neighborY)) continue;
                var dirFromNeighbor = dirs[dirIndex].opposite();

                for (int i = 0; i < nPatterns; i++) {
                    var neighborId = new PatternId(i);

                    {
                        // not an enabler
                        if (!cx.model.rule.isLegalSafe(neighborId, removal.id, dirFromNeighbor)) continue;
                        // not a possible pattern
                        if (!cx.state.isCompatible(neighborX, neighborY, neighborId)) continue;
                    }

                    bool isOnZeroCount = cx.state.enablerCounts.reduce(neighborX, neighborY, neighborId, dirFromNeighbor);
                    if (!isOnZeroCount) continue;

                    // let's remove the pattern
                    cx.state.removePatternUpdatingEntropy(neighborX, neighborY, neighborId, cx.model.patterns);
                    if (cx.state.entropies[neighborX, neighborY].totalWeight == 0) {
                        return WfcContext.AdvanceStatus.Fail; // contradiction
                    }

                    // and propagate the removal
                    observer.updateHeap(neighborX, neighborY, cx.state.entropies[neighborX, neighborY].entropyWithNoise());
                    this.addRemoval(neighborX, neighborY, neighborId);
                }
            }

            return WfcContext.AdvanceStatus.Continue;
        }
    }
}
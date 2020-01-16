using System.Collections.Generic;
using System.Diagnostics;

namespace Wfc.Overlap {
    /// <summary>The top interface of the wave function collapse algorithm</summary>
    public class WfcContext {
        public readonly Model model;
        public readonly State state;
        public readonly System.Random random = new System.Random();

        public WfcContext(Map source, int N, Vec2 outputSize) {
            Debug.Assert(N >= 2, $"each pattern must be greater than or equal to 2x2 (N = {N})");
            this.model = new Model(source, N, outputSize);
            this.state = new State(outputSize.x, outputSize.y, this.model.patterns, ref this.model.rule);
        }

        /// <summary>Returns if it succeeded</summary>
        public bool run() {
            var observer = new Observer(this.model.input.outputSize, this.state);
            while (true) {
                switch (observer.advance(this)) {
                    case AdvanceStatus.Continue:
                        continue;
                    case AdvanceStatus.Success:
                        // TODO: debug print
                        System.Console.WriteLine("SUCCESS");
                        return true;
                    case AdvanceStatus.Fail:
                        System.Console.WriteLine("FAIL");
                        return false;
                }
            }
        }

        /// <summary>To observe generation</summary>
        public IEnumerable<AdvanceStatus> runIter() {
            var observer = new Observer(this.model.input.outputSize, this.state);
            while (true) {
                var status = observer.advance(this);
                yield return status;
                System.Console.WriteLine(status);
                switch (status) {
                    case AdvanceStatus.Continue:
                        break;
                    case AdvanceStatus.Success:
                        System.Console.WriteLine("SUCCESS");
                        yield break;
                    case AdvanceStatus.Fail:
                        System.Console.WriteLine("FAIL");
                        yield break;
                }
            }
        }

        public enum AdvanceStatus {
            /// <summary>Just in proress</summary>
            Continue,
            /// <summary>Every cell is filled in respect to the local similarity constraint (<c>AdjacencyRule</c>)</summary>
            Success,
            /// <summary>A contradiction is reached, where some cell has no possible pattern</summary>
            Fail,
        }
    }

    /// <summary>
    /// Creates input for the wave function collapse algorithm (overlapping model)
    /// </summary>
    public class Model {
        public Input input;
        public PatternStorage patterns;
        public AdjacencyRule rule;

        /// <summary>Original input from a user</summary>
        public class Input {
            public Map source;
            public int N;
            public Vec2 outputSize;
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

        /// <summary>If the output is not periodic, filter out positions outside of the output area</summary>
        public bool filterPos(int x, int y) {
            var size = this.input.outputSize;
            return x < 0 || x >= size.x || y < 0 || y >= size.y;
        }

        /// <summary>
        /// Extracts every NxN pattern in the <c>source</c> map considering their variants (rotations and flippings)
        /// </summary>
        static PatternStorage extractPatterns(Map source, int N) {
            var patterns = new PatternStorage(source, N);
            var variations = PatternUtil.variations; // TODO: use fixed or stackalloc
            var nVariations = variations.Length;

            // TODO: handling periodic input
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

    /// <summary>Advances the state of WFC</summary>
    public class Observer {
        /// <summary>Used to pick up cell with least entropy</summary>
        CellHeap heap;
        int nRemainingCells;
        Propagator propagator;

        public Observer(Vec2 outputSize, State state) {
            this.heap = new CellHeap(outputSize.area);
            this.nRemainingCells = outputSize.area;
            this.propagator = new Propagator();

            // make all the cells pickable
            for (int y = 0; y < outputSize.y; y++) {
                for (int x = 0; x < outputSize.x; x++) {
                    this.heap.add(x, y, state.entropies[x, y].entropyWithNoise());
                }
            }
        }

        public void updateHeap(int x, int y, double newEntropy) {
            this.heap.add(x, y, newEntropy);
        }

        public WfcContext.AdvanceStatus advance(WfcContext cx) {
            if (this.nRemainingCells <= 0) return WfcContext.AdvanceStatus.Success; // every cell is decided

            var(pos, isContradicted) = Observer.selectNextCellToDecide(ref this.heap, cx.state);
            if (isContradicted) {
                System.Console.WriteLine("Unreachable. The heap is empty, but there are remaining cells");
                return WfcContext.AdvanceStatus.Fail;
            }

            var id = Observer.selectPatternForCell(pos.x, pos.y, cx.state, cx.model.patterns, cx.random);
            this.decidePatternForCell(pos.x, pos.y, id, cx.state, cx.model.patterns, this.propagator);
            return this.propagator.propagateRec(cx, this) ?
                WfcContext.AdvanceStatus.Fail :
                WfcContext.AdvanceStatus.Continue;
        }

        /// <summary>Returns (pos, isOnContradiction)</summary>
        /// <remark>The intent is to minimize the risk of contradiction</summary>
        static(Vec2, bool) selectNextCellToDecide(ref CellHeap heap, State state) {
            while (heap.hasAnyElement()) {
                var cell = heap.pop();
                if (state.entropies[cell.x, cell.y].isDecided) continue;
                return (new Vec2(cell.x, cell.y), false);
            }
            return (new Vec2(-1, -1), true); // contradicted
        }

        /// <summary>Choose a possible pattern for an unlocked cell randomly in respect of weights of patterns</summary>
        static PatternId selectPatternForCell(int x, int y, State state, PatternStorage patterns, System.Random rnd) {
            int random = rnd.Next(0, state.entropies[x, y].totalWeight);

            int sumWeight = 0;
            for (int id_ = 0; id_ < patterns.len; id_++) {
                var id = new PatternId(id_);
                if (!state.isPossible(x, y, id)) continue;

                sumWeight += patterns[id_].weight;
                if (sumWeight > random) return id;
            }

            System.Console.WriteLine("ERROR: tried to select a pattern for a contradicted cell");
            return new PatternId(-1);
        }

        /// <summary>Lockin a cell into a <c>Pattern</c>. Collapse, observe</summary>
        /// <remark>Every pattern is decided through this method</remark>
        void decidePatternForCell(int x, int y, PatternId id, State state, PatternStorage patterns, Propagator propagator) {
            state.onDecidePattern(x, y, patterns[id.asIndex].weight);
            this.nRemainingCells -= 1;

            // setup next propagation
            var nPatterns = patterns.len;
            for (int i = 0; i < nPatterns; i++) {
                var otherId = new PatternId(i);
                if (!state.isPossible(x, y, otherId) || i == id.asIndex) continue;

                state.removePattern(x, y, otherId);
                propagator.onDecidePattern(x, y, otherId);
            }
        }
    }

    public class Propagator {
        /// <summary>LIFO</summary>
        Stack<TileRemoval> removals;

        public Propagator() {
            this.removals = new Stack<TileRemoval>(10);
        }

        struct TileRemoval {
            public Vec2 pos;
            public PatternId id;

            public TileRemoval(int x, int y, PatternId id) {
                this.pos = new Vec2(x, y);
                this.id = id;
            }
        }

        /// <summary>Setting up a removal, which will be recursively propagated</summary>
        public void onDecidePattern(int x, int y, PatternId id) {
            this.removals.Push(new TileRemoval(x, y, id));
        }

        /// <summary>True if contradicted</summary>
        public bool propagateRec(WfcContext cx, Observer observer) {
            // propagate the effect of a removal
            while (true) {
                if (this.removals.Count == 0) break;
                bool isContradicted = this.propagateRemoval(this.removals.Pop(), cx, observer);
                if (isContradicted) return true;
            }
            return false;
        }

        static Vec2[] dirVecs = new [] {
            // N, E, S, W
            new Vec2(0, -1), new Vec2(1, 0), new Vec2(0, 1), new Vec2(-1, 0)
        };

        struct Neighbor {
            public Vec2 pos;
            public PatternId id;
        }

        /// <summary>Reduces enabler counts around the cell. True if contradicted</summary>
        bool propagateRemoval(TileRemoval removal, WfcContext cx, Observer observer) {
            int nPatterns = cx.model.patterns.len;
            var outputSize = cx.model.input.outputSize;

            var nb = new Neighbor { };
            for (int dirIndex = 0; dirIndex < 4; dirIndex++) {
                nb.pos = removal.pos + dirVecs[dirIndex];
                if (cx.model.filterPos(nb.pos.x, nb.pos.y)) continue;

                // for periodic output
                nb.pos += outputSize;
                nb.pos %= outputSize;

                var dirFromNeighbor = ((OverlappingDirection) dirIndex).opposite();
                for (int i = 0; i < nPatterns; i++) {
                    nb.id = new PatternId(i);

                    // skip some combinations (not an enabler or already disabled)
                    if (!cx.model.rule.canOverlap(nb.id, dirFromNeighbor, removal.id)) continue;
                    if (!cx.state.isPossible(nb.pos.x, nb.pos.y, nb.id)) continue;

                    // decrement the enabler count
                    bool doRemove = cx.state.enablerCounts.decrement(nb.pos.x, nb.pos.y, nb.id, dirFromNeighbor);
                    if (!doRemove) continue;

                    bool isContradicted = this.onRecursion(nb.pos.x, nb.pos.y, nb.id, cx, observer);
                    if (isContradicted) return true;
                }
            }

            return false;
        }

        /// <summary>Called when a next removal is found. Returns true if we're on contradiction</summary>
        bool onRecursion(int x, int y, PatternId id, WfcContext cx, Observer observer) {
            // remove the pattern
            if (cx.state.removePatternUpdatingEntropy(x, y, id, cx.model.patterns)) return true;

            observer.updateHeap(x, y, cx.state.entropies[x, y].entropyWithNoise());
            this.removals.Push(new TileRemoval(x, y, id));
            return false;
        }
    }
}
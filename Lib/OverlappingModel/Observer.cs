using System.Collections.Generic;

namespace Wfc.Overlap {
    /// <summary>Advances the state of WFC</summary>
    public class Observer : iObserver {
        /// <summary>Used to pick up cell with least entropy</summary>
        CellHeap heap;
        int nRemainingCells;
        Propagator propagator;

        public Observer(Vec2i outputSize, State state) {
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
            Observer.lockinPatternForCell(pos.x, pos.y, id, cx.state, cx.model.patterns, this.propagator);
            this.nRemainingCells -= 1;

            return this.propagator.propagateAll(cx, this) ?
                WfcContext.AdvanceStatus.Fail :
                WfcContext.AdvanceStatus.Continue;
        }

        /// <summary>Returns (pos, isOnContradiction)</summary>
        /// <remark>The intent is to minimize the risk of contradiction</summary>
        static(Vec2i, bool) selectNextCellToDecide(ref CellHeap heap, State state) {
            while (heap.hasAnyElement()) {
                var cell = heap.pop();
                if (state.entropies[cell.x, cell.y].isDecided) continue;
                return (new Vec2i(cell.x, cell.y), false);
            }
            return (new Vec2i(-1, -1), true); // contradicted
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
        static void lockinPatternForCell(int x, int y, PatternId idToLockin, State state, PatternStorage patterns, Propagator propagator) {
            state.onLockinPattern(x, y, patterns[idToLockin.asIndex].weight);

            // setup next propagation
            for (int i = 0; i < patterns.len; i++) {
                var idToRemove = new PatternId(i);
                if (!state.isPossible(x, y, idToRemove) || i == idToLockin.asIndex) continue;

                state.removePattern(x, y, idToRemove);
                propagator.onLockIn(x, y, idToRemove);
            }
        }
    }

    // TODO: cache for propagator??
    public class Propagator {
        /// <summary>LIFO</summary>
        Stack<TileRemoval> removals;

        public Propagator() {
            this.removals = new Stack<TileRemoval>(10);
        }

        struct TileRemoval {
            public Vec2i pos;
            public PatternId id;

            public TileRemoval(int x, int y, PatternId id) {
                this.pos = new Vec2i(x, y);
                this.id = id;
            }
        }

        /// <summary>Add a removal, which will be propagated later</summary>
        public void onLockIn(int x, int y, PatternId id) {
            this.removals.Push(new TileRemoval(x, y, id));
        }

        // TODO: on lockin, just remove patterns for performance and then propagate
        /// <summary>True if contradicted. Recursively propagates all the removals</summary>
        public bool propagateAll(WfcContext cx, Observer observer) {
            // propagate the effect of a removal
            while (this.removals.Count > 0) {
                if (this.propagateRemoval(this.removals.Pop(), cx, observer)) return true;
            }
            return false;
        }

        static Vec2i[] dirVecs = new [] {
            // N, E, S, W
            new Vec2i(0, -1), new Vec2i(1, 0), new Vec2i(0, 1), new Vec2i(-1, 0)
        };

        struct Neighbor {
            public Vec2i pos;
            public PatternId id;
        }

        /// <summary>Reduces enabler counts around the cell. True if contradicted</summary>
        bool propagateRemoval(TileRemoval removal, WfcContext cx, Observer observer) {
            int nPatterns = cx.model.patterns.len;
            var outputSize = cx.model.outputSize;

            var nb = new Neighbor { };
            for (int dirIndex = 0; dirIndex < 4; dirIndex++) {
                nb.pos = removal.pos + dirVecs[dirIndex];
                if (cx.model.filterPos(nb.pos.x, nb.pos.y)) continue;

                // for periodic output
                nb.pos += outputSize;
                nb.pos %= outputSize;

                var dirFromNeighbor = ((Dir4) dirIndex).opposite();
                for (int i = 0; i < nPatterns; i++) {
                    nb.id = new PatternId(i);

                    // skip some combinations (not an enabler or already removed)
                    if (!cx.model.rule.isLegal(nb.id, dirFromNeighbor, removal.id)) continue;
                    if (!cx.state.isPossible(nb.pos.x, nb.pos.y, nb.id)) continue;

                    // decrement the enabler count for the compatible pattern
                    if (!cx.state.enablerCounts.decrement(nb.pos.x, nb.pos.y, nb.id, dirFromNeighbor)) continue;

                    if (this.recPropagate(nb.pos.x, nb.pos.y, nb.id, cx, observer)) return true;
                }
            }

            return false;
        }

        /// <summary>Called when a next removal is found. Returns true if we're on contradiction</summary>
        bool recPropagate(int x, int y, PatternId id, WfcContext cx, Observer observer) {
            if (cx.state.removePatternUpdatingEntropy(x, y, id, cx.model.patterns)) return true;

            observer.updateHeap(x, y, cx.state.entropies[x, y].entropyWithNoise());
            this.removals.Push(new TileRemoval(x, y, id));
            return false;
        }
    }
}
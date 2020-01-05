using System.Collections.Generic;
using System.Diagnostics;

namespace Wfc.Overlap {
    /// <summary>Context of wave function collapse algorithm</summary>
    public class Context {
        public readonly Model model;
        public readonly State state;
        public readonly System.Random random = new System.Random();

        public Context(Map source, int N, Vec2 outputSize) {
            Debug.Assert(N >= 2, $"each pattern must be greater than or equal to  2x2 (N = {N})");
            this.model = new Model(source, N, outputSize);
            this.state = new State(outputSize.x, outputSize.y, this.model.patterns, ref this.model.rule);
        }

        /// <summary>
        /// Tries to solve the constraint satisfication problem with the observe-propagate loop
        /// </summary>
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
    /// Forces the local similarity constraint: any NxN pattern in the <c>output</c>
    /// can be found in the <c>source</c>. Patterns are extracted from the <c>source</c>
    /// considering their variants (flippings and rotations).
    /// </summary>
    public class Model {
        public Input input;
        public PatternStorage patterns;
        public AdjacencyRule rule;

        public struct Input {
            public Map source;
            public int N;
            public Vec2 outputSize;

            // TODO: periodic output
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

        public void onUpdateEntropy(int x, int y, double entropy) {
            this.heap.add(x, y, entropy);
        }

        public Context.AdvanceStatus advance(Context cx) {
            if (this.nRemainings <= 0) {
                this.propagator.propagate(cx, this);
                return Context.AdvanceStatus.Success;
            }
            var(pos, isOnContradiction) = Observer.selectNextCell(this.heap, cx.state);
            if (isOnContradiction) return Context.AdvanceStatus.Fail;
            var id = selectPatternForCell(pos.x, pos.y, cx.state, cx.model.patterns, cx.random);
            this.decidePatternForCell(pos.x, pos.y, id, cx.state, cx.model.patterns, this.propagator);
            return this.propagator.propagate(cx, this);
        }

        /// <summary>
        /// Select one of the cells with least total weight. Cell with least uncernity.
        /// Returns (pos, isOnContradiction)
        /// </summary>
        /// <remark>The intent is to minimize the risk of contradiction</summary>
        static(Vec2, bool) selectNextCell(Heap heap, State state) {
            while (heap.hasAnyElement()) {
                var cell = heap.pop();
                if (state.entropies[cell.x, cell.y].isDecided) continue;
                return (new Vec2(cell.x, cell.y), false);
            }
            System.Console.WriteLine("Unreachable. The heap is empty, but there are remaining cells undecided.");
            return (new Vec2(-1, -1), true);
        }

        /// <summary>Randomly choose a possible pattern for a cell in respect of weights of patterns</summary>
        static PatternId selectPatternForCell(int x, int y, State state, PatternStorage patterns, System.Random rnd) {
            var totalWeight = state.entropies[x, y].totalWeight;
            int random = rnd.Next(0, totalWeight); // [0, totalWeight) < totalWeight

            int nPatterns = patterns.len;
            int sumWeight = 0;
            for (int id = 0; id < nPatterns; id++) {
                if (state.isLegal(x, y, new PatternId(id)) == false) continue;
                sumWeight += patterns[id].weight;
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
        void decidePatternForCell(int x, int y, PatternId id, State state, PatternStorage patterns, Propagator propagator) {
            // TODO: maybe use Span<T> to modify a struct in a List<T>?
            { // state that we've decided the cell (without updating the entropy cache)
                var newCache = state.entropies[x, y];
                newCache.isDecided = true;
                state.entropies[x, y] = newCache;
            }
            this.nRemainings -= 1;
            // System.Console.Write($"{id.asIndex} ");

            // remove all other possible patterns for the cell
            var nPatterns = patterns.len;
            for (int i = 0; i < nPatterns; i++) {
                // skip illegal patterns and the pattern locked into
                if (state.isLegal(x, y, new PatternId(i)) == false || i == id.asIndex) continue;
                // remove the pattern from the legality distribution
                state.removePattern(x, y, new PatternId(i));
                // stack the removal so that we can later propagate the local similarity constraint (following AdjacencyRule)
                propagator.push(x, y, new PatternId(i));
            }
        }
    }

    public class Propagator {
        Stack<TileRemoval> removals;

        public Propagator() {
            this.removals = new Stack<TileRemoval>(10);
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

        public void push(int x, int y, PatternId id) {
            this.removals.Push(new TileRemoval(x, y, id));
        }

        // TODO: use fixed, stackalloc or int to enum
        static OverlappingDirection[] dirs = new [] { OverlappingDirection.N, OverlappingDirection.E, OverlappingDirection.S, OverlappingDirection.W };
        static(int, int) [] dirVecs = new [] {
            (0, -1), (1, 0), (0, 1), (-1, 0)
        };

        public Context.AdvanceStatus propagate(Context cx, Observer observer) {
            while (this.removals.Count > 0) {
                var removal = this.removals.Pop();
                var status = this.handleRemoval(removal, cx, observer);
                if (status != Context.AdvanceStatus.Continue) return status;
            }
            return Context.AdvanceStatus.Continue;
        }

        Context.AdvanceStatus handleRemoval(TileRemoval removal, Context cx, Observer observer) {
            int nPatterns = cx.model.patterns.len;
            for (int dirIndex = 0; dirIndex < 4; dirIndex++) {
                var dirFromNeighbor = dirs[dirIndex].opposite();
                int neighborX = removal.x + dirVecs[dirIndex].Item1;
                int neighborY = removal.y + dirVecs[dirIndex].Item2;

                // TODO: boundary check / periodicity
                if (cx.model.input.isOnBoundary(neighborX, neighborY)) continue;

                for (int i = 0; i < nPatterns; i++) {
                    var neighborId = new PatternId(i);
                    // just scan through legal patterns in the neighbor
                    if (!cx.model.rule.isLegalSafe(neighborId, removal.id, dirFromNeighbor)) continue;

                    // TODO: FIXME
                    // if the possibility is already removed, just skip the pattern for the neighbor cell
                    if (cx.state.isLegal(neighborX, neighborY, neighborId) == false) continue;

                    int nEnablers = cx.state.enablerCounts[neighborX, neighborY, neighborId, dirFromNeighbor];
                    // if (nEnablers == 0) System.Console.Write("ERROR zero enabler ");
                    // TODO: what's this?
                    if (nEnablers == 1 && !cx.state.enablerCounts.anyZeroEnablerFor(neighborX, neighborY, neighborId)) {
                        // System.Console.WriteLine($"neighbor: {neighborX}, {neighborY}, {neighborId.asIndex}");
                        // finally the pattern is not compatible
                        cx.state.removePatternUpdatingEntropy(neighborX, neighborY, neighborId, cx.model.patterns);
                        if (cx.state.entropies[neighborX, neighborY].totalWeight == 0) {
                            return Context.AdvanceStatus.Fail; // contradiction
                        }
                        // update heap so that this cell is easier to choose next time
                        observer.onUpdateEntropy(neighborX, neighborY, cx.state.entropies[neighborX, neighborY].entropyWithNoise());
                        // and let it be propagated
                        this.removals.Push(new TileRemoval(neighborX, neighborY, neighborId));
                    }

                    cx.state.enablerCounts.decrement(neighborX, neighborY, neighborId, dirFromNeighbor);
                }
            }
            return Context.AdvanceStatus.Continue;
        }
    }
}
using System.Diagnostics;

namespace Wfc.Overlap {
    /// <summary>
    /// Forces the local similarity constraint: any NxN pattern in the <c>output</c>
    /// can be found in the <c>source</c>. Patterns are extracted from the <c>source</c>
    /// considering their variants (flippings and rotations).
    /// </summary>
    public class Model {
        /// <summary>The source <c>Map</c></summary>
        public Map source;
        /// <summary>We'll stick with NxN patterns in the source</summary>
        public int N;
        /// <summary>Solved cells, observed cells, state</summary>
        public Map output;

        public PatternStorage patterns;
        public AdjacencyRule adjacencyRule;
        public Distribution distribution;

        public Model(Map source, int N, int outputW, int outputH) {
            Debug.Assert(N > 1, $"patterns must be bigger than two (N = {N})");
            this.source = source;
            this.N = N;
            this.output = new Map(outputH, outputW);
            this.patterns = new PatternStorage(source, N);

            this.extractPatterns();
            this.distribution = new Distribution(outputW, outputH, this.patterns);
            this.adjacencyRule = AdjacencyRule.build(this.patterns, this.source);
        }

        /// <summary>
        /// Creates NxN patterns from the source counting their frequencies / considering
        /// their rotations and flippings
        /// </summary>
        void extractPatterns() {
            var nVariants = PatternUtil.variations.Length;

            for (int y = 0; y < source.height - N + 1; y++) {
                for (int x = 0; x < source.width - N + 1; x++) {
                    for (int i = 0; i < nVariants; i++) {
                        this.patterns.add(x, y, PatternUtil.variations[i]);
                    }
                }
            }

            this.patterns.afterExtract();
        }

        /// <summary>
        /// Tries to solve the constraint satisfication problem with WFC's observe-propagate loop
        /// </summary>
        public void run() {
            while (true) {
                // "observe" a next cell
                var minWeight = this.distribution.minTotalWeight;

                if (minWeight == 0) {
                    // Succeeded; ll cells have been decided (collapsed)
                    return;
                }

                // If we find nothing, we are on a contradiction; now it's impossible to solve.
                // We can a) restart b) backtrack (if we could) c) remove surrounding cells and continue
                // Let's just remove the entire state and restart

                // on completion
                break;

                // "propagete" constranits (term of WFC)

                this.fillCellWithTile();
            }
        }

        public enum ObserveStatus {
            Finish,
            Fail,
            Continue,
        }

        /// <summary>Finds a cell with fewest legal tiles (more than zero). Cell with the least remaining values</summary>
        /// <remark>Cell with least entropy, most constrained cell.</summary>
        Vec2 selectNextCell() {
            // if nothing was found, we failed
            // if the lowest entropy is zero, we succeeded
            return new Vec2(0, 0);
        }

        /// <summary>New assignment to a partial solution</summary>
        void fillCellWithTile() {

        }

        /// <summary>Updates adjacent cells when one is decided (solved, assigned)</summary>
        void propagate(Vec2 pos) {
            // for each neighboring cell
            foreach(var p in pos.neighbors) {
                //
            }
            // for each pattern that was potentially valid
            // if the pattenr is no longer valid in regard of the cell changed
            // disable the pattern and mark the cell needs updated in the next iteration
        }
    }
}
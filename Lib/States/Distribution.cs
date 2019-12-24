using System.Collections.Generic;
using System.Linq;

namespace Wfc {
    /// <summary>Three-dimensional array [x, y, patternId]</summary>
    public class Distribution {
        ///<summary>[x, y, patternId] -> isValid</summary>
        public Array3D<bool> possibilities;
        ///<summary>[patternId] -> totalWeight</summary>
        public List<int> totalWeights;
        public int minTotalWeight;
        /// <remark>Index, constraint propagator</summary>
        int adjacencies;

        public Distribution(int w, int h, PatternStorage patterns) {
            var nPatterns = patterns.buffer.Count;
            this.possibilities = new Array3D<bool>(w, h, nPatterns);
            this.totalWeights = new List<int>(nPatterns);

            // initialize the weight maps
            int totalWeight = patterns.buffer.Select(p => p.weight).Sum();
            for (int i = 0; i < nPatterns; i++) {
                this.totalWeights.Add(totalWeight);
            }
            this.minTotalWeight = totalWeight;
        }

        public bool isValid(int x, int y, PatternId id) => this.possibilities.get(x, y, id.asInt);
        public int weight(PatternId id) => this.totalWeights[id.asInt];

        public Vec2 cellWithLeastPossibility() {
            return new Vec2(0, 0);
        }
    }
}
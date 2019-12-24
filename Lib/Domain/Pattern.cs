using System.Collections.Generic;

namespace Wfc {
    /// <summary>
    /// A local pattern in a source <c>Map</c> with frequency.
    /// Equality of two <c>Pattern</c>s can be tested through an extension method.
    /// </summary>
    /// <remark>
    /// Each local position can be converted to one in a source map with <c>this.variant.apply</c>
    /// </remark>
    public class Pattern {
        public Vec2 offset;
        public PatternVariation variant;
        /// <summary>Frequency, the number of occurencies of it in the source map</summary>
        public int weight;

        public void incWeight() => this.weight += 1;

        public Pattern(Vec2 offset, PatternVariation variant) {
            this.offset = offset;
            this.weight = 1;
            this.variant = variant;
        }
    }

    /// <summary>Wraps an integer as a special type</summary>
    public struct PatternId {
        public int asInt;

        public PatternId(int data) {
            this.asInt = data;
        }
    }

    /// <summary>Stores every <c>Pattern</c> of a source <c>Map</c></summary>
    public class PatternStorage {
        public Map source;
        public readonly int N;
        public List<Pattern> buffer;
        public Dictionary<Pattern, PatternId> patternIds;

        public PatternStorage(Map source, int N) {
            this.source = source;
            this.buffer = new List<Pattern>();
            this.N = N;
        }

        /// <summary>Stores an extracted pattern considering duplicates</summary>
        public void add(int x, int y, PatternVariation v) {
            var offset = new Vec2(x, y);
            var newPattern = new Pattern(offset, v);

            for (int i = 0; i < this.buffer.Count; i++) {
                if (this.buffer[i].isDuplicateOf(newPattern, this.source, this.N)) {
                    this.buffer[i].incWeight();
                    return;
                }
            }

            this.buffer.Add(newPattern);
        }

        /// <summary>Creates unique IDs for each pattern</summary>
        /// <remark>IDs can be used for indexes for lists</remark>
        public void afterExtract() {
            this.patternIds = new Dictionary<Pattern, PatternId>(this.buffer.Count);
            for (int i = 0; i < this.buffer.Count; i++) {
                this.patternIds.Add(this.buffer[i], new PatternId(i));
            }
        }
    }
}
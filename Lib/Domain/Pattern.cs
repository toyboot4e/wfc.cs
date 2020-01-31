using System.Collections.Generic;

namespace Wfc {
    /// <summary>
    /// A local pattern of a source <c>Map</c> with variation (rotation, flipping or none)
    /// </summary>
    /// <remark>
    /// Each position in a pattern can be converted into one in a source map using <c>this.variant.apply</c>.
    /// This implementation is like a slice, but <c>Pattern</c> can also be implemented as an array. Which is better?
    /// </remark>
    public class Pattern {
        public readonly Vec2i offset;
        public readonly PatternVariation variant;
        /// <summary>Frequency. The number of times it appears in a source <c>Map</c></summary>
        public int weight;

        public void incWeight() => this.weight += 1;

        public Pattern(Vec2i offset, PatternVariation variant) {
            this.offset = offset;
            this.weight = 1;
            this.variant = variant;
        }

        public Tile tileAt(int x, int y, int N, Map source) {
            var pos = this.offset + this.variant.apply(new Vec2i(x, y), N);
            return source[pos.x, pos.y];
        }
    }

    /// <summary>Wraps an index integer as a special type</summary>
    public struct PatternId {
        public readonly int asIndex;

        public PatternId(int index) {
            this.asIndex = index;
        }
    }

    /// <summary>Stores every <c>Pattern</c> of a source <c>Map</c></summary>
    public class PatternStorage {
        public Map source;
        /// <summary>Each pattern has a size of <c>N</c>x<c>N</c></summary>
        public readonly int N;
        readonly List<Pattern> buffer;

        public PatternStorage(Map source, int N) {
            this.source = source;
            this.buffer = new List<Pattern>();
            this.N = N;
        }

        public int len => this.buffer.Count;

        public Pattern this[int id] => this.buffer[id];

        /// <summary>Stores an extracted pattern considering duplicates</summary>
        public void store(int x, int y, PatternVariation v) {
            var offset = new Vec2i(x, y);
            var newPattern = new Pattern(offset, v);

            for (int i = 0; i < this.buffer.Count; i++) {
                if (this.buffer[i].isDuplicateOf(newPattern, this.source, this.N)) {
                    this.buffer[i].incWeight();
                    return;
                }
            }

            this.buffer.Add(newPattern);
        }
    }
}
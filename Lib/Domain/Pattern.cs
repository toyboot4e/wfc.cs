using System.Collections.Generic;

namespace Wfc {
    /// <summary>
    /// A local pattern in a source <c>Map</c> with frequency with size of NxN.
    /// Equality of two <c>Pattern</c>s can be tested through an extension method.
    /// </summary>
    /// <remark>
    /// Each local position can be converted to one in a source map with <c>this.variant.apply(this)</c>.
    /// This implementation is like a slice, but <c>Pattern</c> can also be implemented as an array. Which is better?
    /// </remark>
    // TODO: DIP for the arbitary of implementations
    public class Pattern {
        public Vec2 offset;
        public PatternVariation variant;
        /// <summary>The number of times it appears in the source <c>Map</c></summary>
        /// <remark>Frequency</remark>
        public int weight;

        public void incWeight() => this.weight += 1;

        public Pattern(Vec2 offset, PatternVariation variant) {
            this.offset = offset;
            this.weight = 1;
            this.variant = variant;
        }

        public Tile tileAt(int x, int y, int N, Map source) {
            var pos = this.offset + this.variant.apply(N, new Vec2(x, y));
            return source[pos.x, pos.y];
        }
    }

    /// <summary>Wraps an integer as a special type</summary>
    public struct PatternId {
        public int asIndex;

        public PatternId(int data) {
            this.asIndex = data;
        }
    }

    /// <summary>Stores every <c>Pattern</c> of a source <c>Map</c></summary>
    public class PatternStorage {
        public Map source;
        public readonly int N;
        List<Pattern> buffer;

        public PatternStorage(Map source, int N) {
            this.source = source;
            this.buffer = new List<Pattern>();
            this.N = N;
        }

        public int len => this.buffer.Count;

        public Pattern this[int id] => this.buffer[id];

        // TODO: add indexer and hide buffer

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
    }
}
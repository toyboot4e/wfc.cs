namespace Wfc {
    /// <summary>
    /// Flipping, rotation or none. Can be applied to each point in a <c>Pattern<c/> to map it
    /// to one in a source map.
    /// </summary>
    public enum PatternVariation {
        Original = 0,
        Rot90 = 1,
        Rot180 = 2,
        Rot270 = 3,
        FlipX = 4, // -
        FlipY = 5, // |
        FlipSlash = 6, // /
        FlipBackslash = 7, // \
    }

    public static class PatternUtil {
        public static PatternVariation[] variations = new [] {
            PatternVariation.Original,
            PatternVariation.Rot90,
            PatternVariation.Rot180,
            PatternVariation.Rot270,
            PatternVariation.FlipX,
            PatternVariation.FlipY,
            PatternVariation.FlipSlash,
            PatternVariation.FlipBackslash,
        };
    }

    public static class PatternVariantionExt {
        public static Vec2 apply(this PatternVariation self, int N, Vec2 v) {
            return applyInt((int) self, N - 1, v);
        }

        /// <remark>Can be used not only for <c>PatternVariant</c> but also for <c>OverlapDirection</c></remark>
        public static Vec2 applyInt(int i, int n, Vec2 v) {
            switch (i) {
                // 0     90    180   270
                // ##..  ...#  ....  ....
                // .##.  ..##  ....  .#..
                // ....  ..#.  .##.  ##..
                // ...   ....  ..##  #...
                case 0: // Original
                    return new Vec2(v.x, v.y);
                case 1: // Rot90
                    return new Vec2(n - v.y, v.x);
                case 2: // Rot180
                    return new Vec2(n - v.x, n - v.y);
                case 3: // Rot270
                    return new Vec2(v.y, n - v.x);
                    // [-]   [|]   [/]   [\]
                    // ....  ..##  ....  #...
                    // ....  .##.  ..#.  ##..
                    // .##.  ....  ..##  .#..
                    // ##..  ....  ...#  ....
                case 4: // -
                    return new Vec2(v.x, n - v.y);
                case 5: // |
                    return new Vec2(n - v.x, v.y);
                case 6: // /
                    return new Vec2(v.y, v.x);
                case 7: // \
                    return new Vec2(n - v.y, n - v.x);
                default:
                    // TODO: just assert
                    return new Vec2(-10000, -10000);
            }
        }

        /// <summary>Converts a point in a <c>Pattern</c> to one in the <c>source</c></summary>
        public static Vec2 localPosToSourcePos(this Pattern self, Vec2 localPosition, int N) {
            return self.offset + self.variant.apply(N, localPosition);
        }

        /// <summary>Tests equality of two <c>Pattern<c/>s</summary>
        public static bool isDuplicateOf(this Pattern self, Pattern another, Map source, int N) {
            for (int j = 0; j < N; j++) {
                for (int i = 0; i < N; i++) {
                    var posA = self.localPosToSourcePos(new Vec2(i, j), N);
                    var posB = another.localPosToSourcePos(new Vec2(i, j), N);
                    if (source[posA.x, posA.y] != source[posB.x, posB.y]) {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
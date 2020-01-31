namespace Wfc {
    /// <remark>Can be used as an index (with a very small cost)</summary>
    public enum Dir4 {
        N = 0, // original
        E = 1, // rotate 90
        S = 2, // rotate 180 (flip y also works)
        W = 3, // rotate 270
    }

    public static class Dir4Ext {
        public static Vec2i applyAsRotation(this Dir4 self, Vec2i v, int N) {
            return PatternVariantionExt.applyInt((int) self, v, N);
        }

        public static Dir4 opposite(this Dir4 self) {
            switch (self) {
                case Dir4.N:
                    return Dir4.S;
                case Dir4.E:
                    return Dir4.W;
                case Dir4.S:
                    return Dir4.N;
                case Dir4.W:
                    return Dir4.E;
            }
            // TODO: faster calculation
            throw new System.Exception("THE DIRECTION IS NOT IN [0, 3]");
        }

        public static int oppositeInt(int d) {
            return (d + 2) % 2;
        }
    }
}
namespace Wfc.Overlap {
    /// <remark>Can be used as an index (with a very small cost)</summary>
    public enum OverlappingDirection {
        N = 0, // original
        E = 1, // rotate 90
        S = 2, // rotate 180 (flip y also works)
        W = 3, // rotate 270
    }

    public static class OverlapDirectionExt {
        public static Vec2 applyAsRotation(this OverlappingDirection self, Vec2 v, int N) {
            return PatternVariantionExt.applyInt((int) self, v, N);
        }

        public static OverlappingDirection opposite(this OverlappingDirection self) {
            switch (self) {
                case OverlappingDirection.N:
                    return OverlappingDirection.S;
                case OverlappingDirection.E:
                    return OverlappingDirection.W;
                case OverlappingDirection.S:
                    return OverlappingDirection.N;
                case OverlappingDirection.W:
                    return OverlappingDirection.E;
            }
            // TODO: faster calculation
            throw new System.Exception("THE DIRECTION IS NOT IN [0, 3]");
        }

        public static int oppositeInt(int d) {
            return (d + 2) % 2;
        }
    }
}
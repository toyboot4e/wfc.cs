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
            return PatternVariantionExt.applyInt((int) self, N - 1, v);
        }

        public static OverlappingDirection opposite(this OverlappingDirection self) {
            return (OverlappingDirection) (((int) self + 2) % 2);
        }

        public static int opposite(int d) {
            return (d + 2) % 2;
        }
    }
}
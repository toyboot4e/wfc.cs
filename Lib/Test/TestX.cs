namespace Wfc.Overlap {
    public class Test {
        public static void testEveryRow(State state, ref Rule rule, PatternStorage patterns) {
            System.Console.WriteLine($"=== Test every row ===");
            int h = state.outputSize.y;
            int w = state.outputSize.x;
            int n = patterns.len;
            for (int y = 0; y < h; y++) {
                for (int x = 0; x < w - 1; x++) {
                    var fromId = state.patternIdAt(x, y, n);
                    var toId = state.patternIdAt(x + 1, y, n);
                    if (fromId == null || toId == null) continue;
                    if (!rule.canOverlap(fromId.Value, Dir4.E, toId.Value)) {
                        System.Console.WriteLine($"illegal: {x}, {y} ({fromId.Value.asIndex}) -> {x+1}, {y} ({toId.Value.asIndex})");
                    }
                }
            }
        }

        public static void testEveryColumn(State state, ref Rule rule, PatternStorage patterns) {
            System.Console.WriteLine($"=== Test every column ===");
            int h = state.outputSize.y;
            int w = state.outputSize.x;
            int n = patterns.len;
            for (int x = 0; x < w; x++) {
                for (int y = 0; y < h - 1; y++) {
                    var fromId = state.patternIdAt(x, y, n);
                    var toId = state.patternIdAt(x, y + 1, n);
                    if (fromId == null || toId == null) continue;
                    if (!rule.canOverlap(fromId.Value, Dir4.S, toId.Value)) {
                        System.Console.WriteLine($"illegal: {x}, {y} ({fromId.Value.asIndex}) -> {x}, {y+1} ({toId.Value.asIndex})");
                    }
                }
            }
        }

        public static void printInitialEnableCounter(int width, int height, PatternStorage patterns, ref Rule rule) {
            System.Console.WriteLine($"=== Initial enabler count ===");

            var counts = EnablerCounter.initial(width, height, patterns, ref rule);
            int nPatterns = patterns.len;

            for (int id_ = 0; id_ < nPatterns; id_++) {
                System.Console.Write($"{id_}: ");
                var id = new PatternId(id_);
                for (int d = 0; d < 4; d++) {
                    var dir = (Dir4) d;
                    int count = counts[0, 0, id, dir];
                    System.Console.Write($"{dir}: {count}, ");
                }
                System.Console.WriteLine("");
            }
        }
    }
}
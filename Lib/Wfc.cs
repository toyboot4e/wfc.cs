namespace Wfc {
    public class WfcOverlap : WfcContext {
        int N;

        WfcOverlap(Model model, State state, int N) : base(model, state) {
            this.N = N;
        }

        public static WfcOverlap create(ref Map source, int N, Vec2i outputSize) {
            if (N < 2) {
                throw new System.ArgumentException($"given N = {N}; it must be bigger than one");
            }

            var model = OverlappingModel.create(ref source, N, outputSize);
            var state = new State(outputSize.x, outputSize.y, model.patterns, ref model.rule);
            return new WfcOverlap(model, state, N);
        }

        public bool run() {
            return this.run(new Wfc.Solver(this.model.gridSize, this.state));
        }

        public Map getOutput(ref Map source) {
            int nPatterns = this.model.patterns.len;
            var outputSize = this.model.gridSize;
            var output = new Map(outputSize.x, outputSize.y);
            for (int i = 0; i < outputSize.area; i++) {
                int x = i % outputSize.x;
                int y = i / outputSize.y;
                var patternId = this.state.patternIdAt(x, y, nPatterns);
                if (patternId == null) {
                    output.tiles.add(Tile.None);
                } else {
                    var pattern = this.model.patterns[((PatternId) patternId).asIndex];
                    // tile at theleft-up corner of the pattern is used for the output
                    var sourcePos = pattern.localPosToSourcePos(new Vec2i(0, 0), N);
                    var tile = source[sourcePos.x, sourcePos.y];
                    output.tiles.add(tile);
                }
            }
            return output;
        }
    }

    public class WfcAdjacency : WfcContext {
        int N;

        WfcAdjacency(Model model, State state, int N) : base(model, state) {
            this.N = N;
        }

        public static WfcAdjacency create(ref Map source, int N, Vec2i outputSize) {
            if (N < 2) {
                throw new System.ArgumentException($"given N = {N}; it must be bigger than one");
            }

            var model = AdjacencyModel.create(ref source, N, outputSize);
            var gridSize = outputSize / N;
            var state = new State(gridSize.x, gridSize.y, model.patterns, ref model.rule);
            return new WfcAdjacency(model, state, N);
        }

        public bool run() {
            return this.run(new Wfc.Solver(this.model.gridSize, this.state));
        }

        public Map getOutput(ref Map source) {
            int nPatterns = this.model.patterns.len;
            var N = this.N;

            var gridSize = this.model.gridSize;
            var outputSize = gridSize * N;

            var output = new Map(outputSize.x, outputSize.y);
            for (int i = 0; i < outputSize.area; i++) {
                output.tiles.add(Tile.None);
            }

            for (int i = 0; i < gridSize.area; i++) {
                int gridX = i % gridSize.x;
                int gridY = i / gridSize.y;
                var patternId = this.state.patternIdAt(gridX, gridY, nPatterns);

                if (patternId == null) {
                    continue;
                }

                var pattern = this.model.patterns[((PatternId) patternId).asIndex];
                for (int iy = 0; iy < N; iy++) {
                    for (int ix = 0; ix < N; ix++) {
                        var sourcePos = pattern.localPosToSourcePos(new Vec2i(ix, iy), N);
                        var tile = source[sourcePos.x, sourcePos.y];
                        var outputPos = N * new Vec2i(gridX, gridY) + new Vec2i(ix, iy);
                        output[outputPos] = tile;
                    }
                }
            }

            return output;
        }
    }
}
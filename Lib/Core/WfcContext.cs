namespace Wfc {
    /// <summary>Runs WFC</summary>
    public class WfcContext {
        public Model model;
        public State state;
        public System.Random random = new System.Random();

        public WfcContext(Model model, State state) {
            this.model = model;
            this.state = state;
        }

        public enum AdvanceStatus {
            /// <summary>Just in proress</summary>
            Continue,
            /// <summary>Every cell is filled in respect to the local similarity constraint (<c>AdjacencyRule</c>)</summary>
            Success,
            /// <summary>A contradiction is reached, where some cell has no possible pattern</summary>
            Fail,
        }

        public bool run<T>(T observer) where T : iObserver {
            while (true) {
                switch (observer.advance(this)) {
                    case AdvanceStatus.Continue:
                        continue;
                    case AdvanceStatus.Success:
                        // TODO: remove debug print
                        System.Console.WriteLine("SUCCESS");
                        return true;
                    case AdvanceStatus.Fail:
                        System.Console.WriteLine("FAIL");
                        return false;
                }
            }
        }
    }

    public interface iObserver {
        WfcContext.AdvanceStatus advance(WfcContext cx);
    }

    /// <summary>Input to the core algorithm of WFC</summary>
    public class Model {
        // public Input input;
        public Vec2i gridSize;
        public PatternStorage patterns;
        public RuleData rule;

        public Model(Vec2i gridSize, PatternStorage patterns, RuleData rule) {
            // this.input = input;
            this.gridSize = gridSize;
            this.patterns = patterns;
            this.rule = rule;
        }

        /// <summary>If the output is not periodic, filter out positions outside of the output area</summary>
        public bool filterPos(int x, int y) {
            var size = this.gridSize;
            return x < 0 || x >= size.x || y < 0 || y >= size.y;
        }
    }
}
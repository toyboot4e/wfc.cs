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

        public void onRestart(State state) {
            this.state = state;
        }

        public Map getOutput() {
            return this.state.getOutput(
                this.model.input.outputSize.x,
                this.model.input.outputSize.y,
                this.model.input.source,
                this.model.input.N,
                this.model.patterns
            );
        }
    }

    public interface iObserver {
        WfcContext.AdvanceStatus advance(WfcContext cx);
    }

    /// <summary>
    /// Creates input for the wave function collapse algorithm (overlapping model)
    /// i.e. patterns and a rule to place them
    /// </summary>
    public class Model {
        public Input input;
        public PatternStorage patterns;
        public RuleData rule;

        public Model(Input input, PatternStorage patterns, RuleData rule) {
            this.input = input;
            this.patterns = patterns;
            this.rule = rule;
        }

        /// <summary>Original input from a user</summary>
        public class Input {
            public Map source;
            public int N;
            public Vec2i outputSize;
        }

        /// <summary>If the output is not periodic, filter out positions outside of the output area</summary>
        public bool filterPos(int x, int y) {
            var size = this.input.outputSize;
            return x < 0 || x >= size.x || y < 0 || y >= size.y;
        }
    }
}
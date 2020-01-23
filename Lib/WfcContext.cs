using System.Collections.Generic;
using System.Diagnostics;

namespace Wfc.Overlap {
    /// <summary>The top interface of the wave function collapse algorithm</summary>
    public class WfcContext {
        public readonly Model model;
        public readonly State state;
        public readonly System.Random random = new System.Random();

        public WfcContext(Map source, int N, Vec2 outputSize) {
            Debug.Assert(N >= 2, $"each pattern must be greater than or equal to 2x2 (N = {N})");
            this.model = new Model(source, N, outputSize);
            this.state = new State(outputSize.x, outputSize.y, this.model.patterns, ref this.model.rule);
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

        /// <summary>Returns if it succeeded</summary>
        public void runJustOnce() {
            var observer = new Observer(this.model.input.outputSize, this.state);
            observer.advance(this);
        }

        /// <summary>Solves the problem with local similarity constraint. True if succeeded</summary>
        public bool run() {
            var observer = new Observer(this.model.input.outputSize, this.state);
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

        /// <summary>To observe generation</summary>
        public IEnumerable<AdvanceStatus> runIter() {
            var observer = new Observer(this.model.input.outputSize, this.state);
            while (true) {
                var status = observer.advance(this);
                yield return status;
                System.Console.WriteLine(status);
                switch (status) {
                    case AdvanceStatus.Continue:
                        break;
                    case AdvanceStatus.Success:
                        System.Console.WriteLine("SUCCESS");
                        yield break;
                    case AdvanceStatus.Fail:
                        System.Console.WriteLine("FAIL");
                        yield break;
                }
            }
        }

        public enum AdvanceStatus {
            /// <summary>Just in proress</summary>
            Continue,
            /// <summary>Every cell is filled in respect to the local similarity constraint (<c>AdjacencyRule</c>)</summary>
            Success,
            /// <summary>A contradiction is reached, where some cell has no possible pattern</summary>
            Fail,
        }
    }
}
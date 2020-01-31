namespace Wfc {
    public class WfcOverlap : WfcContext {
        public WfcOverlap(Model model, State state) : base(model, state) { }

        public static WfcOverlap create(Map source, int N, Vec2i outputSize) {
            var model = Wfc.Overlap.ModelBuilder.create(source, 3, outputSize);
            var state = new State(outputSize.x, outputSize.y, model.patterns, ref model.rule);
            return new WfcOverlap(model, state);
        }

        public bool run() {
            return this.run(new Wfc.Overlap.Observer(this.model.input.outputSize, this.state));
        }
    }

    // public class WfcAdjacent : WfcContext {
    //     public WfcAdjacent(Model model, State state) : base(model, state) { }

    //     public static WfcOverlap create(Map source, int N, Vec2i outputSize) {
    //         var model = Wfc.Overlap.ModelBuilder.create(source, 3, outputSize);
    //         var state = new State(outputSize.x / N, outputSize.y / N, model.patterns, ref model.rule);
    //         return new WfcOverlap(model, state);
    //     }

    //     public bool run() {
    //         return this.run(new Wfc.Adjacent.Observer(this.model.input.outputSize, this.state));
    //     }
    // }
}
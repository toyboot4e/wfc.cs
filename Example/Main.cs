using System;
using System.IO;
using Wfc;

namespace Wfc.Example {
    class Program {
        static void Main(string[] args) {
            var wfc = new WfcInput() {
                mapFile = "Example/Res/a.txt",
                    inputSize = new Vec2(6, 6),
                    outputSize = new Vec2(30, 30),
                    N = 3,
            }.createModel();
            // Console.WriteLine("======================");
            // wfc.patterns.print();
            // Console.WriteLine("======================");
            wfc.run();
            wfc.output.print();

            // var another = new WfcInput() {
            //     mapFile = "Res/b.txt",
            //         inputSize = new Vec2(3, 3),
            //         outputSize = new Vec2(30, 30),
            //         N = 3,
            // }.create();
            // another.run();
            // another.output.print();
        }
    }

    class WfcInput {
        public string mapFile;
        public Vec2 inputSize;
        public Vec2 outputSize;
        public int N;

        public Overlap.Model createModel() {
            string asciiMap = File.ReadAllText(this.mapFile);
            var nl = Environment.NewLine;
            Console.WriteLine($@"Source map:{nl}{asciiMap}{nl}");

            var map = MapExt.fromString(asciiMap, this.inputSize.x, this.inputSize.y);
            return new Overlap.Model(map, this.N, this.outputSize.x, this.outputSize.y);
        }
    }
}
using System;
using System.IO;
using Wfc.Overlap;

namespace Wfc.Example {
    class Program {
        static void Main(string[] args) {
            // var sourceMap = loadAsciiMap("Example/Res/a.txt", new Vec2(6, 6));
            // var input = new Model.Input() {
            //     source = sourceMap,
            //     N = 3,
            //     outputSize = new Vec2(30, 30),
            // };

            // var sourceMap = loadAsciiMap("Example/Res/c.txt", new Vec2(7, 7));
            // var input = new Model.Input() {
            //     source = sourceMap,
            //     N = 3,
            //     outputSize = new Vec2(50, 50),
            // };

            var sourceMap = loadAsciiMap("Example/Res/wide.txt", new Vec2(7, 7));
            var input = new Model.Input() {
                source = sourceMap,
                N = 3,
                outputSize = new Vec2(50, 50),
            };

            var wfc = new Model(input);

            Console.WriteLine("======================");
            wfc.patterns.print();
            Console.WriteLine("======================");
            wfc.rule.print(wfc.patterns.len);

            wfc.run();
            var output = wfc.state.getOutput(input.outputSize.x, input.outputSize.y, sourceMap, input.N, wfc.patterns);

            Console.WriteLine("Output:");
            output.print();
        }

        static string nl => Environment.NewLine;

        static Map loadAsciiMap(string path, Vec2 inputSize) {
            string asciiMap = File.ReadAllText(path);
            Console.WriteLine($@"Source map:{nl}{asciiMap}{nl}");
            return MapExt.fromString(asciiMap, inputSize.x, inputSize.y);
        }
    }
}
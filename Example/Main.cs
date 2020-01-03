using System;
using System.IO;
using Wfc.Overlap;

namespace Wfc.Example {
    class Program {
        static void Main(string[] args) {
            // var sourceMap = loadAsciiMap("Example/Res/wide.txt", new Vec2(7, 7));
            var sourceMap = loadAsciiMap("Example/Res/rect.txt", new Vec2(11, 11));
            var cx = new Context(sourceMap, 3, new Vec2(50, 50));

            var model = cx.model;
            var input = model.input;

            {
                // Console.WriteLine("======================");
                // model.patterns.print();
                // Console.WriteLine("======================");
                // model.rule.print(model.patterns.len);

                // var state = new State(50, 50, model.patterns, ref model.rule);
                // Test.testInitialEnableCounter(50, 50, model.patterns, ref model.rule);
            }

            cx.run();
            var output = cx.state.getOutput(input.outputSize.x, input.outputSize.y, sourceMap, input.N, cx.model.patterns);

            {
                Console.WriteLine("=== Output: ===");
                output.print();
            }
        }

        static string nl => Environment.NewLine;

        static Map loadAsciiMap(string path, Vec2 inputSize) {
            string asciiMap = File.ReadAllText(path);
            Console.WriteLine($@"=== Source map ==={nl}{asciiMap}{nl}");
            return MapExt.fromString(asciiMap, inputSize.x, inputSize.y);
        }
    }
}
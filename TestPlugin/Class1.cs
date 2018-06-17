using System;
using TestPluginHost;
using NPluginsManager;

namespace TestPlugin {
    public static class AdderTest {
        [Hook("testadder")]
        public static void Adder1(int a, int b) {
            Console.WriteLine("Adder 1: {0} {1}", a, b);
        }

        [Hook("testadder")]
        public static void Adder2(int a, int b) {
            Console.WriteLine("Adder 2: {0} {1}", a, b);
        }
    }

    [Hook("testcontainer")]
    public class TestContainer: IPlugin<PluginArgsContainer> {
        public void Execute(PluginArgsContainer argument) {
            Console.WriteLine("Executed TestContainer: {0}", argument.value);
            argument.value++;
        }

        void IPlugin.Execute() {}
    }

    [Hook("testcontainer")]
    public class TestContainer2: IPlugin<PluginArgsContainer> {
        public void Execute(PluginArgsContainer argument) {
            Console.WriteLine("Executed TestContainer 2: {0}", argument.value);
            argument.value += 10;
        }

        void IPlugin.Execute() {}
    }
}

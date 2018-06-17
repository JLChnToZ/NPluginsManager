using System;
using NPluginsManager;

namespace TestPluginHost {
    class Program {
        static PluginsManager pluginsManager;
        static string key;
        
        static void Main(string[] args) {
            pluginsManager = new PluginsManager();
            pluginsManager.Load("TestPlugin.dll", out key);
            RunTest();
            Console.ReadKey(false);
        }

        static void RunTest() {
            Action<int, int> adder = pluginsManager.GetHookDelegate<Action<int, int>>("testadder");
            Console.WriteLine("Test invoke {0} {1}", 1, 2);
            adder?.Invoke(1, 2);
            Console.WriteLine("Test invoke {0} {1}", 2, 3);
            adder?.Invoke(2, 3);
            PluginArgsContainer testContainer = new PluginArgsContainer {
                value = 1
            };
            Console.WriteLine("PluginArgsContainer {0}", testContainer.value);
            pluginsManager.ExecutePlugin("testcontainer", testContainer);
            Console.WriteLine("PluginArgsContainer {0}", testContainer.value);
        }
    }

    public class PluginArgsContainer {
        public int value;
    }
}

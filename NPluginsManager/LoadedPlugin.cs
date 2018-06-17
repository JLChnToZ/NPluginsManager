using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace NPluginsManager {
    internal class LoadedPlugin {
        private const BindingFlags CTOR_BINDINGS =
            BindingFlags.NonPublic |
            BindingFlags.Public |
            BindingFlags.Instance;
        private const BindingFlags STATIC_MEMBERS_BINDINGS =
            BindingFlags.NonPublic |
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.FlattenHierarchy;
        private static readonly Type pluginInterface = typeof(IPlugin);
        private static readonly Type[] emptyTypes = new Type[0];

        public readonly string pluginKey;
        public readonly Assembly assembly;
        private readonly IDictionary<string, ICollection<IPlugin>> plugins =
            new Dictionary<string, ICollection<IPlugin>>();
        private readonly IDictionary<DelegateHookKey, Delegate> hooks =
            new Dictionary<DelegateHookKey, Delegate>();

        public LoadedPlugin(string pluginKey, Assembly assembly) {
            this.pluginKey = pluginKey;
            this.assembly = assembly;
            
            foreach (Type pluginClass in (
                from type in assembly.GetTypes()
                where pluginInterface.IsAssignableFrom(type)
                select type
            )) {
                if (!(pluginClass.GetConstructor(CTOR_BINDINGS, null, CallingConventions.HasThis, emptyTypes, null)?
                    .Invoke(null) is IPlugin plugin))
                    continue;
                foreach (HookAttribute hook in pluginClass.GetCustomAttributes<HookAttribute>(false)) {
                    if (!plugins.TryGetValue(hook.usage, out ICollection<IPlugin> pluginList))
                        plugins.Add(hook.usage, pluginList = new List<IPlugin>());
                    pluginList.Add(plugin);
                }
            }
        }

        public Delegate GetHookDelegate(string usage, Type delegateType) {
            DelegateHookKey cacheKey = new DelegateHookKey(usage, delegateType);
            if (hooks.TryGetValue(cacheKey, out Delegate cachedDelegate))
                return cachedDelegate;
            Delegate[] delegates = (
                from type in assembly.GetTypes()
                from method in type.GetMethods(STATIC_MEMBERS_BINDINGS)
                where (from hook in method.GetCustomAttributes<HookAttribute>(false)
                       where string.Equals(hook.usage, usage, StringComparison.Ordinal)
                       select hook).Any()
                select Delegate.CreateDelegate(delegateType, method, false) into @delegate
                where @delegate != null
                select @delegate
            ).ToArray();
            if (delegates.Length == 0) return null;
            Delegate combinedDelegates = Delegate.Combine(delegates);
            hooks.Add(cacheKey, combinedDelegates);
            return combinedDelegates;
        }

        public bool ExecutePlugin(string usage, ICollection<IPlugin> results) {
            if (!plugins.TryGetValue(usage, out ICollection<IPlugin> pluginList))
                return false;
            foreach (IPlugin plugin in pluginList) {
                plugin.Execute();
                results?.Add(plugin);
            }
            return true;
        }

        public bool ExecutePlugin<T>(string usage, T argument, ICollection<IPlugin<T>> results) {
            if (!plugins.TryGetValue(usage, out ICollection<IPlugin> pluginList))
                return false;
            foreach (IPlugin plugin in pluginList) {
                if (!(plugin is IPlugin<T> typedPlugin))
                    continue;
                typedPlugin.Execute(argument);
                results?.Add(typedPlugin);
            }
            return true;
        }

        public bool ExecutePlugin<T, U>(string usage, T argument, ICollection<U> results) {
            if (!plugins.TryGetValue(usage, out ICollection<IPlugin> pluginList))
                return false;
            foreach (IPlugin plugin in pluginList) {
                if (!(plugin is IPlugin<T, U> typedPlugin))
                    continue;
                U result = typedPlugin.Execute(argument);
                results?.Add(result);
            }
            return true;
        }

        public bool ExecutePluginChain(string usage, ICollection<IPluginChain> results) {
            if (!plugins.TryGetValue(usage, out ICollection<IPlugin> pluginList))
                return false;
            foreach (IPlugin plugin in pluginList) {
                if (!(plugin is IPluginChain pluginChain))
                    continue;
                results?.Add(pluginChain);
                if (!pluginChain.Execute())
                    continue;
                else
                    return true;
            }
            return false;
        }

        public bool ExecutePluginChain(string usage, out IPluginChain result) {
            if (!plugins.TryGetValue(usage, out ICollection<IPlugin> pluginList)) {
                result = null;
                return false;
            }
            foreach (IPlugin plugin in pluginList) {
                if (!(plugin is IPluginChain pluginChain))
                    continue;
                result = pluginChain;
                if (!pluginChain.Execute())
                    continue;
                else
                    return true;
            }
            result = null;
            return false;
        }

        public bool ExecutePluginChain<T>(string usage, T argument, ICollection<IPluginChain<T>> results) {
            if (!plugins.TryGetValue(usage, out ICollection<IPlugin> pluginList))
                return false;
            foreach (IPlugin plugin in pluginList) {
                if (!(plugin is IPluginChain<T> pluginChain))
                    continue;
                results?.Add(pluginChain);
                if (!pluginChain.Execute(argument))
                    continue;
                else
                    return true;
            }
            return false;
        }

        public bool ExecutePluginChain<T>(string usage, T argument, out IPluginChain<T> result) {
            if (!plugins.TryGetValue(usage, out ICollection<IPlugin> pluginList)) {
                result = null;
                return false;
            }
            foreach (IPlugin plugin in pluginList) {
                if (!(plugin is IPluginChain<T> pluginChain))
                    continue;
                result = pluginChain;
                if (!pluginChain.Execute(argument))
                    continue;
                else
                    return true;
            }
            result = null;
            return false;
        }
    }
}

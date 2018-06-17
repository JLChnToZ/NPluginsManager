using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace NPluginsManager {
    /// <summary>
    /// Handles plugins
    /// </summary>
    public class PluginsManager {
        private const BindingFlags CTOR_BINDINGS =
            BindingFlags.Public |
            BindingFlags.NonPublic;
        private const BindingFlags STATIC_MEMBERS_BINDINGS =
            BindingFlags.NonPublic |
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.FlattenHierarchy;

        private readonly IDictionary<string, LoadedPlugin> loadedPlugins = new Dictionary<string, LoadedPlugin>();
        
        /// <summary>
        /// Loads a plugin with a file path.
        /// </summary>
        /// <param name="path">File path to the assembly</param>
        /// <param name="pluginKey">The key string reference to this loaded plugin</param>
        /// <returns><c>true</c> if load successful, otherwise <c>false</c></returns>
        public bool Load(string path, out string pluginKey) {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            path = Path.GetFullPath(path);
            if (!File.Exists(path))
                throw new FileNotFoundException("Requested plugin assembly not found.", path);
            if (loadedPlugins.ContainsKey(path)) {
                pluginKey = string.Empty;
                return false;
            }
            LoadedPlugin plugin = new LoadedPlugin(path, Assembly.LoadFile(path));
            loadedPlugins.Add(plugin.pluginKey, plugin);
            pluginKey = path;
            return true;
        }

        /// <summary>
        /// Loads a plugin with raw assembly binaries.
        /// </summary>
        /// <param name="coffBlob">The COFF image format byte array that contains the plugin</param>
        /// <param name="pluginKey">The key string reference to this loaded plugin</param>
        /// <returns><c>true</c> if load successful, otherwise <c>false</c></returns>
        public bool Load(byte[] coffBlob, out string pluginKey) {
            if (coffBlob == null)
                throw new ArgumentNullException(nameof(coffBlob));
            using (HashAlgorithm hasher = SHA512.Create()) {
                string key = BitConverter.ToString(hasher.ComputeHash(coffBlob));
                if (loadedPlugins.ContainsKey(key)) {
                    pluginKey = string.Empty;
                    return false;
                }
                LoadedPlugin plugin = new LoadedPlugin(key, Assembly.Load(coffBlob));
                loadedPlugins.Add(plugin.pluginKey, plugin);
                pluginKey = key;
                return true;
            }
        }

        /// <summary>
        /// Loads a plugin with an assembly name.
        /// </summary>
        /// <param name="assemblyName">Assembly name that contains the plugin</param>
        /// <param name="pluginKey">The key string reference to this loaded plugin</param>
        /// <returns><c>true</c> if load successful, otherwise <c>false</c></returns>
        public bool Load(AssemblyName assemblyName, out string pluginKey) {
            if (assemblyName == null)
                throw new ArgumentNullException(nameof(assemblyName));
            string key = assemblyName.FullName;
            if (loadedPlugins.ContainsKey(key)) {
                pluginKey = string.Empty;
                return false;
            }
            LoadedPlugin plugin = new LoadedPlugin(key, Assembly.Load(assemblyName));
            loadedPlugins.Add(plugin.pluginKey, plugin);
            pluginKey = key;
            return true;
        }

        /// <summary>
        /// Gets the combined delegate with specific <see cref="HookAttribute.usage"/> tag
        /// within <see cref="HookAttribute"/> from all loaded plugins.
        /// </summary>
        /// <typeparam name="T">The delegate matches the requested methods</typeparam>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <returns>The combined delegate</returns>
        /// <remarks>
        /// Under the hood it will iterate all loaded plugins and collect the matched delegates
        /// (either perform a search or just take a cached version from each plugins)
        /// and combine them in every call, which can be very time consuming.
        /// Therefore, for best performance, unless new plugin is loaded,
        /// please cache the result if they are reusable.
        /// </remarks>
        public T GetHookDelegate<T>(string usage) where T : MulticastDelegate {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            Type delegateType = typeof(T);
            Delegate[] delegates = (
                from plugin in loadedPlugins.Values
                select plugin.GetHookDelegate(usage, delegateType) into @delegate
                where @delegate != null
                select @delegate
            ).ToArray();
            return delegates.Length > 0 ? Delegate.Combine(delegates) as T : null;
        }

        /// <summary>
        /// Gets the combined delegate with specific <see cref="HookAttribute.usage"/> tag
        /// within <see cref="HookAttribute"/> from specific plugins.
        /// </summary>
        /// <typeparam name="T">The delegate matches the requested methods</typeparam>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <param name="pluginKey">The plugin key</param>
        /// <returns>The combined delegate</returns>
        public T GetHookDelegate<T>(string usage, string pluginKey) where T : MulticastDelegate {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            if (pluginKey == null)
                throw new ArgumentNullException(nameof(pluginKey));
            if (!loadedPlugins.TryGetValue(pluginKey, out LoadedPlugin plugin))
                return null;
            return plugin.GetHookDelegate(usage, typeof(T)) as T;
        }

        /// <summary>
        /// Executes plugin entry points with specific <see cref="HookAttribute.usage"/> tag.
        /// </summary>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <returns>Executed plugin entry points instances array</returns>
        public IPlugin[] ExecutePlugin(string usage) {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            List<IPlugin> result = new List<IPlugin>();
            ExecutePlugin(usage, result);
            return result.ToArray();
        }

        /// <summary>
        /// Executes plugin entry points accepts 1 argument with specific <see cref="HookAttribute.usage"/> tag.
        /// </summary>
        /// <typeparam name="T">Argument type</typeparam>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <param name="argument">The argument should be passed to the plugins</param>
        /// <returns>Executed plugin entry points instances array</returns>
        public IPlugin<T>[] ExecutePlugin<T>(string usage, T argument) {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            List<IPlugin<T>> result = new List<IPlugin<T>>();
            ExecutePlugin(usage, argument, result);
            return result.ToArray();
        }

        /// <summary>
        /// Executes returnable plugin entry points accepts 1 argument with specific <see cref="HookAttribute.usage"/> tag.
        /// </summary>
        /// <typeparam name="T">Argument type</typeparam>
        /// <typeparam name="U">Return type</typeparam>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <param name="argument">The argument should be passed to the plugins</param>
        /// <returns>Results array from the plugins</returns>
        public U[] ExecutePlugin<T, U>(string usage, T argument) {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            List<U> result = new List<U>();
            ExecutePlugin(usage, argument, result);
            return result.ToArray();
        }

        /// <summary>
        /// Executes plugin entry points with specific <see cref="HookAttribute.usage"/> tag,
        /// optionally store them into the <paramref name="results"/>.
        /// </summary>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <param name="results">Collections that will be appended the executed entry points instances, can be <c>null</c>.</param>
        /// <returns><c>true</c> when successful, otherwise <c>false</c>.</returns>
        public bool ExecutePlugin(string usage, ICollection<IPlugin> results) {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            bool result = false;
            foreach (LoadedPlugin plugin in loadedPlugins.Values)
                result |= plugin.ExecutePlugin(usage, results);
            return result;
        }

        /// <summary>
        /// Executes plugin entry points accepts 1 argument with specific <see cref="HookAttribute.usage"/> tag,
        /// optionally store them into the <paramref name="results"/>.
        /// </summary>
        /// <typeparam name="T">Argument type</typeparam>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <param name="argument">The argument should be passed to the plugins</param>
        /// <param name="results">Collections that will be appended the executed entry points instances, can be <c>null</c>.</param>
        /// <returns><c>true</c> when successful, otherwise <c>false</c>.</returns>
        public bool ExecutePlugin<T>(string usage, T argument, ICollection<IPlugin<T>> results) {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            bool result = false;
            foreach (LoadedPlugin plugin in loadedPlugins.Values)
                result |= plugin.ExecutePlugin(usage, argument, results);
            return result;
        }

        /// <summary>
        /// Executes returnable plugin entry points accepts 1 argument with specific <see cref="HookAttribute.usage"/> tag,
        /// optionally store the results into <paramref name="results"/>.
        /// </summary>
        /// <typeparam name="T">Argument type</typeparam>
        /// <typeparam name="U">Return type</typeparam>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <param name="argument">The argument should be passed to the plugins</param>
        /// <param name="results">Collections that will be appended the executed entry points instances, can be <c>null</c>.</param>
        /// <returns><c>true</c> when successful, otherwise <c>false</c>.</returns>
        public bool ExecutePlugin<T, U>(string usage, T argument, ICollection<U> results) {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            bool result = false;
            foreach (LoadedPlugin plugin in loadedPlugins.Values)
                result |= plugin.ExecutePlugin(usage, argument, results);
            return result;
        }

        /// <summary>
        /// Executes returnable plugin entry points accepts 1 argument with specific <see cref="HookAttribute.usage"/> tag,
        /// optionally store them into the <paramref name="results"/>.
        /// </summary>
        /// <typeparam name="T">Argument type</typeparam>
        /// <typeparam name="U">Return type</typeparam>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <param name="argument">The argument should be passed to the plugins</param>
        /// <param name="results">Collections that will be appended the results, can be <c>null</c>.</param>
        /// <returns><c>true</c> when successful, otherwise <c>false</c>.</returns>
        public bool ExecutePlugin<T, U>(string usage, T argument, ICollection<IPlugin<T, U>> results) {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            bool result = false;
            foreach (LoadedPlugin plugin in loadedPlugins.Values)
                result |= plugin.ExecutePlugin(usage, argument, results);
            return result;
        }

        /// <summary>
        /// Executes chainable plugin entry points with specific <see cref="HookAttribute.usage"/> tag
        /// until it tells to stop propagation, and returns it.
        /// </summary>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <returns>The first ex</returns>
        public IPluginChain ExecutePluginChain(string usage) {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            ExecutePluginChain(usage, out IPluginChain result);
            return result;
        }

        /// <summary>
        /// Executes chainable plugin entry points accepts 1 argument with specific <see cref="HookAttribute.usage"/> tag
        /// until it tells to stop propagation, and returns it.
        /// </summary>
        /// <typeparam name="T">Argument type</typeparam>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <param name="argument">The argument should be passed to the plugins</param>
        /// <returns></returns>
        public IPluginChain<T> ExecutePluginChain<T>(string usage, T argument) {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            ExecutePluginChain(usage, argument, out IPluginChain<T> result);
            return result;
        }

        /// <summary>
        /// Executes chainable plugin entry points with specific <see cref="HookAttribute.usage"/> tag
        /// until it tells to stop propagation, and optionally store them all into <paramref name="results"/>.
        /// </summary>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <param name="results"></param>
        /// <returns><c>true</c> when successful, otherwise <c>false</c>.</returns>
        public bool ExecutePluginChain(string usage, ICollection<IPluginChain> results) {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            foreach (LoadedPlugin plugin in loadedPlugins.Values)
                if (plugin.ExecutePluginChain(usage, results))
                    return true;
            return false;
        }

        /// <summary>
        /// Executes chainable plugin entry points with specific <see cref="HookAttribute.usage"/> tag
        /// until it tells to stop propagation, and returns it.
        /// </summary>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <param name="results">Collections that will be appended the executed entry points instances, can be <c>null</c>.</param>
        /// <returns><c>true</c> when successful, otherwise <c>false</c>.</returns>
        public bool ExecutePluginChain(string usage, out IPluginChain result) {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            foreach (LoadedPlugin plugin in loadedPlugins.Values)
                if (plugin.ExecutePluginChain(usage, out result))
                    return true;
            result = null;
            return false;
        }

        /// <summary>
        /// Executes typed chainable plugin entry points with specific <see cref="HookAttribute.usage"/> tag
        /// until it tells to stop propagation, and optionally store them all into <paramref name="results"/>.
        /// </summary>
        /// <typeparam name="T">Argument type</typeparam>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <param name="argument">The argument should be passed to the plugins</param>
        /// <param name="results">Collections that will be appended the executed entry points instances, can be <c>null</c>.</param>
        /// <returns><c>true</c> when successful, otherwise <c>false</c>.</returns>
        public bool ExecutePluginChain<T>(string usage, T argument, ICollection<IPluginChain<T>> results) {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            foreach (LoadedPlugin plugin in loadedPlugins.Values)
                if (plugin.ExecutePluginChain(usage, argument, results))
                    return true;
            return false;
        }

        /// <summary>
        /// Executes chainable plugin entry points accepts 1 argument with specific <see cref="HookAttribute.usage"/> tag
        /// until it tells to stop propagation, and returns it.
        /// </summary>
        /// <typeparam name="T">Argument type</typeparam>
        /// <param name="usage">The usage tag defined in the loaded plugins</param>
        /// <param name="argument">The argument should be passed to the plugins</param>
        /// <param name="results">Collections that will be appended the executed entry points instances, can be <c>null</c>.</param>
        /// <returns><c>true</c> when successful, otherwise <c>false</c>.</returns>
        public bool ExecutePluginChain<T>(string usage, T argument, out IPluginChain<T> result) {
            if (usage == null)
                throw new ArgumentNullException(nameof(usage));
            foreach (LoadedPlugin plugin in loadedPlugins.Values)
                if (plugin.ExecutePluginChain(usage, argument, out result))
                    return true;
            result = null;
            return false;
        }
    }
}

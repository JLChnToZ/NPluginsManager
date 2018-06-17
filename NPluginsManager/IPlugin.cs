namespace NPluginsManager {
    /// <summary>
    /// Base interface for plugins entry point.
    /// </summary>
    public interface IPlugin {
        /// <summary>
        /// Plugin execution entry method.
        /// Must implement this to make plugin works.
        /// </summary>
        void Execute();
    }

    /// <summary>
    /// Interface for chainable plugins, which can be stop propagate if <c>true</c> is returned in <see cref="Execute"/>.
    /// </summary>
    public interface IPluginChain: IPlugin {
        /// <summary>
        /// Plugin execution entry method.
        /// Must implement this to make plugin works.
        /// </summary>
        /// <returns><c>true</c> to stop propagation, otherwise <c>false</c>.</returns>
        new bool Execute();
    }

    /// <summary>
    /// Plugin interface that accept 1 argument.
    /// Plugin hosts can use this interface variant to commnunicate with the plugin.
    /// </summary>
    /// <typeparam name="T">Argument type</typeparam>
    public interface IPlugin<T>: IPlugin {
        /// <summary>
        /// Plugin execution entry method.
        /// </summary>
        /// <param name="argument">Argument given from the plugin host.</param>
        void Execute(T argument);
    }

    /// <summary>
    /// Plugin interface that accepts 1 argument with return values.
    /// Plugin hosts can use this interface variant to commnunicate with the plugin.
    /// </summary>
    /// <typeparam name="T">Argument type</typeparam>
    /// <typeparam name="U">Return type</typeparam>
    public interface IPlugin<T, U>: IPlugin {
        /// <summary>
        /// Plugin execution entry method.
        /// </summary>
        /// <param name="argument">Argument given from the plugin host.</param>
        /// <returns>Value that pass back to the plugin host.</returns>
        U Execute(T argument);
    }

    /// <summary>
    /// Chainable plugin interface that accepts 1 argument and allows to stop propagate if needed.
    /// Plugin hosts can use this interface variant to commnunicate with the plugin.
    /// </summary>
    /// <typeparam name="T">Argument type</typeparam>
    public interface IPluginChain<T>: IPluginChain {
        /// <summary>
        /// Plugin execution entry method.
        /// </summary>
        /// <param name="argument">Argument given from the plugin host.</param>
        /// <returns><c>true</c> to stop propagation, otherwise <c>false</c>.</returns>
        bool Execute(T argument);
    }
}

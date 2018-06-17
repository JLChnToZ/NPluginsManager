using System;

namespace NPluginsManager {
    /// <summary>
    /// Specify a static method or class that is used to be the plugin entry point/hook.
    /// </summary>
    /// <remarks>
    /// This attribute is for the plugin writters.
    /// For class entry points, they must implement <see cref="IPlugin"/> or one of its variant;
    /// for static method entry points, they cannot have any return values or output parameters.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class HookAttribute: Attribute {
        /// <summary>
        /// Special usage tag that the plugin host defined
        /// </summary>
        public readonly string usage;

        /// <summary>
        /// Defines the hook.
        /// </summary>
        /// <param name="usage">Special usage tag that the plugin host defined</param>
        public HookAttribute(string usage) {
            this.usage = usage;
        }
    }
}

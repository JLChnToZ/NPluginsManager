using System;

namespace NPluginsManager {
    internal struct DelegateHookKey: IEquatable<DelegateHookKey> {
        public readonly string usage;
        public readonly Type signature;

        public DelegateHookKey(string usage, Type signature) {
            this.usage = usage;
            this.signature = signature;
        }

        public bool Equals(DelegateHookKey other) {
            return string.Equals(usage, other.usage, StringComparison.Ordinal) && signature.Equals(other.signature);
        }

        public override bool Equals(object obj) {
            return obj is DelegateHookKey && Equals((DelegateHookKey)obj);
        }

        public override int GetHashCode() {
            var hashCode = 2044209749;
            hashCode = hashCode * -1521134295 + StringComparer.Ordinal.GetHashCode(usage);
            hashCode = hashCode * -1521134295 + signature.GetHashCode();
            return hashCode;
        }
    }
}

using System;

namespace System.Diagnostics.CodeAnalysis
{
    // For compatibility with .Net 5.+
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class DoesNotReturnAttribute : Attribute
    {
        public DoesNotReturnAttribute()
        {
        }
    }
}
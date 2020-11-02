using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Extensions
{
    internal static class New<T>
    {
        internal static Func<T> Instance(Type type = null)
        {
            var t = type != null ? type : typeof(T);

            if (t == typeof(string))
                return Expression.Lambda<Func<T>>(Expression.Constant(string.Empty)).Compile();

            if (t.HasDefaultConstructor())
                return Expression.Lambda<Func<T>>(Expression.New(t)).Compile();

            return () => (T)FormatterServices.GetUninitializedObject(t);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MapChecker
{
    internal static class ValueGenerators
    {
        private static readonly Dictionary<Type, Func<IEnumerable>> _generators = 
            new Dictionary<Type, Func<IEnumerable>>();

        static ValueGenerators()
        {
            _generators = new Dictionary<Type, Func<IEnumerable>>
            {
                { typeof(bool), GenerateBoolValue },
                { typeof(string), GenerateStringValue },
                { typeof(int), GenerateIntValue },
                { typeof(long), GenerateLongValue },
            };
        }

        public static Func<IEnumerable> GetValueGenerator(Type type, bool isKey = false)
        {
            Func<IEnumerable> generator = null;
            if (_generators.ContainsKey(type))
                generator = _generators[type];

            if (generator == null)
            {
                if (type.IsGenericType)
                    generator = () => GenerateGenericType(type);
                else
                    generator = () => GenerateObjectValue(type);
            }

            if (isKey)
            {
                if  (type == typeof (int))
                    return () => generator().Cast<int>().Where(i => i > 0);
                else if (type == typeof(long))
                    return () => generator().Cast<long>().Where(i => i > 0);
                else if (type.IsClass)
                    return () => generator().Cast<object>().Where(i => i != null && i != "");
            }

            return generator;

            //throw new NotImplementedException("Value generator for type: " + type.Name + " not implemented");
        }

        private static IEnumerable GenerateGenericType(Type type)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof (IList<>) || genericTypeDefinition == typeof(List<>))
            {
                for (int i=0; i<3; i++)
                    yield return Activator.CreateInstance(typeof (List<>).MakeGenericType(type.GetGenericArguments()));
            }
            else
                throw new NotImplementedException("Cannot create generic type: " + type.Name);
        }

        private static IEnumerable<object> GenerateObjectValue(Type type)
        {
            yield return null;
            yield return Activator.CreateInstance(type);
            yield return null;
            yield return Activator.CreateInstance(type);
            yield return null;
            yield return Activator.CreateInstance(type);
        }

        private static IEnumerable<string> GenerateStringValue()
        {
            yield return null;
            yield return "";
            for (int i = 0; i < 5; i++)
                yield return "Test Value " + i;
        }

        private static IEnumerable<bool> GenerateBoolValue()
        {
            return new[] { true, false };
        }

        private static IEnumerable<int> GenerateIntValue()
        {
            return new[] { 0, 9832, 21983792, 32712 };
        }

        private static IEnumerable<long> GenerateLongValue()
        {
            return new[] { 0, 239847298374, 19823, 1298371 };
        }


        internal static IEnumerable ForConstant()
        {
            yield return new object();
        }

        internal static IEnumerable FromConstant(object value)
        {
            yield return value;
        }

        internal static IEnumerable ForEnumerableOf<T>()
        {
            //yield return new List<T>();
            //yield return new List<T> {Activator.CreateInstance<T>()};
            yield return Enumerable.Range(0, 3).Select(x => Activator.CreateInstance<T>()).ToList();
        }
    }
}
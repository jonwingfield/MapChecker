using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MapChecker
{
    public static class QuickCheck
    {
        public static QuickCheckOperation<T> Operation<T>(Func<T> action)
        {
            return new QuickCheckOperation<T>(action);
        }
    }

    public static class QuickCheckExtensions
    {
        public static bool MapsFrom<T>(this T subject, T value)
        {
            Assert.AreEqual(subject, value, GetMessage<T>());
            return false;
        }

        public static Predicate<T> MatchOn<T>(this IEnumerable<T> enumerable, Predicate<T> matcher)
        {
            return matcher;
        }

        private static string GetMessage<T>()
        {
            return "in verification " + 
                QuickCheckOperationContext.CurrentVerification;
        }

        public static bool MapsFromOrDefault<T>(this T subject, T value, Action<T> orDefault) where T : class
        {
            if (value != null)
                Assert.AreEqual(value, subject, GetMessage<T>());
            else
                orDefault(subject);
            return false;
        }
    }
}

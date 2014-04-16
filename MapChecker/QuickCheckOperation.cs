using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MapChecker
{
    public partial class QuickCheckOperation<T>
    {
        public readonly Func<T> Action;

        internal QuickCheckOperation(Func<T> action)
        {
            Action = action;
        }

        public void VerifyMappings(params Expression<Action<T>>[] verifications)
        {
            var action = this.Action;
            foreach (var verification in verifications)
            {
                var arg = (MemberExpression) ((MethodCallExpression) verification.Body).Arguments[0];
                var valueGenerator = ValueGenerators.GetValueGenerator(arg.Type);
                var thing = verification.Compile();

                var closure = (System.Runtime.CompilerServices.Closure)thing.Target;

                ValueSetter valueSetter = new ConstantValueSetter();
                var expression = ((MethodCallExpression) verification.Body).Arguments[1];
                if (!(expression is ConstantExpression))
                {
                    valueSetter = new ReflectionValueSetter((MemberExpression)expression, closure.Constants);
                }
                else
                {
                    valueGenerator = ValueGenerators.ForConstant;
                }

                QuickCheckOperationContext.CurrentVerification = verification.Body.ToString();
                foreach (var value in valueGenerator())
                {
                    valueSetter.SetValue(value);
                    var result = action();
                    thing(result);
                }
            }
        }

        public WhenContext When(Expression<Func<bool>> possibilities, params Action<T>[] verifications)
        {
            var targets = ((System.Runtime.CompilerServices.Closure) possibilities.Compile().Target).Constants;

            ParseAndVerify(possibilities.Body, verifications, targets);

            return new WhenContext(possibilities, this);
        }

        private void ParseAndVerify(Expression possibilities, IEnumerable<Action<T>> verifications, object[] targets)
        {
            if (possibilities is BinaryExpression)
            {
                var exp = possibilities as BinaryExpression;

                if (exp.Left is BinaryExpression)
                    ParseAndVerify(exp.Left, verifications, targets);
                else
                    Verify(exp, verifications, targets);

                if (exp.Right is BinaryExpression)
                    ParseAndVerify(exp.Right, verifications, targets);
            }
        }

        private void Verify(BinaryExpression exp, IEnumerable<Action<T>> verifications, object[] targets)
        {
            var member = exp.Left as MemberExpression;

            if (exp.NodeType == ExpressionType.Equal)
            {
                foreach (var verification in verifications)
                {
                    var valueSetter = new ReflectionValueSetter(member, targets);
                    valueSetter.SetValue(((ConstantExpression)exp.Right).Value);
                    verification(this.Action());
                }
            }
        }

        public class WhenContext
        {
            private Expression<Func<bool>> possibilities;
            private QuickCheckOperation<T> quickCheckOperation;

            public WhenContext(Expression<Func<bool>> possibilities, QuickCheckOperation<T> quickCheckOperation)
            {
                this.possibilities = possibilities;
                this.quickCheckOperation = quickCheckOperation;
            }

            public WhenContext When(Expression<Func<bool>> possibilities, params Action<T>[] verifications)
            {
                return quickCheckOperation.When(possibilities, verifications);
            }
        }

        public QuickCheckCollectionMapping<TItem> ForCollectionMapping<TItem>(Func<T, IEnumerable<TItem>> func)
            where  TItem : new()
        {
            return new QuickCheckCollectionMapping<TItem>(this, func);
        }
    }

    // TODO: modify the method call to MapsFrom in VerifyMappings so we don't need this variable
    internal static class QuickCheckOperationContext
    {
        [ThreadStatic]
        internal static string CurrentVerification;
    }
}
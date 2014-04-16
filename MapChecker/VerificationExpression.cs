using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MapChecker
{
    interface MappingExpression
    {
        Type SourceType { get; }
        Type Type { get; }
        MemberExpression[] MemberExpressions { get; }
    }

    internal static class MappingExpressionExtensions
    {
        internal static ValueSetter ValueSetterFor<TMappedFrom>(
            this IEnumerable collectionValue, MappingExpression expression, bool isKey = false)
        {           
            if (expression.MemberExpressions.Any())
                return new CollectionValueSetter<TMappedFrom>(
                        expression.MemberExpressions,
                        collectionValue,
                        ValueGenerators.GetValueGenerator(expression.Type, isKey));
            else
            {
                return new CollectionValueSetter<TMappedFrom>(
                        expression.MemberExpressions,
                        collectionValue,
                        ValueGenerators.GetValueGenerator(expression.Type, isKey),
                        (memberExpression, context) => new ConstantValueSetter());
            }
        }

        internal static ValueSetter ValueSetterFor<TMappedFrom, TMappedFrom2>(
            this IEnumerable collectionValue, IEnumerable collectionValue2, MatchExpression expression,
            bool isKey = false)
        {
            if (expression.SourceType != null && expression.SourceType == typeof (TMappedFrom))
            {
                return new CollectionValueSetter<TMappedFrom2>(
                    expression.MemberExpressions,
                    new CompositeEnumerable<object>(collectionValue2, collectionValue),
                    ValueGenerators.GetValueGenerator(expression.Type, isKey),
                    (exp, context) => new CorrellatedValueSetter(exp, (IEnumerable)context[0]));
            }
            else
            {
                return new CollectionValueSetter<TMappedFrom2>(
                        expression.MemberExpressions,
                        collectionValue,
                        ValueGenerators.GetValueGenerator(expression.Type, isKey));
            }
        }
    }

    class VerificationExpression : MappingExpression
    {
        public readonly Type DestinationMemberType;
        public MemberExpression SourceMemberExpression;
        private readonly Type _sourceType;

        public VerificationExpression(LambdaExpression verification)
        {
            // verification method
            var arg = (MemberExpression)((MethodCallExpression)verification.Body).Arguments[0];
            DestinationMemberType = arg.Type;

            var _valueSetter = new ConstantValueSetter();
            var expression = ((MethodCallExpression)verification.Body).Arguments[1];
            if (expression is MemberExpression && expression.RootObject() is ParameterExpression)
            {
                SourceMemberExpression = (MemberExpression) expression;
            }
            // otherwise the SourceExpression will be null, which means it's a constant

            expression = expression.RootObject();
            _sourceType = expression.Type;
        }

        public Type Type
        {
            get
            {
                return DestinationMemberType;
            }
        }

        public MemberExpression[] MemberExpressions
        {
            get { return SourceMemberExpression != null ? new[] {SourceMemberExpression} : new MemberExpression[0]; }
        }

        public Type SourceType
        {
            get { return _sourceType; }
        }
    }

    class MatchExpression : MappingExpression
    {
        private readonly Type _type;
        public readonly MemberExpression[] _memberExpressions;
        private readonly Type _sourceType = null;

        public MatchExpression(LambdaExpression matchPredicate)
        {
            var matchOnArg = ((dynamic)matchPredicate.Body).Arguments[1].Body.Left;
            var Right = ((dynamic)matchPredicate.Body).Arguments[1].Body.Right;

            var memberExpressions = new List<MemberExpression>();

            if (matchPredicate.Parameters.Count > 1)
            {
                _sourceType = matchPredicate.Parameters[1].Type;
                _memberExpressions = new MemberExpression[] { matchOnArg, Right };
            }
            else
            {
                _memberExpressions = new[] { (MemberExpression)matchOnArg };
            }

            _type = matchOnArg.Type;
        }

        public Type Type
        {
            get { return _type; }
        }

        public MemberExpression[] MemberExpressions
        {
            get { return _memberExpressions; }
        }

        public Type SourceType
        {
            get { return _sourceType; }
        }
    }

    class ClosureExpression
    {
        private readonly InstanceMemberAccessor _closureAccessor;

        public ClosureExpression(LambdaExpression matchPredicate, Delegate compiledMatchExpression)
        {
            var lambdaArgs = ((MethodCallExpression)matchPredicate.Body).Arguments;
            if (lambdaArgs.Count != 2) throw new InvalidOperationException("No source object found. Please specify a source object by using the following form: .From(dest => source.MatchOn(src => src.Field == dest.Field)");

            var fromMember = ((MemberExpression)lambdaArgs[0]).Member;

            var matchClosure = ((System.Runtime.CompilerServices.Closure)compiledMatchExpression.Target).Constants;

            _closureAccessor =
                new FieldAccessor(matchClosure[0].GetType().GetField(fromMember.Name))
                .ForInstance(matchClosure[0]);
        }

        public InstanceMemberAccessor Accessor
        {
            get { return _closureAccessor; }
        }
    }
}

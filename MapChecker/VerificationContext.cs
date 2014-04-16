using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace MapChecker
{
    interface VerificationContext<in TItem>
    {
        void SetValue(object o);
        void Verify(TItem item, object context);
    }

    class VerificationContext<TItem, TMappedFrom, TMappedFrom2> : VerificationContext<TItem>
        where TMappedFrom : class, new()
        where TMappedFrom2 : class, new()
    {
        private readonly ValueSetter _valueSetter;
        private readonly ValueSetter _matchOnValueSetter;
        private readonly ValueSetter _matchOnValueSetter2;
        private readonly Action<TItem, TMappedFrom, TMappedFrom2, int> _verifyFunc;
        private readonly Expression<Action<TItem, TMappedFrom, TMappedFrom2, int>> _verification;
        private readonly object _collectionValue;
        private readonly IEnumerable _collectionValue2;
        private readonly Func<TItem, Predicate<TMappedFrom>> _matchCompiled;
        private readonly Func<TItem, TMappedFrom, Predicate<TMappedFrom2>> _matchCompiled2;

        public VerificationContext(
            Expression<Action<TItem, TMappedFrom, TMappedFrom2, int>> verification,
            IEnumerable collectionValue,
            IEnumerable collectionValue2,
            Expression<Func<TItem, Predicate<TMappedFrom>>> matchPredicate,
            Func<TItem, Predicate<TMappedFrom>> matchCompiled,
            Expression<Func<TItem, TMappedFrom, Predicate<TMappedFrom2>>> matchPredicate2,
            Func<TItem, TMappedFrom, Predicate<TMappedFrom2>> matchCompiled2)
        {
            _verification = verification;
            _collectionValue = collectionValue;
            _collectionValue2 = collectionValue2;
            _matchCompiled = matchCompiled;
            _matchCompiled2 = matchCompiled2;
            _verifyFunc = verification.Compile();

            var setterExpression = new VerificationExpression(verification);
            _valueSetter = (setterExpression.SourceType == typeof (TMappedFrom) ? collectionValue : collectionValue2)
                .ValueSetterFor<TMappedFrom>(setterExpression);
            _matchOnValueSetter = collectionValue.ValueSetterFor<TMappedFrom>(
                new MatchExpression(matchPredicate), isKey: true);
            _matchOnValueSetter2 = collectionValue.ValueSetterFor<TMappedFrom, TMappedFrom2>(
                collectionValue2,
                new MatchExpression(matchPredicate2), isKey: true);
        }

        public void SetValue(object o)
        {
            _valueSetter.SetValue(o);
            _matchOnValueSetter.SetValue(o);
            _matchOnValueSetter2.SetValue(o);
        }

        public void Verify(TItem item, object context)
        {
            QuickCheckOperationContext.CurrentVerification = _verification.Body.ToString();

            var mappedFrom1 = ((IEnumerable<TMappedFrom>)_collectionValue).First(
                new Func<TMappedFrom, bool>(_matchCompiled(item)));
            _verifyFunc(item, 
                mappedFrom1,
                ((IEnumerable<TMappedFrom2>)_collectionValue2).First(
                    new Func<TMappedFrom2, bool>(_matchCompiled2(item, mappedFrom1))),
                (int)context);
        }
    }
    
    class VerificationContext<TItem, TMappedFrom> : VerificationContext<TItem> 
        where TMappedFrom : class, new()
    {
        private readonly ValueSetter _valueSetter;
        private readonly ValueSetter _matchOnValueSetter;
        private readonly Action<TItem, TMappedFrom> _verifyFunc;
        private readonly object _collectionValue;
        private readonly Func<TItem, Predicate<TMappedFrom>> _matchCompiled;

        public VerificationContext(Expression<Action<TItem, TMappedFrom>> verification, 
            IEnumerable collectionValue,
            Expression<Func<TItem, Predicate<TMappedFrom>>> matchPredicate,
            Func<TItem, Predicate<TMappedFrom>> matchCompiled)
        {
            _collectionValue = collectionValue;
            _matchCompiled = matchCompiled;
            _verifyFunc = verification.Compile();

            _valueSetter = collectionValue.ValueSetterFor<TMappedFrom>(
                new VerificationExpression(verification));
            _matchOnValueSetter = collectionValue.ValueSetterFor<TMappedFrom>(
                new MatchExpression(matchPredicate));
        }

        public virtual void SetValue(object o)
        {
            _valueSetter.SetValue(o);
            _matchOnValueSetter.SetValue(o);
        }

        public virtual void Verify(TItem item, object context)
        {
            _verifyFunc(item, ((IEnumerable<TMappedFrom>)_collectionValue).First(
                                        new Func<TMappedFrom, bool>(_matchCompiled(item))));
        }
    }

    class CollectionVerificationContext<TItem> : VerificationContext<TItem>, IEnumerable<VerificationContext<TItem>>
    {
        private readonly List<VerificationContext<TItem>> _contexts;
        public CollectionVerificationContext(IEnumerable<VerificationContext<TItem>> contexts)
        {
            _contexts = contexts.ToList();
        }

        public void SetValue(object o)
        {
            foreach (var verificationContext in _contexts)
            {
                verificationContext.SetValue(o);
            }
        }

        public void Verify(TItem item, object context)
        {
            foreach (var verificationContext in _contexts)
            {
                verificationContext.Verify(item, context);
            }
        }

        public IEnumerator<VerificationContext<TItem>> GetEnumerator()
        {
            return _contexts.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    class VerificationContextWrapper<TItem> : VerificationContext<TItem>
    {
        private readonly VerificationContext<TItem> _wrapped;
        private readonly Action<object> _afterSet;

        public VerificationContextWrapper(VerificationContext<TItem> wrapped,
            Action<object> afterSet)
        {
            _wrapped = wrapped;
            _afterSet = afterSet;
        }

        public void SetValue(object o)
        {
            _wrapped.SetValue(o);
            _afterSet(o);
        }

        public void Verify(TItem item, object context)
        {
            _wrapped.Verify(item, context);
        }
    }
}

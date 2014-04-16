using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MapChecker
{
    public partial class QuickCheckOperation<T>
    {
        public class QuickCheckCollectionMapping<TItem> where TItem : new()
        {
            private readonly QuickCheckOperation<T> _quickCheckOperation;
            private Func<T, IEnumerable<TItem>> _destCollection;

            public QuickCheckCollectionMapping(QuickCheckOperation<T> quickCheckOperation,
                Func<T, IEnumerable<TItem>> destCollection)
            {
                _quickCheckOperation = quickCheckOperation;
                this._destCollection = destCollection;
            }

            public MappingFrom<TMappedFrom1> From<TMappedFrom1>(
                Expression<Func<TItem, Predicate<TMappedFrom1>>> matchPredicate)
                where TMappedFrom1 : class, new()
            {
                return new MappingFrom<TMappedFrom1>(this, matchPredicate);
            }

            public class MappingFrom<TMappedFrom1, TMappedFrom2>
                where TMappedFrom1 : class, new()
                where TMappedFrom2 : class, new()
            {
                private MappingFrom<TMappedFrom1> _mappingFrom1;
                private Expression<Func<TItem, TMappedFrom1, Predicate<TMappedFrom2>>> _matchPredicate;
                private Func<TItem, TMappedFrom1, Predicate<TMappedFrom2>> _matchCompiled;
                private ClosureExpression _closureExpression;
                private List<Action<TMappedFrom1, TMappedFrom2, int>> _beforeOperations;

                public MappingFrom(
                    QuickCheckCollectionMapping<TItem> collectionMapping,
                    MappingFrom<TMappedFrom1> mappingFrom,
                    Expression<Func<TItem, TMappedFrom1, Predicate<TMappedFrom2>>> matchPredicate) 
                {
                    _mappingFrom1 = mappingFrom;
                    _matchPredicate = matchPredicate;
                    _matchCompiled = matchPredicate.Compile();
                    _closureExpression = new ClosureExpression(matchPredicate, _matchCompiled);
                }

                public void Verify(params Expression<Action<TItem, TMappedFrom1, TMappedFrom2, int>>[] verifications)
                {
                    var value2Enumerator = ValueGenerators.ForEnumerableOf<TMappedFrom2>().GetEnumerator();

                    _mappingFrom1.Verify(source1Collection =>
                    {
                        value2Enumerator.MoveNext();
                        var source2Collection = (IEnumerable)value2Enumerator.Current;
                        _closureExpression.Accessor.Value = source2Collection;

                        return GetVerificationContext(verifications, 
                            source1Collection,
                            source2Collection);
                    });
                }

                private void BeforeOperations(IEnumerable source1Collection, IEnumerable source2Collection)
                {
                    if (_beforeOperations != null)
                    {
                        _beforeOperations.ForEach(op =>
                        {
                            int i = 0;
                            foreach (var item in new CompositeEnumerable<object>(
                                source1Collection, source2Collection))
                            {
                                op((TMappedFrom1) ((object[]) item)[0],
                                    (TMappedFrom2) ((object[]) item)[1],
                                    i++);
                            }
                        });
                    }
                }

                public MappingFrom<TMappedFrom1, TMappedFrom2> ForEach(params Action<TMappedFrom1, TMappedFrom2, int>[] beforeOperations)
                {
                    _beforeOperations = beforeOperations.ToList();
                    return this;
                }

                private VerificationContext<TItem> GetVerificationContext(
                    IEnumerable<Expression<Action<TItem, TMappedFrom1, TMappedFrom2, int>>> verifications,
                    IEnumerable collectionValue,
                    IEnumerable collectionValue2)
                {
                    return new VerificationContextWrapper<TItem>(
                        new CollectionVerificationContext<TItem>(
                            verifications.Select(verification =>
                                new VerificationContext<TItem, TMappedFrom1, TMappedFrom2>(verification,
                                    collectionValue,
                                    collectionValue2,
                                    _mappingFrom1.matchPredicate,
                                    _mappingFrom1.matchCompiled,
                                    _matchPredicate,
                                    _matchCompiled))),
                        // run BeforeOperations after everything else
                        afterSet: (item) => BeforeOperations(collectionValue, collectionValue2));
                }

            }

            public class MappingFrom<TMappedFrom1>
                where TMappedFrom1 : class, new()
            {
                private readonly QuickCheckCollectionMapping<TItem> collectionMapping;
                internal readonly Expression<Func<TItem, Predicate<TMappedFrom1>>> matchPredicate;
                internal readonly Func<TItem, Predicate<TMappedFrom1>> matchCompiled;
                private ClosureExpression _closureExpression;

                public MappingFrom(QuickCheckCollectionMapping<TItem> quickCheckCollectionMapping,
                    Expression<Func<TItem, Predicate<TMappedFrom1>>> matchPredicate)
                {
                    this.collectionMapping = quickCheckCollectionMapping;
                    this.matchPredicate = matchPredicate;

                    matchCompiled = matchPredicate.Compile();
                    _closureExpression = new ClosureExpression(matchPredicate, matchCompiled);
                }

                public void Verify(params Expression<Action<TItem, TMappedFrom1>>[] verifications)
                {
                    Verify(collectionValue => GetVerificationContext(verifications, 
                            (IEnumerable)collectionValue));
                }

                internal void Verify(Func<IEnumerable, VerificationContext<TItem>> verificationContextFactory)
                {
                    Func<IEnumerable> item1Generator = ValueGenerators.ForEnumerableOf<TMappedFrom1>;

                    foreach (var collectionValue in item1Generator())
                    {
                        int i = 0;

                        // arrange
                        _closureExpression.Accessor.Value = collectionValue;

                        var verificationContext = verificationContextFactory((IEnumerable)collectionValue);
                        verificationContext.SetValue(null);

                        // act
                        var result = collectionMapping._quickCheckOperation.Action();

                        // assert
                        foreach (var item in collectionMapping._destCollection(result))
                        {
                            verificationContext.Verify(item, i++);
                        }
                    }                    
                }

                private VerificationContext<TItem> GetVerificationContext(
                    IEnumerable<Expression<Action<TItem, TMappedFrom1>>> verifications,
                    IEnumerable collectionValue)
                {
                    return new CollectionVerificationContext<TItem>(
                        verifications.Select(verification =>
                            new VerificationContext<TItem, TMappedFrom1>(verification, 
                                collectionValue, 
                                matchPredicate,
                                matchCompiled)));
                }

                public MappingFrom<TMappedFrom1, TMappedFrom2> From<TMappedFrom2>(
                    Expression<Func<TItem, TMappedFrom1, Predicate<TMappedFrom2>>> matchPredicate)
                    where TMappedFrom2 : class, new()
                {
                    return new MappingFrom<TMappedFrom1, TMappedFrom2>(this.collectionMapping,
                        this, matchPredicate);
                }
            }
        }
    }
}

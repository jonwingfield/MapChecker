using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MapChecker
{
    internal class CompositeEnumerable<T> : IEnumerable<IEnumerable<T>>
    {
        private readonly List<IEnumerable<T>> _enumerables;

        public CompositeEnumerable(IEnumerable<IEnumerable<T>> enumerables)
        {
            _enumerables = enumerables.ToList();
        }

        public CompositeEnumerable(params IEnumerable[] enumerables)
        {
            _enumerables = enumerables.Cast<IEnumerable<T>>().ToList();
        }

        public IEnumerator GetEnumerator()
        {
            return new CompositeEnumerator<T>(_enumerables);
        }

        IEnumerator<IEnumerable<T>> IEnumerable<IEnumerable<T>>.GetEnumerator()
        {
            return new CompositeEnumerator<T>(_enumerables);
        }

        internal class CompositeEnumerator<T> : IEnumerator<IEnumerable<T>>
        {
            private readonly List<IEnumerator<T>> _enumerators;

            public CompositeEnumerator(IEnumerable<IEnumerable<T>> _enumerables)
            {
                _enumerators = _enumerables.Select(e => e.GetEnumerator()).ToList();
            }

            public object Current
            {
                get
                {
                    return _enumerators.Select(x => x.Current).ToArray();
                }
            }

            public bool MoveNext()
            {
                return _enumerators.All(e => e.MoveNext());
            }

            public void Reset()
            {
                _enumerators.ForEach(e => e.Reset());
            }

            IEnumerable<T> IEnumerator<IEnumerable<T>>.Current
            {
                get { return _enumerators.Select(x => x.Current); }
            }

            public void Dispose()
            {
                _enumerators.ForEach(x => x.Dispose());
            }
        }
    }
}
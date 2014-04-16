using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace MapChecker
{
    internal interface ValueSetter
    {
        void SetValue(object value);
    }

    internal class ReflectionValueSetter : ValueSetter
    {
        private readonly MemberExpression _memberExpression;
        private readonly object[] _context;
        private Action<object> setValueOnInput = null; 

        public ReflectionValueSetter(MemberExpression memberExpression, object[] context)
        {
            if (memberExpression == null)
                throw new ArgumentNullException("memberExpression");

            _memberExpression = memberExpression;
            _context = context;
            Setup();
        }

        private void Setup()
        {
            var param = _context[0];
            var accessor = _memberExpression.Member.GetAccessor();
            
            setValueOnInput = v => accessor.SetValue(param, v);

            if (_memberExpression.Expression is MemberExpression)
            {
                param = GetValue(_memberExpression, param, true);
            }
        }

        private object GetValue(MemberExpression memberExpression, object param, bool first)
        {
            var expression = memberExpression.Expression as MemberExpression;
            if (expression != null)
            {
                param = GetValue(expression, param, false);
                // don't load the value from the last property/field on the stack, because that's the field we want to set
                if (first) return param;
            }

            var accessor = memberExpression.Member.GetAccessor();
            return NullCheck(param, accessor.GetValue(param), accessor);
        }

        private object NullCheck(object parentParam, object param, MemberAccessor accessor)
        {
            var type = accessor.MemberType;
            if (param == null && !type.IsPrimitive && type != typeof(string))
            {
                param = Activator.CreateInstance(type);
                accessor.SetValue(parentParam, param);
                return param;
            }
            return param;
        }

        public virtual void SetValue(object value)
        {
            setValueOnInput(value);
        }
    }

    class ConstantValueSetter : ValueSetter
    {
        public void SetValue(object value)
        {
            
        }
    }

    class CollectionValueSetter<TCollection> : ValueSetter
    {
        private readonly IEnumerable<MemberExpression> _memberExpression;
        private readonly IEnumerable _collectionValue;
        private readonly Func<IEnumerable> _valueGenerator;
        private readonly Func<MemberExpression, object[], ValueSetter> _valueSetterFactory;

        public CollectionValueSetter(
            IEnumerable<MemberExpression> memberExpression,
            IEnumerable collectionValue,
            Func<IEnumerable> valueGenerator,
            Func<MemberExpression, object[], ValueSetter> valueSetterFactory = null)
        {
            if (valueSetterFactory == null)
                valueSetterFactory = (expression, context) =>
                    new ReflectionValueSetter(expression, context);

            // TODO: Complete member initialization
            this._memberExpression = memberExpression;
            this._collectionValue = collectionValue;
            _valueGenerator = valueGenerator;
            _valueSetterFactory = valueSetterFactory;
        }

        public void SetValue(object value)
        {
            var enumerator = _valueGenerator().GetEnumerator();
            foreach (var item in _collectionValue)
            {
                if (!enumerator.MoveNext())
                {
                    enumerator.Reset();
                    enumerator.MoveNext();
                }

                foreach (var memberExpression in _memberExpression)
                {
                    _valueSetterFactory(memberExpression, new [] { item })
                        .SetValue(value ?? enumerator.Current);
                }
            }
        }
    }

    class CorrellatedValueSetter : ValueSetter
    {
        private readonly MemberExpression _memberExpression;
        private readonly IEnumerable _objects;

        public CorrellatedValueSetter(
            MemberExpression memberExpression,
            IEnumerable objects)
        {
            _memberExpression = memberExpression;
            _objects = objects;
        }

        public void SetValue(object value)
        {
            foreach (var item in _objects)
            {
                var rootObject = _memberExpression.RootObject();

                if (rootObject.Type == item.GetType())
                    new ReflectionValueSetter(_memberExpression, new[] { item })
                        .SetValue(value);                        
            }
        }
    }

}

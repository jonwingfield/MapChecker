using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MapChecker
{
    static class MemberExpressionAccessorExtensions
    {
        public static MemberAccessor GetAccessor(this MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo)
                return new PropertyAccessor((PropertyInfo)memberInfo);
            if (memberInfo is FieldInfo)
                return new FieldAccessor((FieldInfo)memberInfo);

            throw new NotImplementedException("Can't support type of field: " + memberInfo.GetType().Name);
        }

        public static InstanceMemberAccessor ForInstance(this MemberAccessor accessor, object instance)
        {
            return new InstanceMemberAccessor(accessor, instance);            
        }
    }

    /// <summary>
    /// This just provides a common interface for Property/Field value access
    /// </summary>
    interface MemberAccessor
    {
        void SetValue(object instance, object value);
        object GetValue(object instance);
        Type MemberType { get; }
    }

    class PropertyAccessor : MemberAccessor
    {
        private readonly PropertyInfo _property;

        public PropertyAccessor(PropertyInfo property)
        {
            if (property == null) throw new ArgumentNullException("property");
            _property = property;
        }

        public void SetValue(object instance, object value)
        {
            _property.SetValue(instance, value);
        }

        public object GetValue(object instance)
        {
            return _property.GetValue(instance);
        }

        public Type MemberType
        {
            get { return _property.PropertyType; }
        }
    }

    class FieldAccessor : MemberAccessor
    {
        private readonly FieldInfo _field;

        public FieldAccessor(FieldInfo property)
        {
            if (property == null) throw new ArgumentNullException("property");
            _field = property;
        }

        public void SetValue(object instance, object value)
        {
            _field.SetValue(instance, value);
        }

        public object GetValue(object instance)
        {
            return _field.GetValue(instance);
        }

        public Type MemberType
        {
            get { return _field.FieldType; }
        }
    }

    class InstanceMemberAccessor
    {
        private readonly MemberAccessor _accessor;
        private readonly object _instance;

        public InstanceMemberAccessor(MemberAccessor accessor, object instance)
        {
            _accessor = accessor;
            _instance = instance;
        }

        public object Value
        {
            get
            {
                return _accessor.GetValue(_instance);
            }
            set
            {
                _accessor.SetValue(_instance, value);
            }
        }
    }
}

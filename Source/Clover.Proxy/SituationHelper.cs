using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Clover.Proxy
{
    public static class SituationHelper
    {
        public static Type[] GetInternalTypeFormArray(Type type)
        {
            if (type.IsGenericType)
            {
                return type.GetGenericArguments();
            }
            else if (type.IsArray)
            {
                return new Type[1] { type.GetElementType() };
            }
            return new Type[1] { type };
        }

        public static string GetExpression(Type type, string key)
        {
            Situation situation = GetSituation(type);
            string expression = "";
            switch (situation)
            {
                case Situation.SerializableNullableT:
                case Situation.Serializable:
                case Situation.SerializableArray:
                case Situation.SerializableIEnumerableOfT:
                case Situation.SerializableDictionary:
                    {
                        expression = key;
                        break;
                    }
                case Situation.UNSerializable:
                    {
                        expression = string.Format(" {0}==null ? null : new Serializable_{1}({0})", key, type.Name);
                        break;
                    }
                case Situation.Array:
                    {
                        Type[] types = GetInternalTypeFormArray(type);
                        expression = string.Format(
                            " {0}==null ? null : {0}.ToList().ConvertAll(p => p = {1}).ToArray()", key,
                            GetExpression(types[0], "p"));
                        break;
                    }
                case Situation.IEnumerableOfT:
                    {
                        Type[] types = GetInternalTypeFormArray(type);
                        expression = string.Format(" {0}==null ? null : {0}.ToList().ConvertAll(p => p = {1}) ", key,
                                                   GetExpression(types[0], "p"));
                        break;
                    }
                case Situation.Dictionary:
                    {
                        Type[] types = GetInternalTypeFormArray(type);
                        expression =
                            string.Format(" {0}==null ? null : {0}.ToDictionary(p => ({3})({1}) , p=> ({4})({2})) "
                                          , key
                                          , GetExpression(types[0], "p.Key")
                                          , GetExpression(types[1], "p.Value")
                                          , types[0].Name
                                          , types[1].Name
                                );
                        break;
                    }
            }
            return expression;
        }

        public static Situation GetSituation(Type type)
        {
            if (type.IsEnum)
            {
                return Situation.SerializableEnum;
            }
            if (type == typeof(string))
            {
                return Situation.Serializable;
            }
            if (type == typeof(DateTime))
            {
                return Situation.Serializable;
            }
            if (type == typeof(decimal))
            {
                return Situation.Serializable;
            }
            if (type == typeof(object))
            {
                return Situation.Serializable;
            }
            if (type.IsPrimitive)
            {
                return Situation.Serializable;
            }


            //if (t.Namespace.StartsWith("System."))
            //{
            //    return Situation.Serializable;
            //}

            if (type.IsArray)
            {
                if (GetSituation(type.GetElementType()) == Situation.Serializable)
                {
                    return Situation.SerializableArray;
                }
                return Situation.Array;
            }
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return Situation.IEnumerableOfT;
                }
                if (type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    return Situation.IEnumerableOfT;
                }
                if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    return Situation.SerializableNullableT;
                }
                Type[] interfaces = type.GetGenericTypeDefinition().GetInterfaces();

                foreach (Type @interface in interfaces)
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        if (IsSerializable(GetSituation(type.GetGenericArguments()[0])))
                            return Situation.SerializableIEnumerableOfT;
                        else
                            return Situation.IEnumerableOfT;
                    }
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    {
                        if (type.GetGenericArguments().Any(p => !IsSerializable(GetSituation(p))))
                            return Situation.Dictionary;
                        else
                            return Situation.SerializableDictionary;
                    }
                }
            }
            if (type.IsClass && type.IsSerializable)
            {
                return Situation.Serializable;
            }
            return Situation.UNSerializable;
        }

        public static bool IsSerializable(Situation situation)
        {
            return (situation & Situation.Serializable) != 0;
        }

        public static bool IsSerializable(Type type)
        {
            return IsSerializable(GetSituation(type));
        }

        public static IEnumerable<MemberInfo> GetMembers(Type type)
        {
            var list = new List<MemberInfo>();

            foreach (FieldInfo item in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                list.Add(item);
            }
            foreach (PropertyInfo item in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (item.CanRead && item.CanWrite)
                    list.Add(item);
            }
            return list;
        }

        public static Type GetRealType(MemberInfo member)
        {
            var fieldInfo = member as FieldInfo;
            if (fieldInfo != null)
            {
                return fieldInfo.FieldType;
            }

            var propertyType = member as PropertyInfo;
            if (propertyType != null)
            {
                return propertyType.PropertyType;
            }
            
            return member.DeclaringType;
        }

        public static Type[] GetToBeSerializableTypes(Type type)
        {
            switch (GetSituation(type))
            {
                case Situation.Array:
                    {
                        return new[] { type.GetElementType() };
                    }
                case Situation.Dictionary:
                    {
                        return type.GetGenericArguments();
                    }
                case Situation.IEnumerableOfT:
                    {
                        return type.GetGenericArguments();
                    }
                case Situation.SerializableNullableT:
                    {
                        return type.GetGenericArguments();
                    }
                case Situation.SerializableEnum:
                    {
                        return new Type[0];
                    }
                case Situation.SerializableDictionary:
                    {
                        return new Type[0];
                    }
                case Situation.UNSerializable:
                    {
                        return new[] { type };
                    }
            }
            return new Type[0];
        }

        public static string GetMethodName(Type type)
        {
            return GetMethodName(type, true);
        }

        public static string GetMethodName(Type type, bool includePrefix)
        {
            string typeName = null;
            if (type == typeof(string))
                typeName = "String";
            else if (type == typeof(Int32))
                typeName = "Int32";
            else
                typeName = type.Name;
            if (includePrefix)
                return "Get" + typeName;
            else
                return typeName;
        }
    }
}
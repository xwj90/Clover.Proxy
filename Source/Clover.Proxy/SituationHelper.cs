 
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
namespace Clover.Proxy
{



    public class SituationHelper
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
        public static string GetExpression(Type t, string key)
        {
            Situation situation = GetSituation(t);
            string expression = "";
            switch (situation)
            {
                case Situation.SerializableNullableT:
                case Situation.Serializable:
                case Situation.SerializableArray:
                case Situation.SerializableIEnumableT:
                case Situation.SerializableDirtionary:
                    {
                        expression = key;
                        break;
                    }
                case Situation.UnSerializable:
                    {
                        expression = string.Format(" {0}==null ? null : new Serializable_{1}({0})", key, t.Name);
                        break;
                    }
                case Situation.Array:
                    {
                        Type[] types = GetInternalTypeFormArray(t);
                        expression = string.Format(" {0}==null ? null : {0}.ToList().ConvertAll(p => p = {1}).ToArray()", key, GetExpression(types[0], "p"));
                        break;
                    }
                case Situation.IEnumableT:
                    {
                        Type[] types = GetInternalTypeFormArray(t);
                        expression = string.Format(" {0}==null ? null : {0}.ToList().ConvertAll(p => p = {1}) ", key, GetExpression(types[0], "p"));
                        break;
                    }
                case Situation.Dictionary:
                    {

                        Type[] types = GetInternalTypeFormArray(t);
                        expression = string.Format(" {0}==null ? null : {0}.ToDictionary(p => ({3})({1}) , p=> ({4})({2})) "
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

        public static Situation GetSituation(Type t)
        {
            if (t.IsEnum)
            {
                return Situation.SerializableEnum;
            }
            if (t == typeof(string))
            {
                return Situation.Serializable;
            }
            if (t == typeof(DateTime))
            {
                return Situation.Serializable;
            }
            if (t == typeof(decimal))
            {
                return Situation.Serializable;
            }
            if (t == typeof(object))
            {
                return Situation.Serializable;
            }
            if (t.IsPrimitive)
            {
                return Situation.Serializable;
            }


            //if (t.Namespace.StartsWith("System."))
            //{
            //    return Situation.Serializable;
            //}

            if (t.IsArray)
            {
                if (GetSituation(t.GetElementType()) == Situation.Serializable)
                {
                    return Situation.SerializableArray;
                }
                return Situation.Array;
            }
            if (t.IsGenericType)
            {
                if (t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return Situation.IEnumableT;
                }
                if (t.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    return Situation.IEnumableT;
                }
                if (t.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    return Situation.SerializableNullableT;
                }
                Type[] interfaces = t.GetGenericTypeDefinition().GetInterfaces();

                foreach (var @interface in interfaces)
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    {
                        if (IsSerialzable(SituationHelper.GetSituation(t.GetGenericArguments()[0])))
                            return Situation.SerializableIEnumableT;
                        else
                            return Situation.IEnumableT;
                    }
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    {
                        if (t.GetGenericArguments().Any(p => !IsSerialzable(SituationHelper.GetSituation(p))))
                            return Situation.Dictionary;
                        else
                            return Situation.SerializableDirtionary;
                    }
                }


            }
            if (t.IsClass && t.IsSerializable)
            {
                return Situation.Serializable;
            }
            return Situation.UnSerializable;

        }

        public static bool IsSerialzable(Situation situation)
        {
            return (situation & Situation.Serializable) != 0;
        }
        public static bool IsSerialzable(Type type)
        {
            return IsSerialzable(GetSituation(type));
        }

        public static IEnumerable<MemberInfo> GetMembers(Type type)
        {
            List<MemberInfo> list = new List<MemberInfo>();

            foreach (var item in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                list.Add(item);
            }
            foreach (var item in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (item.CanRead && item.CanWrite)
                    list.Add(item);
            }
            return list;
        }

        public static Type GetRealType(MemberInfo member)
        {
            if (member is FieldInfo)
            {
                return (member as FieldInfo).FieldType;
            }
            if (member is PropertyInfo)
            {
                return (member as PropertyInfo).PropertyType;
            }
            return member.DeclaringType;
        }

        public static Type[] GetToBeSerializableTypes(Type t)
        {
            switch (GetSituation(t))
            {
                case Situation.Array:
                    {
                        return new Type[] { t.GetElementType() };
                    }
                case Situation.Dictionary:
                    {
                        return t.GetGenericArguments();
                    }
                case Situation.IEnumableT:
                    {
                        return t.GetGenericArguments();
                    }
                case Situation.SerializableNullableT:
                    {
                        return t.GetGenericArguments();
                    }
                case Situation.SerializableEnum:
                    {
                        return new Type[0];
                    }
                case Situation.SerializableDirtionary:
                    {
                        return new Type[0];
                    }
                case Situation.UnSerializable:
                    {
                        return new Type[] { t };
                    }

            }
            return new Type[0];
        }


        public static string GetMethodName(Type type)
        {
            return GetMethodName(type, true);
        }
        public static string GetMethodName(Type type, bool inclduPrefix)
        {
            string typeName = null;
            if (type == typeof(string))
                typeName = "String";
            else if (type == typeof(Int32))
                typeName = "Int32";
            else
                typeName = type.Name;
            if (inclduPrefix)
                return "Get" + typeName;
            else
                return typeName;
        }



    }

}

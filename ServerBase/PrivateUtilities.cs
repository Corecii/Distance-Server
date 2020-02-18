using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

static class PrivateUtilities
{
    public static object getPrivateField(object obj, string fieldName)
    {
        return obj
            .GetType()
            .GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            )
            .GetValue(obj);
    }
    public static object getPrivateField(Type tp, object obj, string fieldName)
    {
        return tp
            .GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            )
            .GetValue(obj);
    }
    public static void setPrivateField(object obj, string fieldName, object value)
    {
        obj
            .GetType()
            .GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            )
            .SetValue(obj, value);
    }
    public static void setPrivateField(Type tp, object obj, string fieldName, object value)
    {
        tp
            .GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            )
            .SetValue(obj, value);
    }
    public static object getPrivateProperty(object obj, string propertyName)
    {
        return obj
            .GetType()
            .GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            )
            .GetValue(obj);
    }
    public static object getPrivateProperty(Type tp, object obj, string propertyName)
    {
        return tp
            .GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            )
            .GetValue(obj);
    }
    public static void setPrivateProperty(object obj, string propertyName, object value)
    {
        obj
            .GetType()
            .GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            )
            .SetValue(obj, value);
    }
    public static void setPrivateProperty(Type tp, object obj, string propertyName, object value)
    {
        tp
            .GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            )
            .SetValue(obj, value);
    }

    public static object callPrivateMethod(Type tp, object obj, string methodName, params object[] args)
    {
        return tp.GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance
        ).Invoke(obj, args);
    }

    public static object callPrivateMethod(object obj, string methodName, params object[] args)
    {
        return obj.GetType().GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance
        ).Invoke(obj, args);
    }
}

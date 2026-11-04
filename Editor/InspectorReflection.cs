using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SWeetJelly.Editor
{
    internal static class InspectorReflection
    {
        internal const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        internal static IEnumerable<Type> Hierarchy(Type type)
        {
            for (; type != null && type != typeof(UnityEngine.Object); type = type.BaseType) yield return type;
        }

        internal static FieldInfo Field(Type type, string name)
        {
            return Hierarchy(type).Select(t => t.GetField(name, Flags)).FirstOrDefault(f => f != null);
        }

        internal static FieldInfo ResolveField(Type type, string path)
        {
            FieldInfo field = null;
            foreach (string part in path.Replace(".Array.data[", "[").Split('.'))
            {
                int bracket = part.IndexOf('[');
                field = Field(type, bracket < 0 ? part : part.Substring(0, bracket));
                if (field == null) return null;
                type = field.FieldType;
                if (bracket >= 0) type = type.IsArray ? type.GetElementType() : type.GetGenericArguments().FirstOrDefault();
                if (type == null) return null;
            }
            return field;
        }

        internal static object Value(object target, string path)
        {
            if (string.IsNullOrEmpty(path)) return target;
            foreach (string part in path.Replace(".Array.data[", "[").Split('.'))
            {
                if (target == null) return null;
                int bracket = part.IndexOf('[');
                string name = bracket < 0 ? part : part.Substring(0, bracket);
                var field = Field(target.GetType(), name);
                if (field == null) throw new MissingFieldException(target.GetType().Name, name);
                target = field.GetValue(target);
                if (bracket >= 0)
                {
                    int index = int.Parse(part.Substring(bracket + 1, part.Length - bracket - 2));
                    target = target is IList list && index < list.Count ? list[index] : null;
                }
            }
            return target;
        }

        internal static object Owner(object target, string path)
        {
            int dot = path.LastIndexOf('.');
            return dot < 0 ? target : Value(target, path.Substring(0, dot));
        }

        private static void SetPath(object target, string[] parts, int offset, object value)
        {
            string part = parts[offset];
            int bracket = part.IndexOf('[');
            var field = Field(target.GetType(), bracket < 0 ? part : part.Substring(0, bracket));
            bool last = offset == parts.Length - 1;
            if (bracket < 0)
            {
                if (last) { field.SetValue(target, value); return; }
                object child = field.GetValue(target);
                SetPath(child, parts, offset + 1, value);
                if (child.GetType().IsValueType) field.SetValue(target, child);
            }
            else
            {
                var list = (IList)field.GetValue(target);
                int index = int.Parse(part.Substring(bracket + 1, part.Length - bracket - 2));
                if (last) { list[index] = value; return; }
                object child = list[index];
                SetPath(child, parts, offset + 1, value);
                if (child.GetType().IsValueType) list[index] = child;
            }
        }

        internal static MethodInfo Method(Type type, string name, object[] args)
        {
            args = args ?? Array.Empty<object>();
            foreach (var current in Hierarchy(type))
            {
                var matches = current.GetMethods(Flags).Where(m => m.Name == name && !m.ContainsGenericParameters &&
                    !m.GetParameters().Any(p => p.ParameterType.IsByRef) && Matches(m.GetParameters(), args)).ToArray();
                if (matches.Length > 1) throw new AmbiguousMatchException("Ambiguous Inspector method: " + name);
                if (matches.Length == 1) return matches[0];
            }
            throw new MissingMethodException(type.Name, name);
        }

        private static bool Matches(ParameterInfo[] parameters, object[] args)
        {
            if (parameters.Length != args.Length) return false;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (args[i] == null)
                {
                    if (parameters[i].ParameterType.IsValueType && Nullable.GetUnderlyingType(parameters[i].ParameterType) == null) return false;
                }
                else if (!parameters[i].ParameterType.IsInstanceOfType(args[i])) return false;
            }
            return true;
        }

        internal static bool Condition(object target, string name)
        {
            if (target == null) throw new InvalidOperationException("The Inspector condition owner is null.");
            var field = Field(target.GetType(), name);
            if (field != null)
            {
                if (field.FieldType != typeof(bool)) throw new InvalidOperationException(name + " must be bool.");
                return (bool)field.GetValue(target);
            }
            foreach (var type in Hierarchy(target.GetType()))
            {
                var property = type.GetProperty(name, Flags);
                if (property == null) continue;
                if (property.PropertyType != typeof(bool) || property.GetIndexParameters().Length != 0 || property.GetGetMethod(true) == null)
                    throw new InvalidOperationException(name + " must be a readable non-indexed bool property.");
                return (bool)property.GetValue(target);
            }
            var method = Method(target.GetType(), name, Array.Empty<object>());
            if (method.ReturnType != typeof(bool)) throw new InvalidOperationException(name + " must return bool.");
            return (bool)method.Invoke(target, null);
        }

        internal static Exception Unwrap(Exception exception)
        {
            while (exception is TargetInvocationException invocation && invocation.InnerException != null) exception = invocation.InnerException;
            return exception;
        }

        internal static void Invoke(UnityEngine.Object[] roots, string path, string name, object[] args, bool requireVoid = false)
        {
            // Validate every target before the first mutation.
            var owners = roots.Select(root => string.IsNullOrEmpty(path) ? root : Owner(root, path)).ToArray();
            var methods = owners.Select(owner => owner == null ? throw new InvalidOperationException("The action owner is null.") : Method(owner.GetType(), name, args)).ToArray();
            if (requireVoid && methods.Any(m => m.ReturnType != typeof(void))) throw new InvalidOperationException(name + " must return void.");
            if (!requireVoid) Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(name);
            Undo.RecordObjects(roots, name);
            try
            {
                for (int i = 0; i < owners.Length; i++)
                {
                    Exception failure = null;
                    try
                    {
                        var result = methods[i].Invoke(owners[i], args);
                        if (result != null) Debug.Log(result, roots[i]);
                    }
                    catch (Exception exception) { failure = exception; throw; }
                    finally
                    {
                        if (roots[i] != null)
                        {
                            try
                            {
                                if (owners[i].GetType().IsValueType && !string.IsNullOrEmpty(path))
                                {
                                    string ownerPath = path.Substring(0, path.LastIndexOf('.'));
                                    SetPath(roots[i], ownerPath.Replace(".Array.data[", "[").Split('.'), 0, owners[i]);
                                }
                                EditorUtility.SetDirty(roots[i]);
                                if (PrefabUtility.IsPartOfPrefabInstance(roots[i])) PrefabUtility.RecordPrefabInstancePropertyModifications(roots[i]);
                            }
                            catch { if (failure == null) throw; }
                        }
                    }
                }
            }
            finally { Undo.CollapseUndoOperations(group); }
        }
    }
}

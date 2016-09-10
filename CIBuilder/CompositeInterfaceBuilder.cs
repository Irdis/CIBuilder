using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CIBuilder
{
    public class CompositeInterfaceBuilder
    {
        private readonly string _componentName;

        public CompositeInterfaceBuilder()
            : this("__" + Guid.NewGuid().ToString("N"))
        {
        }

        public CompositeInterfaceBuilder(string componentName)
        {
            _componentName = componentName;
        }

        public object Build(IDictionary<Type, object> interfaces, out Type compositeInterface)
        {
            return Build(interfaces, typeof(object), out compositeInterface);
        }
        public object Build(IDictionary<Type, object> interfaces, Type baseType, out Type compositeInterface)
        {
            var implType = Generate(interfaces.Keys, baseType, out compositeInterface);
            return Activator.CreateInstance(implType, interfaces);
        }

        private Type Generate(ICollection<Type> interfaces, Type baseType, out Type compositeInterface)
        {
            var currentDomain = AppDomain.CurrentDomain;
            var assemName = new AssemblyName();
            assemName.Name = _componentName + "Assembly";
            var assemBuilder = currentDomain.DefineDynamicAssembly(assemName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemBuilder.DefineDynamicModule(_componentName + "Module");
            compositeInterface = CreateInterface(moduleBuilder, interfaces);
            var implementationType = CreateImpl(moduleBuilder, baseType, compositeInterface, interfaces);
            return implementationType;
        }

        private Type CreateImpl(ModuleBuilder moduleBuilder, Type baseType, Type interfaceType, ICollection<Type> interfaces)
        {
            var classBuilder = moduleBuilder.DefineType(_componentName + "Class",
                TypeAttributes.Class | TypeAttributes.Public,
                baseType, new[] { interfaceType });

            var fields = CreateFields(classBuilder, interfaces);
            var ctorBuilder = classBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                new[] { typeof(IDictionary<Type, object>) });
            CreateCtor(ctorBuilder, fields);
            foreach (var item in interfaces.Zip(fields, Tuple.Create))
            {
                foreach (var methodInfo in item.Item1.GetMethods())
                {
                    var parameterTypes = methodInfo.GetParameters().Select(info => info.ParameterType).ToArray();
                    var methodBuilder = classBuilder.DefineMethod(string.Join("_", item.Item1.Name, methodInfo.Name),
                        MethodAttributes.Public | MethodAttributes.Virtual,
                        CallingConventions.Standard,
                        methodInfo.ReturnType, parameterTypes);
                    CreateDelegateMethod(methodBuilder, methodInfo, item.Item2);
                }
            }
            return classBuilder.CreateType();
        }

        private IList<FieldBuilder> CreateFields(TypeBuilder builder, ICollection<Type> interfaces)
        {
            return interfaces.Select(
                (@interface, i) => builder.DefineField("field" + i, @interface, FieldAttributes.Private)).ToList();
        }

        private void CreateCtor(ConstructorBuilder ctorBuilder, IList<FieldBuilder> fields)
        {
            var gen = ctorBuilder.GetILGenerator();
            foreach (var fieldBuilder in fields)
            {
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Ldtoken, fieldBuilder.FieldType);
                gen.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle"));
                gen.Emit(OpCodes.Callvirt, typeof(IDictionary<Type, object>).GetMethod("get_Item"));
                gen.Emit(OpCodes.Castclass, fieldBuilder.FieldType);
                gen.Emit(OpCodes.Stfld, fieldBuilder);
            }
            gen.Emit(OpCodes.Ret);
        }

        private void CreateDelegateMethod(MethodBuilder methodBuilder, MethodInfo methodInfo, FieldBuilder fieldBuilder)
        {
            var gen = methodBuilder.GetILGenerator();
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, fieldBuilder);
            for (int i = 0; i < methodInfo.GetParameters().Length; i++)
            {
                gen.Emit(OpCodes.Ldarg_S, i + 1);
            }
            gen.Emit(OpCodes.Callvirt, methodInfo);
            gen.Emit(OpCodes.Ret);
        }

        public Type CreateInterface(ModuleBuilder builder, ICollection<Type> interfaces)
        {
            var interfaceBuilder = builder.DefineType("I" + _componentName, TypeAttributes.Interface | TypeAttributes.Public | TypeAttributes.Abstract);
            foreach (var item in interfaces)
            {
                foreach (var methodInfo in item.GetMethods())
                {
                    var parameterTypes = methodInfo.GetParameters().Select(info => info.ParameterType).ToArray();
                    interfaceBuilder.DefineMethod(string.Join("_", item.Name, methodInfo.Name),
                        MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual,
                        CallingConventions.Standard,
                        methodInfo.ReturnType, parameterTypes);
                }
            }
            return interfaceBuilder.CreateType();
        }
    }
}

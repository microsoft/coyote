// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace Microsoft.Coyote.Benchmarking
{
    internal class TestMetadata
    {
        public readonly List<ParamInfo> TestParams = new List<ParamInfo>();
        public readonly List<TestMethodInfo> TestMethods = new List<TestMethodInfo>();
        public readonly Type TestType;

        public TestMetadata(Type testType)
        {
            this.TestType = testType;
            foreach (PropertyInfo pi in testType.GetProperties())
            {
                var attr = pi.GetCustomAttribute(typeof(ParamsAttribute)) as ParamsAttribute;
                if (attr != null)
                {
                    this.TestParams.Add(new ParamInfo(pi, attr));
                }
            }

            MethodInfo setupMethod = null;

            foreach (MethodInfo mi in testType.GetMethods())
            {
                var setupattr = mi.GetCustomAttribute(typeof(IterationSetupAttribute)) as IterationSetupAttribute;
                if (setupattr != null)
                {
                    setupMethod = mi;
                }
            }

            foreach (MethodInfo mi in testType.GetMethods())
            {
                var attr = mi.GetCustomAttribute(typeof(BenchmarkAttribute)) as BenchmarkAttribute;
                if (attr != null)
                {
                    this.TestMethods.Add(new TestMethodInfo(setupMethod, mi));
                }
            }
        }

        public object InstantiateTest()
        {
            var ctors = this.TestType.GetConstructors();
            var target = ctors[0].Invoke(Array.Empty<object>());
            return target;
        }

        public IEnumerable<List<ParamInfo>> EnumerateParamCombinations(int pos, Stack<ParamInfo> combinations)
        {
            if (this.TestParams.Count == 0)
            {
                yield return combinations.ToList();
            }
            else
            {
                var param = this.TestParams[pos++];
                foreach (var value in param.Values)
                {
                    combinations.Push(param.WithValue(value));
                    if (pos == this.TestParams.Count)
                    {
                        yield return combinations.ToList();
                    }
                    else
                    {
                        foreach (var combo in this.EnumerateParamCombinations(pos, combinations))
                        {
                            yield return combo; // pass it up the stack.
                        }
                    }

                    combinations.Pop();
                }
            }
        }
    }

    internal class TestMethodInfo
    {
        private Action TestAction;
        private Action SetupAction;
        private readonly MethodInfo SetupMethod;
        private readonly MethodInfo TestMethod;

        public string Name { get; set; }

        public TestMethodInfo(MethodInfo setup, MethodInfo test)
        {
            this.Name = test.Name;
            this.SetupMethod = setup;
            this.TestMethod = test;
        }

        public string ApplyParams(object target, List<ParamInfo> testParams)
        {
            if (this.SetupMethod != null)
            {
                this.SetupAction = (Action)Delegate.CreateDelegate(typeof(Action), target, this.SetupMethod);
            }

            this.TestAction = (Action)Delegate.CreateDelegate(typeof(Action), target, this.TestMethod);

            string testName = this.Name;
            foreach (var item in testParams)
            {
                testName += string.Format(" {0}={1}", item.Name, item.Value);
                item.SetValue(target);
            }

            return testName;
        }

        public void Setup()
        {
            if (this.SetupAction != null)
            {
                this.SetupAction();
            }
        }

        public void Run()
        {
            this.TestAction();
        }
    }

    internal class ParamInfo
    {
        public string Name;
        public Type ParamType;
        public object[] Values;
        public PropertyInfo Property;
        public object Value;

        public ParamInfo(PropertyInfo pi, ParamsAttribute attr)
        {
            this.Values = attr.Values;
            this.ParamType = pi.PropertyType;
            this.Name = pi.Name;
            this.Property = pi;
        }

        public ParamInfo()
        {
        }

        public void SetValue(object target)
        {
            if (this.Property != null)
            {
                this.Property.SetValue(target, this.Value);
            }
        }

        public ParamInfo WithValue(object value)
        {
            return new ParamInfo()
            {
                Name = this.Name,
                ParamType = this.ParamType,
                Values = this.Values,
                Property = this.Property,
                Value = value
            };
        }
    }
}

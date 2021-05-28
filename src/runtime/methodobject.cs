using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Python.Runtime
{
    using MaybeMethodInfo = MaybeMethodBase<MethodInfo>;

    /// <summary>
    /// Implements a Python type that represents a CLR method. Method objects
    /// support a subscript syntax [] to allow explicit overload selection.
    /// </summary>
    /// <remarks>
    /// TODO: ForbidPythonThreadsAttribute per method info
    /// </remarks>
    [Serializable]
    internal class MethodObject : ExtensionType
    {
        [NonSerialized]
        private MethodInfo[] _info = null;
        private readonly List<MaybeMethodInfo> infoList;
        internal string name;
        internal MethodBinding unbound;
        internal readonly MethodBinder binder;
        internal bool is_static = false;

        internal IntPtr doc;
        internal Type type;

        public MethodObject(Type type, string name, MethodInfo[] info, bool allow_threads = MethodBinder.DefaultAllowThreads)
        {
            this.type = type;
            this.name = name;
            this.infoList = new List<MaybeMethodInfo>();
            binder = new MethodBinder();
            foreach (MethodInfo item in info)
            {
                this.infoList.Add(item);
                binder.AddMethod(item);
                if (item.IsStatic)
                {
                    this.is_static = true;
                }
            }
            binder.allow_threads = allow_threads;
        }

        internal MethodInfo[] info
        {
            get
            {
                if (_info == null)
                {
                    _info = (from i in infoList where i.Valid select i.Value).ToArray();
                }
                return _info;
            }
        }

        public virtual IntPtr Invoke(IntPtr inst, IntPtr args, IntPtr kw)
        {
            return Invoke(inst, args, kw, null);
        }

        public virtual IntPtr Invoke(IntPtr target, IntPtr args, IntPtr kw, MethodBase info)
        {
            return binder.Invoke(target, args, kw, info, this.info);
        }

        /// <summary>
        /// Helper to get docstrings from reflected method / param info.
        /// </summary>
        internal IntPtr GetDocString()
        {
            if (doc != IntPtr.Zero)
            {
                return doc;
            }
            MethodBase[] methods = binder.GetMethods();
            string str = dochelper.GetDocString(methods);
            doc = Runtime.PyString_FromString(str);
            return doc;
        }

        /// <summary>
        /// This is a little tricky: a class can actually have a static method
        /// and instance methods all with the same name. That makes it tough
        /// to support calling a method 'unbound' (passing the instance as the
        /// first argument), because in this case we can't know whether to call
        /// the instance method unbound or call the static method.
        /// </summary>
        /// <remarks>
        /// The rule we is that if there are both instance and static methods
        /// with the same name, then we always call the static method. So this
        /// method returns true if any of the methods that are represented by
        /// the descriptor are static methods (called by MethodBinding).
        /// </remarks>
        internal bool IsStatic()
        {
            return is_static;
        }

        /// <summary>
        /// Descriptor __getattribute__ implementation.
        /// </summary>
        public static IntPtr tp_getattro(IntPtr ob, IntPtr key)
        {
            var self = (MethodObject)GetManagedObject(ob);

            if (!Runtime.PyString_Check(key))
            {
                return Exceptions.RaiseTypeError("string expected");
            }

            if (Runtime.PyUnicode_Compare(key, PyIdentifier.__doc__) == 0)
            {
                IntPtr doc = self.GetDocString();
                Runtime.XIncref(doc);
                return doc;
            }

            return Runtime.PyObject_GenericGetAttr(ob, key);
        }

        /// <summary>
        /// Descriptor __get__ implementation. Accessing a CLR method returns
        /// a "bound" method similar to a Python bound method.
        /// </summary>
        public static IntPtr tp_descr_get(IntPtr ds, IntPtr ob, IntPtr tp)
        {
            var self = (MethodObject)GetManagedObject(ds);
            MethodBinding binding;

            // If the method is accessed through its type (rather than via
            // an instance) we return an 'unbound' MethodBinding that will
            // cached for future accesses through the type.

            if (ob == IntPtr.Zero)
            {
                if (self.unbound == null)
                {
                    self.unbound = new MethodBinding(self, IntPtr.Zero, tp);
                }
                binding = self.unbound;
                Runtime.XIncref(binding.pyHandle);
                ;
                return binding.pyHandle;
            }

            if (Runtime.PyObject_IsInstance(ob, tp) < 1)
            {
                return Exceptions.RaiseTypeError("invalid argument");
            }

            // If the object this descriptor is being called with is a subclass of the type
            // this descriptor was defined on then it will be because the base class method
            // is being called via super(Derived, self).method(...).
            // In which case create a MethodBinding bound to the base class.
            var obj = GetManagedObject(ob) as CLRObject;
            if (obj != null
                && obj.inst.GetType() != self.type
                && obj.inst is IPythonDerivedType
                && self.type.IsInstanceOfType(obj.inst))
            {
                ClassBase basecls = ClassManager.GetClass(self.type);
                binding = new MethodBinding(self, ob, basecls.pyHandle);
                return binding.pyHandle;
            }

            binding = new MethodBinding(self, ob, tp);
            return binding.pyHandle;
        }

        /// <summary>
        /// Descriptor __repr__ implementation.
        /// </summary>
        public static IntPtr tp_repr(IntPtr ob)
        {
            var self = (MethodObject)GetManagedObject(ob);
            return Runtime.PyString_FromString($"<method '{self.name}'>");
        }

        protected override void Clear()
        {
            Runtime.Py_CLEAR(ref this.doc);
            if (this.unbound != null)
            {
                Runtime.XDecref(this.unbound.pyHandle);
                this.unbound = null;
            }

            ClearObjectDict(this.pyHandle);
            base.Clear();
        }

        protected override void OnSave(InterDomainContext context)
        {
            base.OnSave(context);
            if (unbound != null)
            {
                Runtime.XIncref(unbound.pyHandle);
            }
            Runtime.XIncref(doc);
        }
    }
}

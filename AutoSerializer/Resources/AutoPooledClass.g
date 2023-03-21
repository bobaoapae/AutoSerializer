using System;
using System.IO;
using Newtonsoft.Json.Linq;
using AutoSerializer.Definitions;
using Microsoft.Extensions.ObjectPool;

namespace {0}
{{
    public partial class {1}{2}
    {{

        private static ObjectPool<{1}> _objectPool = new DefaultObjectPool<{1}>(new PooledPolicy<{1}>());

        public {4} static {1} Create()
        {{
            var obj = _objectPool.Get();
            obj.Initialize();
            return obj;
        }}

        private bool _disposedValue;

        public {3} void Initialize()
        {{
{5}
        }}

#region Dispose

        public {3} void CleanObject() 
        {{
{6}
        }}

        protected {3} void Dispose(bool disposing)
        {{
            if (!_disposedValue)
            {{
                if (disposing)
                {{
                    CleanObject();
                    _objectPool.Return(this);
                }}

                _disposedValue = true;
            }}
        }}

        {7}

#endregion
    }}
}}

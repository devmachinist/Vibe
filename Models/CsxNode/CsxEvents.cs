using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Vibe
{
    public partial class CsxNode
    {
        public void on(string eventName, object listener)
        {
            if (!_handlers.ContainsKey(eventName))
                _handlers[eventName] = new List<object>();
            _handlers[eventName].Add(listener);
            document.AddSessionEvent(Xid, eventName);
        }
        public virtual void emit(string eventName, params object[] args)
        {
            if (_handlers.TryGetValue(eventName, out var hds))
            {
                foreach (var handler in hds)
                {
                    switch(handler){
                        case Action handle:
                            handle();
                            break;
                        case Action<object[]> handle:
                            handle(args);
                            break;
                        case Action<ICsxNode, object[]> handle:
                            handle(this, args);
                            break;
                    }
                }
            }
        }

        public void once(string eventName, Action<object[]> listener)
        {
            Action<object[]> wrapper = null;
            wrapper = args =>
            {
                listener(args);
                off(eventName);
            };
            on(eventName, wrapper);
        }

        public void off(string eventName)
        {
            if (_handlers.ContainsKey(eventName))
                _handlers.Remove(eventName);
        }
    }
}

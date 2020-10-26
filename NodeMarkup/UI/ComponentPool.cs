using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI
{
    public interface IReusable
    {
        void DeInit();
    }
    public static class ComponentPool
    {
        private static Dictionary<Type, Queue<UIComponent>> Pool { get; } = new Dictionary<Type, Queue<UIComponent>>();

        public static Component Get<Component>(UIComponent parent)
            where Component : UIComponent, IReusable
        {
            Component component;

            var queue = GetQueue<Component>();
            if (queue.Any())
            {
                component = queue.Dequeue() as Component;
                parent.AttachUIComponent(component.gameObject);
            }
            else
                component = parent.AddUIComponent<Component>();

            return component;
        }

        public static void Free<Component>(Component component)
            where Component : UIComponent
        {
            if (component is IReusable reusable)
            {
                if (component.parent != null)
                    component.parent.RemoveUIComponent(component);

                reusable.DeInit();

                var queue = GetQueue(component.GetType());
                queue.Enqueue(component);
            }
            else
                Delete(component);
        }

        private static Queue<UIComponent> GetQueue<Component>() where Component : UIComponent => GetQueue(typeof(Component));
        private static Queue<UIComponent> GetQueue(Type type)
        {
            if (!Pool.TryGetValue(type, out Queue<UIComponent> queue))
            {
                queue = new Queue<UIComponent>();
                Pool[type] = queue;
            }
            return queue;
        }

        public static void Clear()
        {
            Logger.LogDebug($"{nameof(ComponentPool)}.{nameof(Clear)}");

            foreach (var type in Pool.Values)
            {
                while (type.Any())
                    Delete(type.Dequeue());
            }

            Pool.Clear();
        }
        private static void Delete(UIComponent component)
        {
            if (component != null)
            {
                if (component.parent != null)
                    component.parent.RemoveUIComponent(component);
                UnityEngine.Object.Destroy(component);
            }
        }
    }
}

namespace Hypernex.Tools
{
    public static class QuickInvoke
    {
        public static void InvokeActionOnMainThread(object action, params object[] args) => UnityMainThreadDispatcher
            .Instance().Enqueue(() => action.GetType().GetMethod("Invoke").Invoke(action, args));
    }
}

using System;
using System.Collections.Generic;
using Hypernex.Game;
using Hypernex.Game.Bindings;
using Nexbox;
using UnityEngine;

namespace Hypernex.Sandboxing.SandboxedTypes.Handlers
{
    public class Bindings
    {
        private IPlayer player;
        
        public Bindings() { throw new Exception("Cannot instantiate Bindings!"); }

        internal Bindings(IPlayer player) => this.player = player;
        
        public string[] GetAllPresentBindings()
        {
            // Don't return any bindings if we are on LocalAvatar and the Avatar Owner is not us
            if (player != null && !player.IsLocal)
                return Array.Empty<string>();
            List<string> binding = new List<string>();
            foreach (IBinding instanceBinding in LocalPlayer.Instance.Bindings)
                binding.Add(instanceBinding.Id);
            return binding.ToArray();
        }

        private IBinding GetBinding(string bindingId)
        {
            // Don't return any binding if we are on LocalAvatar and the Avatar Owner is not us
            if (player != null && !player.IsLocal)
                return null;
            foreach (IBinding instanceBinding in LocalPlayer.Instance.Bindings)
                if (instanceBinding.Id == bindingId)
                    return instanceBinding;
            return null;
        }

        public float GetUp(string binding) => GetBinding(binding)?.Up ?? 0.00f;
        public float GetDown(string binding) => GetBinding(binding)?.Down ?? 0.00f;
        public float GetLeft(string binding) => GetBinding(binding)?.Left ?? 0.00f;
        public float GetRight(string binding) => GetBinding(binding)?.Right ?? 0.00f;
        public bool GetButton(string binding) => GetBinding(binding)?.Button ?? false;
        public bool GetButton2(string binding) => GetBinding(binding)?.Button2 ?? false;
        public float GetTrigger(string binding) => GetBinding(binding)?.Trigger ?? 0.00f;
        public bool GetGrab(string binding) => GetBinding(binding)?.Grab ?? false;

        private List<(IBinding, Action)> RegisteredButtonClicks = new();
        private List<(IBinding, Action)> RegisteredButton2Clicks = new();
        private List<(IBinding, Action)> RegisteredTriggerClicks = new();
        private List<(IBinding, KeyCode, Action)> RegisteredKeyboardClicks = new();
        private List<(IBinding, int, Action)> RegisteredMouseClicks = new();

        public void RegisterButtonClick(string binding, object o)
        {
            IBinding b = GetBinding(binding);
            if (b != null)
            {
                SandboxFunc f = SandboxFuncTools.TryConvert(o);
                Action buttonClick = () => SandboxFuncTools.InvokeSandboxFunc(f, b.Button);
                RegisteredButtonClicks.Add((b, buttonClick));
                b.ButtonClick += buttonClick;
            }
        }
        public void RegisterButton2Click(string binding, object o)
        {
            IBinding b = GetBinding(binding);
            if (b != null)
            {
                SandboxFunc f = SandboxFuncTools.TryConvert(o);
                Action button2Click = () => SandboxFuncTools.InvokeSandboxFunc(f, b.Button2);
                RegisteredButton2Clicks.Add((b, button2Click));
                b.Button2Click += button2Click;
            }
        }
        public void RegisterTriggerClick(string binding, object o)
        {
            IBinding b = GetBinding(binding);
            if (b != null)
            {
                SandboxFunc f = SandboxFuncTools.TryConvert(o);
                Action triggerClick = () => SandboxFuncTools.InvokeSandboxFunc(f, b.Trigger);
                RegisteredTriggerClicks.Add((b, triggerClick));
                b.TriggerClick += triggerClick;
            }
        }

        public void RegisterKeyboardClick(string keyCode, object o)
        {
            IBinding b = GetBinding("Keyboard");
            if (b != null)
            {
                KeyCode k = (KeyCode) Enum.Parse(typeof(KeyCode), keyCode);
                SandboxFunc f = SandboxFuncTools.TryConvert(o);
                Action keyboardClick = () => SandboxFuncTools.InvokeSandboxFunc(f);
                RegisteredKeyboardClicks.Add((b, k, keyboardClick));
                ((Keyboard) b).RegisterCustomKeyDownEvent(k, keyboardClick);
            }
        }

        public void RegisterMouseClick(int mouseId, object o)
        {
            IBinding b = GetBinding("Mouse");
            if (b != null)
            {
                SandboxFunc f = SandboxFuncTools.TryConvert(o);
                Action mouseClick = () => SandboxFuncTools.InvokeSandboxFunc(f);
                RegisteredMouseClicks.Add((b, mouseId, mouseClick));
                ((Mouse) b).RegisterCustomMouseButtonDownEvent(mouseId, mouseClick);
            }
        }

        internal void Dispose()
        {
            RegisteredButtonClicks.ForEach(x => x.Item1.ButtonClick -= x.Item2);
            RegisteredButtonClicks.Clear();
            RegisteredButton2Clicks.ForEach(x => x.Item1.Button2Click -= x.Item2);
            RegisteredButton2Clicks.Clear();
            RegisteredTriggerClicks.ForEach(x => x.Item1.TriggerClick -= x.Item2);
            RegisteredTriggerClicks.Clear();
            RegisteredKeyboardClicks.ForEach(x => ((Keyboard) x.Item1).RemoveCustomKeyDownEvent(x.Item2, x.Item3));
            RegisteredKeyboardClicks.Clear();
            RegisteredMouseClicks.ForEach(x => ((Mouse) x.Item1).RemoveCustomKeyDownEvent(x.Item2, x.Item3));
            RegisteredMouseClicks.Clear();
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace Massive.Examples.NetAdvanced {

    public enum Bind {
        Forward,
        Backward,
        Left,
        Right,
        Jump,
        Interact,
        Attack
    }

    public static class KeyBind {

        private static readonly Dictionary<Bind, KeyCode> KeyBinds = new Dictionary<Bind, KeyCode> {
            {Bind.Forward, KeyCode.W},
            {Bind.Backward, KeyCode.S},
            {Bind.Left, KeyCode.A},
            {Bind.Right, KeyCode.D},
            {Bind.Jump, KeyCode.Space},
            {Bind.Interact, KeyCode.E},
            {Bind.Attack, KeyCode.Mouse0}
        };

        public static KeyCode Code(Bind bind) {
            return KeyBinds[bind];
        }

        public static void Assign(KeyCode code, Bind bind) {
            InputHandler.Instance.SwapKeyCodes(Code(bind), code);
            KeyBinds[bind] = code;
        }

        public static bool CodeInUse(KeyCode code) {
            return KeyBinds.ContainsValue(code);
        }

    }

}
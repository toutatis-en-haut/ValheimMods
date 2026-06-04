using System.Linq;
using ExtendedStorage.Storage;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace ExtendedStorage.State
{
    internal static class CabinetDebugCommands
    {
        public static void Register()
        {
            CommandManager.Instance.AddConsoleCommand(new DumpCommand());
            CommandManager.Instance.AddConsoleCommand(new GiveCommand());
            CommandManager.Instance.AddConsoleCommand(new SetLabelCommand());
        }

        internal static CabinetContainer FindNearestCabinet(out string err)
        {
            err = null;
            var player = Player.m_localPlayer;
            if (player == null)
            {
                err = "No local player.";
                return null;
            }

            var origin = player.transform.position;
            CabinetContainer best = null;
            float bestDist = float.MaxValue;

            foreach (var cab in Object.FindObjectsOfType<CabinetContainer>())
            {
                if (!cab.IsReady) continue;
                var d = (cab.transform.position - origin).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = cab;
                }
            }

            if (best == null) err = "No cabinet found in the loaded zone.";
            return best;
        }
    }

    internal class DumpCommand : ConsoleCommand
    {
        public override string Name => "cabinet_dump";
        public override string Help => "Log the nearest Extended Storage cabinet's tabs.";
        public override bool IsCheat => true;

        public override void Run(string[] args)
        {
            var cab = CabinetDebugCommands.FindNearestCabinet(out var err);
            if (cab == null) { Console.instance.Print(err); return; }

            Console.instance.Print(
                $"[ExtendedStorage] Cabinet at {cab.transform.position} (distance {Vector3.Distance(cab.transform.position, Player.m_localPlayer.transform.position):F1}m)");
            for (int i = 0; i < CabinetStorage.TabCount; i++)
            {
                var inv = cab.GetTab(i);
                int count = inv?.GetAllItems().Count ?? 0;
                int totalStack = inv?.GetAllItems().Sum(it => it.m_stack) ?? 0;
                Console.instance.Print(
                    $"  tab {i} '{cab.Storage.EffectiveLabel(i)}' — {count} stack(s) / {totalStack} item(s)");
                if (inv == null) continue;
                foreach (var it in inv.GetAllItems())
                {
                    Console.instance.Print(
                        $"    - {it.m_dropPrefab?.name ?? "?"} x{it.m_stack}");
                }
            }
        }
    }

    internal class GiveCommand : ConsoleCommand
    {
        public override string Name => "cabinet_give";
        public override string Help => "cabinet_give <tab 0-5> <prefab> [amount=1] — push items into a cabinet tab.";
        public override bool IsCheat => true;

        public override void Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.instance.Print("usage: cabinet_give <tab 0-5> <prefab> [amount=1]");
                return;
            }
            if (!int.TryParse(args[0], out var tabIndex) || tabIndex < 0 || tabIndex >= CabinetStorage.TabCount)
            {
                Console.instance.Print("tab must be 0..5");
                return;
            }
            var prefabName = args[1];
            int amount = 1;
            if (args.Length >= 3 && int.TryParse(args[2], out var n)) amount = n;

            var cab = CabinetDebugCommands.FindNearestCabinet(out var err);
            if (cab == null) { Console.instance.Print(err); return; }
            var inv = cab.GetTab(tabIndex);
            if (inv == null) { Console.instance.Print("tab not ready"); return; }

            var added = inv.AddItem(prefabName, amount, 1);
            Console.instance.Print(added != null
                ? $"Added {amount}x {prefabName} to tab {tabIndex}."
                : $"Could not add {prefabName} (full or unknown prefab).");
        }
    }

    internal class SetLabelCommand : ConsoleCommand
    {
        public override string Name => "cabinet_set_label";
        public override string Help => "cabinet_set_label <tab 0-5> <label> — set a cabinet tab label.";
        public override bool IsCheat => true;

        public override void Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.instance.Print("usage: cabinet_set_label <tab 0-5> <label>");
                return;
            }
            if (!int.TryParse(args[0], out var tabIndex) || tabIndex < 0 || tabIndex >= CabinetStorage.TabCount)
            {
                Console.instance.Print("tab must be 0..5");
                return;
            }
            var label = string.Join(" ", args.Skip(1));

            var cab = CabinetDebugCommands.FindNearestCabinet(out var err);
            if (cab == null) { Console.instance.Print(err); return; }
            cab.SetLabel(tabIndex, label);
            Console.instance.Print($"Tab {tabIndex} label set to '{cab.Storage.EffectiveLabel(tabIndex)}'.");
        }
    }
}

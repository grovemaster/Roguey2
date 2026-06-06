using System.Collections.Generic;
using System.Text;
using JRogue.Actors;
using JRogue.Stats;
using UnityEngine;

namespace JRogue.Dialog
{
    public static class DialogParameterResolver
    {
        static readonly Dictionary<string, System.Func<DialogContext, string>> BuiltIn = new()
        {
            ["npcName"] = ctx => ctx.Npc != null ? ctx.Npc.DisplayName : string.Empty,
            ["partyName"] = ctx => ctx.Speaker != null ? ctx.Speaker.DisplayName : string.Empty,
            ["speakerName"] = ctx => ctx.Speaker != null ? ctx.Speaker.DisplayName : string.Empty,
            ["partySize"] = ctx =>
            {
                var party = JRogue.Manager.Party.PartyManager.Instance;
                return party != null ? party.partyMembers.Count.ToString() : "0";
            },
        };

        public static string Resolve(string template, DialogContext context)
        {
            if (string.IsNullOrEmpty(template) || context == null)
                return template ?? string.Empty;

            var sb = new StringBuilder(template);
            foreach (KeyValuePair<string, System.Func<DialogContext, string>> pair in BuiltIn)
            {
                string token = "{" + pair.Key + "}";
                sb.Replace(token, pair.Value(context) ?? string.Empty);
            }

            return sb.ToString();
        }

        public static void RegisterToken(string tokenName, System.Func<DialogContext, string> resolver)
        {
            if (string.IsNullOrWhiteSpace(tokenName) || resolver == null)
                return;

            BuiltIn[tokenName.Trim()] = resolver;
        }
    }
}

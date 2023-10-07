using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.SlashCommands;

using System.Text;
using System.Text.RegularExpressions;

namespace CeresDSP.CommandModules
{
    internal class TonalIndicatorCommands : ApplicationCommandModule
    {
        private Dictionary<string, string> Indicators { get; init; }

        public TonalIndicatorCommands()
        {
            Indicators = new()
            {
                { "a", "alterous" },
                { "ay", "at you" },
                { "c", "copypasta" },
                { "cb", "clickbait" },
                { "ex", "exaggeration" },
                { "f", "fake" },
                { "fi", "figuratively" },
                { "g", "genuine" },
                { "gen", "genuine" },
                { "gq", "genuine question" },
                { "genq", "genuine question" },
                { "hj", "half joking" },
                { "hyp", "hyperbole" },
                { "ij", "inside joke" },
                { "j", "joking" },
                { "l", "lyrics" },
                { "ly", "lyrics" },
                { "lh", "light hearted" },
                { "li", "literally" },
                { "lu", "a little upset" },
                { "m", "metaphor" },
                { "nay", "not at you" },
                { "nbh", "nobody here (the entity (person/group/etc.) that is talked about is not here)" },
                { "nbr", "not being rude" },
                { "nc", "negative connotation" },
                { "neg", "negative connotation" },
                { "neu", "neutral connotation" },
                { "nf", "not forced (e.g. for when making a suggestion)" },
                { "nm", "not mad" },
                { "nsb", "not subtweeting" },
                { "nsrs", "not serious" },
                { "ns", "non-sexual intent" },
                { "nsx", "non-sexual intent" },
                { "ot", "off topic" },
                { "p", "platonic" },
                { "pc", "positive connotation" },
                { "pos", "positive connotation" },
                { "q", "quote" },
                { "r", "romantic" },
                { "ref", "reference" },
                { "rt", "rhetorical question" },
                { "rh", "rhetorical question" },
                { "s", "sarcasm" },
                { "srs", "serious" },
                { "sx", "sexual intent" },
                { "x", "sexual intent" },
                { "t", "teasing" },
                { "th", "threat" }
            };
        }

        [ContextMenu(DSharpPlus.ApplicationCommandType.MessageContextMenu, "Explain tonal indicators")]
        [Description("Sends a message only you can see in the chat with an explanation for each found tonal indicator in that message.")]
        internal async Task ExplainMessage(ContextMenuContext ctx)
        {
            string msgContent = ctx.TargetMessage.Content;
            MatchCollection indicatorMatches = Regex.Matches(msgContent, @"\s+\/([a-zA-Z]+)\b");
            if (indicatorMatches.Count == 0)
            {
                await ctx.CreateResponseAsync("Couldn't find tonal indicators in that message.", true);
                return;
            }

            StringBuilder explanations = new();
            List<string> unknownIndicators = new(indicatorMatches.Count);
            for (int i = 0; i < indicatorMatches.Count; i++)
            {
                string indicator = indicatorMatches[i].Groups[1].Value;
                string explanation;
                try
                {
                    explanation = Indicators[indicator];
                }
                catch (KeyNotFoundException)
                {
                    unknownIndicators.Add($"/{indicator}");
                    continue;
                }
                explanations.Append($"/{indicator} = {explanation}\n");
            }

            if (unknownIndicators.Count > 0)
                explanations.Append($"\n*Couldn't find explanation(s) for following indicator(s): {string.Join(", ", unknownIndicators)}*");

            await ctx.CreateResponseAsync(explanations.ToString(), true);
        }
    }
}

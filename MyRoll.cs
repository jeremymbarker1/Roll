using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Xml;

namespace Roll
{
    public class MyRoll
    {
        // dice
        private int dice_count;
        private int dice_sides;

        // options
        private int keep_highest;
        private int keep_lowest;
        private int reroll;
        private int limit;

        // flags
        private bool isAdvantage;
        private bool isDisadvantage;
        private bool isLast;
        private bool isHide;
        private bool isTotal;

        // const string values
        private const string dice_arg_str = "d";
        private const string keep_hightest_option_str = "kh";
        private const string keep_lowest_option_str = "kl";
        private const string reroll_option_str = "rr";
        private const string limit_option_str = "lim";
        private const string advantage_flag = "--advantage";
        private const string advantage_flag_abbr = "-a";
        private const string disadvantage_flag = "--disadvantage";
        private const string disadvantage_flag_abbr = "-d";
        private const string hide_flag = "--hide";
        private const string hide_flag_abbr = "-h";
        private const string last_flag = "--last";
        private const string last_flag_abbr = "-l";
        private const string total_flag = "--total";
        private const string total_flag_abbr = "-t";

        private const string help = @"
HELP
$ roll.exe ""[dice]"" <flags>
   Dice: [n] d[m](options)
      [n]: The quanity of dice to roll
      [m]: The sidedness of the dice to roll
      Options:
         kh(n)    keeps the highest(n) number of results(default n = 1)
         kl(n)    keeps the lowest(n) number of results(default n = 1)
         rr[n]    reroll when the result is less than or equal to [n]
         lim[n]   limit number of reroll attempts by[n]
      Flags:
         -a, --advantage      make a roll with advantage(same as kh1)
         -d, --disadvantage   make a roll with disadvantage(same as kl1)
         -l, --last           only show the last reroll result
         -h, --hide           only show the kept results
         -t, --total          also show the sum of the rolls
      Examples:
         roll.exe ""4 d6 kh3""
         roll.exe ""2d6 rr2 lim1"" -last";

        public void Run(string[] args)
        {
            // initialize
            keep_highest = 0;
            keep_lowest = 0;
            reroll = 0;
            limit = 0;
            isAdvantage = false;
            isDisadvantage = false;
            isLast = false;
            isHide = false;
            isTotal = false;

            string dice_string = string.Empty;
            List<string> flags = new();
            foreach (string arg in args)
            {
                if (arg.StartsWith('-'))
                {
                    flags.Add(arg.ToLower());
                }
                else
                {
                    dice_string += arg;
                }
            }

            bool isValid = ParseCommand(dice_string);
            if (!isValid)
            {
                Console.WriteLine(help);
                return;
            }

            ParseFlags(flags);            
            ParseDebug();

            List<List<int>> roll_list = GetRolls();
            PrintRolls(roll_list);
        }

        private bool ParseCommand(string dice_string)
        {
            // remove whitespace
            dice_string = dice_string.Replace(" ", "");

            Dictionary<string, int> indicies = new()
            {
                // string breakdown
                { dice_arg_str, dice_string.IndexOf(dice_arg_str) },
                { keep_hightest_option_str, dice_string.IndexOf(keep_hightest_option_str) },
                { keep_lowest_option_str, dice_string.IndexOf(keep_lowest_option_str) },
                { reroll_option_str, dice_string.IndexOf(reroll_option_str) },
                { limit_option_str, dice_string.IndexOf(limit_option_str) }
            };

            // detect invalid options
            //    [1] must have a dice quanity and sidedness
            //    [2] cannot use both keep highest and keep lowest
            //    [3] limit cannot be used without reroll
            if (indicies[dice_arg_str] == -1
                || (indicies[keep_hightest_option_str] != -1 && indicies[keep_lowest_option_str] != -1)
                || (indicies[reroll_option_str] == -1 && indicies[limit_option_str] != -1))
            {
                return false;
            }

            // get values
            if (!(TryParseRoll(dice_string, indicies, 0, out dice_count)
                  && TryParseRoll(dice_string, indicies, dice_arg_str, out dice_sides)
                  && (indicies[keep_hightest_option_str] == -1 || TryParseRoll(dice_string, indicies, keep_hightest_option_str, out keep_highest))
                  && (indicies[keep_lowest_option_str] == -1 || TryParseRoll(dice_string, indicies, keep_lowest_option_str, out keep_lowest))
                  && (indicies[reroll_option_str] == -1 || TryParseRoll(dice_string, indicies, reroll_option_str, out reroll))
                  && (indicies[limit_option_str] == -1 || TryParseRoll(dice_string, indicies, limit_option_str, out limit))))
            {
                return false;
            }

            // detect invalid values
            // [1,2] cannot keep more dice than is rolled
            // [3] cannot reroll for results greater than sides
            if (keep_highest > dice_count
                || keep_lowest > dice_count
                || reroll > dice_sides)
            {
                return false;
            }
            return true;
        }

        private static bool TryParseRoll(string dice_string, Dictionary<string, int> indicies, int start_index, out int result)
        {
            int length = GetNextIndex(start_index, dice_string.Length, indicies) - start_index;
            return int.TryParse(dice_string.AsSpan(start_index, length), out result);
        }

        private static bool TryParseRoll(string dice_string, Dictionary<string, int> indicies, string key, out int result)
        {
            int start_index = indicies[key] + key.Length;
            int length = GetNextIndex(start_index, dice_string.Length, indicies) - start_index;
            return int.TryParse(dice_string.AsSpan(start_index, length), out result);
        }

        private static int GetNextIndex(int startIndex, int fallback, Dictionary<string, int> indicies)
        {
            int rtn = fallback;
            foreach (int index in indicies.Values)
            {
                if (index > startIndex && index < rtn)
                {
                    rtn = index;
                }
            }
            return rtn;
        }

        private void ParseFlags(List<string> flags)
        {
            isAdvantage = flags.Contains(advantage_flag) || flags.Contains(advantage_flag_abbr);
            isDisadvantage = flags.Contains(disadvantage_flag) || flags.Contains(disadvantage_flag_abbr);
            isLast = flags.Contains(last_flag) || flags.Contains(last_flag_abbr);
            isHide = flags.Contains(hide_flag) || flags.Contains(hide_flag_abbr);
            isTotal = flags.Contains(total_flag) || flags.Contains(total_flag_abbr);
        }

        private List<List<int>> GetRolls()
        {
            Random random = new();

            // track rolls and rerolls
            List<List<int>> roll_list = new();

            // roll the specified number of dice
            for (int i = 0; i < dice_count; i++)
            {
                int roll = random.Next(dice_sides) + 1;
                List<int> rolls = new() { roll };
                // attempt rerolls
                for (int j = 0; rolls[^1] <= reroll && j < limit; j++)
                {
                    int reroll = random.Next(dice_sides) + 1;
                    rolls.Add(reroll);
                }
                roll_list.Add(rolls);
            }

            return roll_list;
        }

        private void ParseDebug()
        {
            Console.WriteLine(string.Format("Dice Count: {0}", dice_count));
            Console.WriteLine(string.Format("Dice Sides: {0}", dice_sides));
            Console.WriteLine(string.Format("Keep Highest: {0}", keep_highest));
            Console.WriteLine(string.Format("Keep Lowest: {0}", keep_lowest));
            Console.WriteLine(string.Format("Reroll: {0}", reroll));
            Console.WriteLine(string.Format("Limit: {0}", limit));
            Console.WriteLine(string.Format("Advantage: {0}", isAdvantage ? "True" : "False"));
            Console.WriteLine(string.Format("Disadvantage: {0}", isDisadvantage ? "True" : "False"));
            Console.WriteLine(string.Format("Last: {0}", isLast ? "True" : "False"));
            Console.WriteLine(string.Format("Hide: {0}", isHide ? "True" : "False"));
            Console.WriteLine(string.Format("Total: {0}", isTotal ? "True" : "False"));
        }

        private void PrintRolls(List<List<int>> roll_list)
        {
        }
    }
}
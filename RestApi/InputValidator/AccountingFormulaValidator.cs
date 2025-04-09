using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HEAppE.RestApi.InputValidator;

/// <summary>
///     Class for Accounting formula validation
/// </summary>
public partial class AccountingFormulaValidator
{
    // Regular expression to match valid elements: strings, numbers, and math operators
    [GeneratedRegex("^[a-zA-Z0-9\\s\\+\\-\\*\\/\\%\\(\\)\\.]*$")]
    private static partial Regex AccountingFormulaRegex();

    public static bool IsValidAccountingFormula(string formula)
    {
        if (string.IsNullOrWhiteSpace(formula)) return false;

        // Check if the formula contains only valid characters
        if (!AccountingFormulaRegex().IsMatch(formula)) return false;

        // Check if parentheses are balanced
        return AreParenthesesBalanced(formula);
    }

    private static bool AreParenthesesBalanced(string formula)
    {
        Stack<char> stack = new();

        foreach (var c in formula)
            if (c == '(')
                stack.Push(c);
            else if (c == ')')
                if (stack.Count == 0 || stack.Pop() != '(')
                    return false;

        // If the stack is not empty, there are unmatched parentheses
        return stack.Count == 0;
    }
}
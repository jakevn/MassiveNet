// MIT License (MIT) - Copyright (c) 2014 jakevn - Please see included LICENSE file
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Massive.Examples.NetAdvanced {

    [Flags]
    public enum Fmt {
        Letter = 1,
        Number = 2,
        /// <summary> Period, exclamation, question, quote, etc. </summary>
        Punctuation = 4,
        /// <summary> Allow more than one whitespace character (space/tab) in a row. </summary>
        RepeatWhitespace = 8,
        Space = 16,
        Tab = 32,
        NewLine = 64,
    }

    public class InputValidator {

        private const char Tab = '\t';
        private const char Space = ' ';
        private const char NewLine = '\n';

        public const int MinPasswordLength = 8;
        public const int MaxPasswordLength = 64;

        public const int MinPlayerNameLength = 3;
        public const int MaxPlayerNameLength = 16;

        public const int MaxChatLength = 128;

        public static bool ChatInputValid(char[] input) {
            if (input.Length > MaxChatLength || input.Length == 0 || input[0] == Space) return false;
            bool spacePrevious = false;
            for (int i = 0; i < input.Length; i++) {
                if (input[i] == ' ') {
                    if (spacePrevious) return false;
                    spacePrevious = true;
                    continue;
                }
                spacePrevious = false;
            }
            return true;
        }

        public static bool IsValid(char[] input, int maxLength, Fmt allowed) {
            if (input.Length > maxLength) return false;
            for (int i = 0; i < input.Length; i++) {
                if ((allowed & Fmt.Letter) == Fmt.Letter) {
                    if (char.IsLetter(input[i])) continue;
                }
                if ((allowed & Fmt.Number) == Fmt.Number) {
                    if (char.IsNumber(input[i])) continue;
                }
                if ((allowed & Fmt.Space) == Fmt.Space) {
                    if (input[i] == Space) {
                        if ((allowed & Fmt.RepeatWhitespace) == Fmt.RepeatWhitespace) continue;
                        if (i == 0 || !char.IsWhiteSpace(input[i - 1])) continue;
                    }
                }
                if ((allowed & Fmt.Punctuation) == Fmt.Punctuation) {
                    if (char.IsPunctuation(input[i])) continue;
                }
                if ((allowed & Fmt.Tab) == Fmt.Tab) {
                    if (input[i] == Tab) {
                        if ((allowed & Fmt.RepeatWhitespace) == Fmt.RepeatWhitespace) continue;
                        if (i == 0 || !char.IsWhiteSpace(input[i - 1])) continue;
                    }
                }
                if ((allowed & Fmt.NewLine) == Fmt.NewLine) {
                    if (input[i] == NewLine) continue;
                }
                return false;
            }
            return true;
        }

        public static bool IsValidEmail(string strIn) {

            if (String.IsNullOrEmpty(strIn)) return false;

            // Use IdnMapping class to convert Unicode domain names. 
            try {
                strIn = Regex.Replace(strIn, @"(@)(.+)$", DomainMapper, RegexOptions.None);
            }
            catch {
                return false;
            }

            // Return true if strIn is in valid e-mail format. 
            try {
                return Regex.IsMatch(strIn,
                    @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$",
                    RegexOptions.IgnoreCase);
            }
            catch {
                return false;
            }
        }

        public static bool IsValidPassword(string strIn) {
            return strIn.Length >= MinPasswordLength && strIn.Length <= MaxPasswordLength;
        }

        public static bool IsValidPlayerName(string strIn) {
            return strIn.Length >= MinPlayerNameLength && strIn.Length <= MaxPlayerNameLength && LettersOnly(strIn);
        }

        /// <summary> If the first character is a letter and is not already uppercase, turns to uppercase. </summary>
        public static void FmtUppercaseFirstChar(char[] input) {
            if (input.Length == 0 || !char.IsLetter(input[0]) || char.IsUpper(input[0])) return;
            input[0] = char.ToUpper(input[0]);
        }

        /// <summary> If the first character is a letter and is not already uppercase, turns to uppercase. </summary>
        public static string FmtUppercaseFirstChar(string input) {
            if (input.Length == 0 || !char.IsLetter(input[0]) || char.IsUpper(input[0])) return input;
            char[] charArr = input.ToCharArray();
            charArr[0] = char.ToUpper(input[0]);
            return new string(charArr);
        }

        /// <summary> Excluding the first letter, turns every letter to lowercase. </summary>
        public static void FmtLowercaseAfterFirstChar(char[] input) {
            for (int i = 1; i < input.Length; i++) {
                if (!char.IsLetter(input[i]) || char.IsLower(input[i])) continue;
                input[i] = char.ToLower(input[i]);
            }
        }

        public static void FmtAllLowercase(char[] input) {
            for (int i = 0; i < input.Length; i++) {
                if (!char.IsLetter(input[i]) || char.IsLower(input[i])) continue;
                input[i] = char.ToLower(input[i]);
            }
        }

        public static string FmtAllLowercase(string input) {
            char[] reformatted = null;
            for (int i = 0; i < input.Length; i++) {
                if (!char.IsLetter(input[i]) || char.IsLower(input[i])) continue;
                if (reformatted == null) reformatted = input.ToCharArray();
                reformatted[i] = char.ToLower(input[i]);
            }
            return reformatted == null ? input : new string(reformatted);
        }

        public static bool LowercaseOnly(char[] input) {
            for (int i = 0; i < input.Length; i++) {
                if (char.IsLetter(input[i]) && char.IsUpper(input[i])) return false;
            }
            return true;
        }

        public static bool LowercaseOnly(string input) {
            for (int i = 0; i < input.Length; i++) {
                if (char.IsLetter(input[i]) && char.IsUpper(input[i])) return false;
            }
            return true;
        }

        /// <summary> Returns true if only letters are present. </summary>
        public static bool LettersOnly(char[] input) {
            for (int i = 0; i < input.Length; i++) {
                if (!char.IsLetter(input[i])) return false;
            }
            return true;
        }

        /// <summary> Returns true if only letters are present. </summary>
        public static bool LettersOnly(string input) {
            for (int i = 0; i < input.Length; i++) {
                if (!char.IsLetter(input[i])) return false;
            }
            return true;
        }

        /// <summary> Returns true if only letters and numbers are present. </summary>
        public static bool LettersNumbersOnly(char[] input) {
            for (int i = 0; i < input.Length; i++) {
                if (!char.IsLetterOrDigit(input[i])) return false;
            }
            return true;
        }

        /// <summary> Returns true if only letters and numbers are present. </summary>
        public static bool LettersNumbersOnly(string input) {
            for (int i = 0; i < input.Length; i++) {
                if (!char.IsLetterOrDigit(input[i])) return false;
            }
            return true;
        }

        private static string DomainMapper(Match match) {
            // IdnMapping class with default property values.
            IdnMapping idn = new IdnMapping();
            string domainName = match.Groups[2].Value;
            domainName = idn.GetAscii(domainName);
            return match.Groups[1].Value + domainName;
        }
    }
}
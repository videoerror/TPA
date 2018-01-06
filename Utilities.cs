#region LICENCE
/*
Copyright 2017 - 2018 video_error

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */
#endregion

using System;
using System.Text;

using fCraft;

namespace Tpa {

	internal static class Utilities {

		public enum ColorCode : byte {
			Black = 48,
			DarkBlue = 49,
			DarkGreen = 50,
			DarkAqua = 51,
			DarkRed = 52,
			DarkPurple = 53,
			Gold = 54,
			Gray = 55,
			DarkGray = 56,
			Blue = 57,
			Green = 65,
			Aqua = 66,
			Red = 67,
			LightPurple = 68,
			Yellow = 69,
			White = 70
		}

		public static string ToString(this ColorCode colorCode,
									  bool? insertAmpersandOrPercentOrNeither) {
			string colorCodeText =
				Encoding.GetEncoding(437).GetString(new byte[] {
				((byte)colorCode)
			});

			switch(insertAmpersandOrPercentOrNeither) {
				case null:
					return colorCodeText;

				case true:
					return "&" + colorCodeText;

				case false:
					return "%" + colorCodeText;

				default:
					goto case null;
			}
		}

		private static bool IsColorCode(string colorCodeText) {
			try {
				colorCodeText = colorCodeText.Trim();

				char startColorCodeCharacter = colorCodeText[0];
				char endColorCodeCharacter = colorCodeText[1];

				if(startColorCodeCharacter == '&' ||
				   startColorCodeCharacter == '%') {
					byte startColorCode = ((byte)ColorCode.Black);
					byte endColorCode = ((byte)ColorCode.Blue);

					bool colorCodeRangeOneDone = false;

					for(byte colorCodesIndexer = startColorCode;
						colorCodesIndexer <= endColorCode;
						colorCodesIndexer++) {
						if(endColorCodeCharacter ==
						   ((ColorCode)colorCodesIndexer).ToString(((bool?)null))[0]) {
							return true;
						} else if(colorCodesIndexer == endColorCode &&
								  !colorCodeRangeOneDone) {
							startColorCode = ((byte)ColorCode.Green);
							endColorCode = ((byte)ColorCode.White);

							colorCodesIndexer = --startColorCode;

							colorCodeRangeOneDone = true;
						}
					}
				}

				return false;
			} catch {
				return false;
			}
		}

		public const char SymbolEscapeCharacter = '\x00A0';

		public static string EscapeSymbols(this string input) {
			return SymbolEscapeCharacter + input +
				   SymbolEscapeCharacter;
		}

		public static readonly string Symbols = "~`!@#$^*()_-+={[}]|\\:;\"'<,>.?/";

		public static string ColorCodeBeforeSymbolsAndAfter(string input, ColorCode beforeColorCode,
															ColorCode afterColorCode) {
			bool escaping = false;
			bool wasEscaping = false;
			bool symbolStreak = false;

			for(int inputIndexer = 0; inputIndexer < input.Length; inputIndexer++) {
				char inputCharacter = input[inputIndexer];

				if(inputCharacter == SymbolEscapeCharacter) {
					escaping = !escaping;
				}

				if(escaping) {
					symbolStreak = false;
					wasEscaping = true;
				} else if(inputIndexer < input.Length - 1 && wasEscaping) {
					if(Symbols.Contains(input[inputIndexer + 1].ToString())) {
						continue;
					}

					input = input.Insert(inputIndexer + 1, afterColorCode.ToString(true));

					inputIndexer += 2;

					wasEscaping = false;
				} else if(inputIndexer < input.Length - 2 &&
						  IsColorCode(input.Substring(inputIndexer, 2))) {
					inputIndexer += 2;

					symbolStreak = false;
				} else if(Symbols.Contains(inputCharacter.ToString()) && !symbolStreak) {
					input = input.Insert(inputIndexer, beforeColorCode.ToString(true));

					inputIndexer += 2;

					symbolStreak = true;
				} else if((((inputCharacter == ' ' ||
							 Char.IsLetter(inputCharacter)) ||
							 Char.IsNumber(inputCharacter)) && symbolStreak) ||
						  (inputIndexer > 0 && input[inputIndexer - 1] == '\n')) {
					if(inputIndexer != input.Length - 1) {
						input = input.Insert(inputIndexer, afterColorCode.ToString(true));
					}

					inputIndexer += 2;

					symbolStreak = false;
				}
			}

			input = input.Replace(SymbolEscapeCharacter.ToString(), "");

			if(!IsColorCode(input.TrimStart())) {
				input = input.Insert(0, afterColorCode.ToString(true));
			}

			return input;
		}

		public static void TpaPluginMessage(this Player player, string message) {
			player.Message(Init.StylizedNameAndVersionWithBrackets + " " + ColorCodeBeforeSymbolsAndAfter(message, ColorCode.DarkGray, ColorCode.Yellow));
		}
	}
}

﻿using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Events;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Miki.Modules.Logging.Interpreter
{
	public class LogQueryInterpreter
	{
		private static List<RegexToken> tokens = new List<RegexToken>();

		private static LogQueryInterpreter instance = new LogQueryInterpreter();

		public enum QueryTokenType
		{
			// command
			ADD,

			REMOVE,

			// types
			WELCOME,

			LEAVE,

			// Data
			STRING,

			NUMBER
		};

		public class TokenMatch
		{
			public string remainingText;
			public QueryTokenType type;
			public string value;
		}

		public class Token
		{
			public QueryTokenType type;
			public string value;
		}

		private class RegexToken
		{
			private string query = "";
			private QueryTokenType type;

			public RegexToken(string query, QueryTokenType type)
			{
				this.query = query;
				this.type = type;

				tokens.Add(this);
			}

			public TokenMatch Match(string text)
			{
				Match m = Regex.Match(text, query);
				if (m.Success)
				{
					return new TokenMatch()
					{
						remainingText = text.Substring(m.Length),
						type = this.type,
						value = m.Value
					};
				}
				return null;
			}
		}

		public LogQueryInterpreter()
		{
			new RegexToken("^(new|add)", QueryTokenType.ADD);
			new RegexToken("^(remove|delete|del)", QueryTokenType.REMOVE);
			new RegexToken("^(welcome|join)", QueryTokenType.WELCOME);
			new RegexToken("^(leave)", QueryTokenType.LEAVE);
			new RegexToken("^\".*\"", QueryTokenType.STRING);
		}

		public static void Run(IContext x)
		{
			List<Token> allTokens = instance.Tokenize(x.GetArgumentPack().ToString());
		}

		public class Executor
		{
			private List<Token> currentTokens = new List<Token>();

			private int currentIndex = 0;

			private Token Current => currentTokens[currentIndex];

			public Executor(List<Token> t)
			{
				currentTokens = t;
			}

			public void Parse()
			{
			}

			public bool Accept(QueryTokenType t)
			{
				if (Current.type == t)
				{
					Next();
					return true;
				}
				return false;
			}

			public void Next()
			{
				currentIndex++;
			}
		}

		public List<Token> Tokenize(string text)
		{
			string currentText = text;
			List<Token> allTokens = new List<Token>();

			while (!string.IsNullOrWhiteSpace(currentText))
			{
				TokenMatch m = GetMatch(currentText);
				if (m != null)
				{
					currentText = m.remainingText;

					allTokens.Add(new Token()
					{
						type = m.type,
						value = m.value
					});
				}
				else
				{
					currentText = currentText.TrimStart(' ', '\n', '\r');
				}
			}
			return allTokens;
		}

		public TokenMatch GetMatch(string text)
		{
			foreach (RegexToken t in tokens)
			{
				TokenMatch m = t.Match(text);
				if (m != null)
				{
					return m;
				}
			}
			return null;
		}
	}
}
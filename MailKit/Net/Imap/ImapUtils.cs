﻿//
// ImapUtils.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2014 Jeffrey Stedfast
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;

using MimeKit;
using MimeKit.Utils;

namespace MailKit.Net.Imap {
	/// <summary>
	/// IMAP utility functions.
	/// </summary>
	static class ImapUtils
	{
		static readonly Encoding Latin1 = Encoding.GetEncoding (28591);

		static readonly string[] Months = {
			"Jan", "Feb", "Mar", "Apr", "May", "Jun",
			"Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
		};

		/// <summary>
		/// Formats a date in a format suitable for use with the APPEND command.
		/// </summary>
		/// <returns>The formatted date string.</returns>
		/// <param name="date">The date.</param>
		public static string FormatInternalDate (DateTimeOffset date)
		{
			return string.Format ("{0:D2}-{1}-{2:D4} {3:D2}:{4:D2}:{5:D2} {6:+00;-00}{7:00}",
				date.Day, Months[date.Month - 1], date.Year, date.Hour, date.Minute, date.Second,
				date.Offset.Hours, date.Offset.Minutes);
		}

		/// <summary>
		/// Parses the internal date string.
		/// </summary>
		/// <returns>The date.</returns>
		/// <param name="text">The text to parse.</param>
		public static DateTimeOffset ParseInternalDate (string text)
		{
			return DateTimeOffset.ParseExact (text.Trim (), "d-MMM-yyyy HH:mm:ss zzz", CultureInfo.InvariantCulture.DateTimeFormat);
		}

		/// <summary>
		/// Attempts to parse an atom token as a set of UIDs.
		/// </summary>
		/// <returns><c>true</c> if the UIDs were successfully parsed, otherwise <c>false</c>.</returns>
		/// <param name="atom">The atom string.</param>
		/// <param name="uids">The UIDs.</param>
		public static bool TryParseUidSet (string atom, out string[] uids)
		{
			var ranges = atom.Split (new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			var list = new List<string> ();

			uids = null;

			for (int i = 0; i < ranges.Length; i++) {
				var minmax = ranges[i].Split (':');
				uint min;

				if (!uint.TryParse (minmax[0], out min) || min == 0)
					return false;

				if (minmax.Length == 2) {
					uint max;

					if (!uint.TryParse (minmax[1], out max) || max == 0)
						return false;

					for (uint uid = min; uid <= max; uid++)
						list.Add (uid.ToString ());
				} else if (minmax.Length == 1) {
					list.Add (minmax[0]);
				} else {
					return false;
				}
			}

			uids = list.ToArray ();

			return true;
		}

		/// <summary>
		/// Formats the array of UIDs as a string suitable for use with IMAP commands.
		/// </summary>
		/// <returns>The UID set.</returns>
		/// <param name="uids">The UIDs.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="uids"/> is <c>null</c>.
		/// </exception>
		public static string FormatUidSet (uint[] uids)
		{
			if (uids == null)
				throw new ArgumentNullException ("uids");

			var builder = new StringBuilder ();
			int index = 0;

			while (index < uids.Length) {
				uint min = uids[index];
				uint max = min;
				int i = 0;

				while (i < uids.Length) {
					if (uids[i] > max + 1)
						break;

					max = uids[i++];
				}

				if (builder.Length > 0)
					builder.Append (',');

				if (max > min)
					builder.AppendFormat ("{0}:{1}", min, max);
				else
					builder.Append (min.ToString ());

				index = i;
			}

			return builder.ToString ();
		}

		/// <summary>
		/// Formats the array of indexes as a string suitable for use with IMAP commands.
		/// </summary>
		/// <returns>The index set.</returns>
		/// <param name="indexes">The indexes.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="indexes"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// One or more of the indexes has a negative value.
		/// </exception>
		public static string FormatUidSet (int[] indexes)
		{
			if (indexes == null)
				throw new ArgumentNullException ("indexes");

			var uids = new uint[indexes.Length];

			for (int i = 0; i < indexes.Length; i++) {
				if (i < 0)
					throw new ArgumentException ("One or more indexes were negative.", "indexes");

				uids[i] = (uint) indexes[i] + 1;
			}

			return FormatUidSet (uids);
		}

		/// <summary>
		/// Formats the array of UIDs as a string suitable for use with IMAP commands.
		/// </summary>
		/// <returns>The UID set.</returns>
		/// <param name="uids">The UIDs.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="uids"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ArgumentException">
		/// One or more of the UIDs is invalid.
		/// </exception>
		public static string FormatUidSet (string[] uids)
		{
			if (uids == null)
				throw new ArgumentNullException ("uids");

			var values = new uint[uids.Length];

			for (int i = 0; i < uids.Length; i++) {
				uint uid;

				if (!uint.TryParse (uids[i], out uid) || uid == 0)
					throw new ArgumentException ("One or more uids were invalid.", "uids");

				values[i] = uid;
			}

			return FormatUidSet (values);
		}

		/// <summary>
		/// Handles an untagged LIST or LSUB response.
		/// </summary>
		/// <param name="engine">The IMAP engine.</param>
		/// <param name="ic">The IMAP command.</param>
		/// <param name="index">The index.</param>
		/// <param name="tok">The token.</param>
		public static void HandleUntaggedListResponse (ImapEngine engine, ImapCommand ic, int index, ImapToken tok)
		{
			var token = engine.ReadToken (ic.CancellationToken);
			var list = (List<ImapFolder>) ic.UserData;
			var attrs = FolderAttributes.None;
			string encodedName;
			ImapFolder folder;
			char delim;

			// parse the folder attributes list
			if (token.Type != ImapTokenType.OpenParen)
				throw ImapEngine.UnexpectedToken (token, false);

			token = engine.ReadToken (ic.CancellationToken);

			while (token.Type == ImapTokenType.Flag || token.Type == ImapTokenType.Atom) {
				string atom = (string) token.Value;

				switch (atom) {
				case "\\NoInferiors":   attrs |= FolderAttributes.NoInferiors; break;
				case "\\Noselect":      attrs |= FolderAttributes.NoSelect; break;
				case "\\Marked":        attrs |= FolderAttributes.Marked; break;
				case "\\Unmarked":      attrs |= FolderAttributes.Unmarked; break;
				case "\\NonExistent":   attrs |= FolderAttributes.NonExistent; break;
				case "\\Subscribed":    attrs |= FolderAttributes.Subscribed; break;
				case "\\Remote":        attrs |= FolderAttributes.Remote; break;
				case "\\HasChildren":   attrs |= FolderAttributes.HasChildren; break;
				case "\\HasNoChildren": attrs |= FolderAttributes.HasNoChildren; break;
				case "\\All":           attrs |= FolderAttributes.All; break;
				case "\\Archive":       attrs |= FolderAttributes.Archive; break;
				case "\\Drafts":        attrs |= FolderAttributes.Drafts; break;
				case "\\Flagged":       attrs |= FolderAttributes.Flagged; break;
				case "\\Junk":          attrs |= FolderAttributes.Junk; break;
				case "\\Sent":          attrs |= FolderAttributes.Sent; break;
				case "\\Trash":         attrs |= FolderAttributes.Trash; break;
					// XLIST flags:
				case "\\AllMail":       attrs |= FolderAttributes.All; break;
				case "\\Important":     attrs |= FolderAttributes.Flagged; break;
				case "\\Inbox":         break;
				case "\\Spam":          attrs |= FolderAttributes.Junk; break;
				case "\\Starred":       attrs |= FolderAttributes.Flagged; break;
				}

				token = engine.ReadToken (ic.CancellationToken);
			}

			if (token.Type != ImapTokenType.CloseParen)
				throw ImapEngine.UnexpectedToken (token, false);

			// parse the path delimeter
			token = engine.ReadToken (ic.CancellationToken);

			if (token.Type == ImapTokenType.QString) {
				var qstring = (string) token.Value;

				delim = qstring[0];
			} else if (token.Type == ImapTokenType.Nil) {
				delim = '\0';
			} else {
				throw ImapEngine.UnexpectedToken (token, false);
			}

			// parse the folder name
			token = engine.ReadToken (ic.CancellationToken);

			switch (token.Type) {
			case ImapTokenType.Literal:
				encodedName = engine.ReadLiteral (ic.CancellationToken);
				break;
			case ImapTokenType.QString:
			case ImapTokenType.Atom:
				encodedName = (string) token.Value;
				break;
			default:
				throw ImapEngine.UnexpectedToken (token, false);
			}

			if (engine.FolderCache.TryGetValue (encodedName, out folder)) {
				folder.Attributes = (folder.Attributes & ~(FolderAttributes.Marked | FolderAttributes.Unmarked)) | attrs;
			} else {
				folder = new ImapFolder (engine, encodedName, attrs, delim);
				engine.FolderCache.Add (encodedName, folder);
			}

			list.Add (folder);
		}

		/// <summary>
		/// Looks up and sets the <see cref="ImapFolder.ParentFolder"/> property of each of the folders.
		/// </summary>
		/// <param name="engine">The IMAP engine.</param>
		/// <param name="folders">The IMAP folders.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static void LookupParentFolders (ImapEngine engine, IEnumerable<ImapFolder> folders, CancellationToken cancellationToken)
		{
			int index;

			foreach (var folder in folders) {
				if (folder.ParentFolder != null)
					continue;

				if ((index = folder.FullName.LastIndexOf (folder.DirectorySeparator)) == -1)
					continue;

				if (index == 0)
					continue;

				var parentName = folder.FullName.Substring (0, index);
				var encodedName = ImapEncoding.Encode (parentName);
				ImapFolder parent;

				if (engine.FolderCache.TryGetValue (encodedName, out parent)) {
					folder.ParentFolder = parent;
					continue;
				}

				var ic = engine.QueueCommand (cancellationToken, null, "LIST \"\" %S\r\n", encodedName);
				ic.RegisterUntaggedHandler ("LIST", ImapUtils.HandleUntaggedListResponse);
				ic.UserData = new List<ImapFolder> ();

				engine.Wait (ic);

				if (!engine.FolderCache.TryGetValue (encodedName, out parent)) {
					parent = new ImapFolder (engine, encodedName, FolderAttributes.NonExistent, folder.DirectorySeparator);
					engine.FolderCache.Add (encodedName, parent);
				}

				folder.ParentFolder = parent;
			}
		}

		static string ReadStringToken (ImapEngine engine, CancellationToken cancellationToken)
		{
			var token = engine.ReadToken (cancellationToken);

			switch (token.Type) {
			case ImapTokenType.Literal:
				return engine.ReadLiteral (cancellationToken);
			case ImapTokenType.QString:
			case ImapTokenType.Atom:
				return (string) token.Value;
			default:
				throw ImapEngine.UnexpectedToken (token, false);
			}
		}

		static string ReadNStringToken (ImapEngine engine, bool rfc2047, CancellationToken cancellationToken)
		{
			var token = engine.ReadToken (cancellationToken);
			string value;

			switch (token.Type) {
			case ImapTokenType.Literal:
				value = engine.ReadLiteral (cancellationToken);
				break;
			case ImapTokenType.QString:
			case ImapTokenType.Atom:
				value = (string) token.Value;
				break;
			case ImapTokenType.Nil:
				return null;
			default:
				throw ImapEngine.UnexpectedToken (token, false);
			}

			return rfc2047 ? Rfc2047.DecodeText (Latin1.GetBytes (value)) : value;
		}

		static uint ReadNumber (ImapEngine engine, CancellationToken cancellationToken)
		{
			var token = engine.ReadToken (cancellationToken);
			uint number;

			if (token.Type != ImapTokenType.Atom || !uint.TryParse ((string) token.Value, out number))
				throw ImapEngine.UnexpectedToken (token, false);

			return number;
		}

		static void ParseParameterList (ParameterList paramList, ImapEngine engine, CancellationToken cancellationToken)
		{
			ImapToken token;

			do {
				token = engine.PeekToken (cancellationToken);

				if (token.Type == ImapTokenType.CloseParen)
					break;

				var name = ReadStringToken (engine, cancellationToken);
				var value = ReadStringToken (engine, cancellationToken);

				// FIXME: may need to decode params
				paramList[name] = value;
			} while (true);

			// read the ')'
			engine.ReadToken (cancellationToken);
		}

		static ContentType ParseContentType (ImapEngine engine, CancellationToken cancellationToken)
		{
			var type = ReadStringToken (engine, cancellationToken);
			var subtype = ReadStringToken (engine, cancellationToken);
			var contentType = new ContentType (type, subtype);
			var token = engine.ReadToken (cancellationToken);

			if (token.Type == ImapTokenType.Nil)
				return contentType;

			if (token.Type != ImapTokenType.OpenParen)
				throw ImapEngine.UnexpectedToken (token, false);

			ParseParameterList (contentType.Parameters, engine, cancellationToken);

			return contentType;
		}

		static ContentDisposition ParseContentDisposition (ImapEngine engine, CancellationToken cancellationToken)
		{
			var token = engine.ReadToken (cancellationToken);

			if (token.Type == ImapTokenType.Nil)
				return null;

			if (token.Type != ImapTokenType.OpenParen)
				throw ImapEngine.UnexpectedToken (token, false);

			var dsp = ReadStringToken (engine, cancellationToken);
			var disposition = new ContentDisposition (dsp);

			token = engine.ReadToken (cancellationToken);

			if (token.Type == ImapTokenType.OpenParen)
				ParseParameterList (disposition.Parameters, engine, cancellationToken);
			else if (token.Type != ImapTokenType.Nil)
				throw ImapEngine.UnexpectedToken (token, false);

			token = engine.ReadToken (cancellationToken);

			if (token.Type != ImapTokenType.CloseParen)
				throw ImapEngine.UnexpectedToken (token, false);

			return disposition;
		}

		static string[] ParseContentLocation (ImapEngine engine, CancellationToken cancellationToken)
		{
			var token = engine.ReadToken (cancellationToken);
			var languages = new List<string> ();
			string language;

			switch (token.Type) {
			case ImapTokenType.Literal:
				language = engine.ReadLiteral (cancellationToken);
				languages.Add (language);
				break;
			case ImapTokenType.QString:
			case ImapTokenType.Atom:
				language = (string) token.Value;
				languages.Add (language);
				break;
			case ImapTokenType.Nil:
				return null;
			case ImapTokenType.OpenParen:
				do {
					token = engine.PeekToken (cancellationToken);

					if (token.Type == ImapTokenType.CloseParen)
						break;

					language = ReadStringToken (engine, cancellationToken);
					languages.Add (language);
				} while (true);

				// read the ')'
				engine.ReadToken (cancellationToken);
				break;
			default:
				throw ImapEngine.UnexpectedToken (token, false);
			}

			return languages.ToArray ();
		}

		static void SkipBodyExtensions (ImapEngine engine, CancellationToken cancellationToken)
		{
			var token = engine.ReadToken (cancellationToken);

			switch (token.Type) {
			case ImapTokenType.OpenParen:
				do {
					token = engine.PeekToken (cancellationToken);

					if (token.Type == ImapTokenType.CloseParen)
						break;

					SkipBodyExtensions (engine, cancellationToken);
				} while (true);

				// read the ')'
				engine.ReadToken (cancellationToken);
				break;
			case ImapTokenType.Literal:
				engine.ReadLiteral (cancellationToken);
				break;
			case ImapTokenType.QString:
			case ImapTokenType.Atom:
			case ImapTokenType.Nil:
				break;
			default:
				throw ImapEngine.UnexpectedToken (token, false);
			}
		}

		static BodyPart ParseMultipart (ImapEngine engine, CancellationToken cancellationToken)
		{
			var body = new BodyPartMultipart ();
			ImapToken token;

			do {
				body.BodyParts.Add (ParseBody (engine, cancellationToken));
				token = engine.PeekToken (cancellationToken);
			} while (token.Type == ImapTokenType.OpenParen);

			var subtype = ReadStringToken (engine, cancellationToken);

			body.ContentType = new ContentType ("multipart", subtype);

			token = engine.PeekToken (cancellationToken);

			if (token.Type != ImapTokenType.CloseParen) {
				token = engine.ReadToken (cancellationToken);

				if (token.Type != ImapTokenType.OpenParen)
					throw ImapEngine.UnexpectedToken (token, false);

				ParseParameterList (body.ContentType.Parameters, engine, cancellationToken);
				token = engine.PeekToken (cancellationToken);
			}

			if (token.Type != ImapTokenType.CloseParen) {
				body.ContentDisposition = ParseContentDisposition (engine, cancellationToken);
				token = engine.PeekToken (cancellationToken);
			}

			if (token.Type != ImapTokenType.CloseParen) {
				body.ContentLanguage = ParseContentLocation (engine, cancellationToken);
				token = engine.PeekToken (cancellationToken);
			}

			if (token.Type != ImapTokenType.CloseParen) {
				body.ContentLocation = ReadNStringToken (engine, false, cancellationToken);
				token = engine.PeekToken (cancellationToken);
			}

			if (token.Type != ImapTokenType.CloseParen)
				SkipBodyExtensions (engine, cancellationToken);

			// read the ')'
			token = engine.ReadToken (cancellationToken);

			if (token.Type != ImapTokenType.CloseParen)
				throw ImapEngine.UnexpectedToken (token, false);

			return body;
		}

		public static BodyPart ParseBody (ImapEngine engine, CancellationToken cancellationToken)
		{
			var token = engine.ReadToken (cancellationToken);

			if (token.Type == ImapTokenType.Nil)
				return null;

			if (token.Type != ImapTokenType.OpenParen)
				throw ImapEngine.UnexpectedToken (token, false);

			token = engine.PeekToken (cancellationToken);

			if (token.Type == ImapTokenType.OpenParen)
				return ParseMultipart (engine, cancellationToken);

			var type = ParseContentType (engine, cancellationToken);
			var id = ReadNStringToken (engine, false, cancellationToken);
			var desc = ReadNStringToken (engine, true, cancellationToken);
			var enc = ReadStringToken (engine, cancellationToken);
			var octets = ReadNumber (engine, cancellationToken);
			BodyPartBasic body;

			if (type.Matches ("message", "rfc822")) {
				var mesg = new BodyPartMessage ();
				mesg.Envelope = ParseEnvelope (engine, cancellationToken);
				mesg.Body = ParseBody (engine, cancellationToken);
				mesg.Lines = ReadNumber (engine, cancellationToken);
				body = mesg;
			} else if (type.Matches ("text", "*")) {
				var text = new BodyPartText ();
				text.Lines = ReadNumber (engine, cancellationToken);
				body = text;
			} else {
				body = new BodyPartBasic ();
			}

			body.ContentTransferEncoding = enc;
			body.ContentDescription = desc;
			body.ContentType = type;
			body.ContentId = id;
			body.Octets = octets;

			// if we are parsing a BODYSTRUCTURE, we may get some more tokens before the ')'
			token = engine.PeekToken (cancellationToken);

			if (token.Type != ImapTokenType.CloseParen) {
				body.ContentMd5 = ReadNStringToken (engine, false, cancellationToken);
				token = engine.PeekToken (cancellationToken);
			}

			if (token.Type != ImapTokenType.CloseParen) {
				body.ContentDisposition = ParseContentDisposition (engine, cancellationToken);
				token = engine.PeekToken (cancellationToken);
			}

			if (token.Type != ImapTokenType.CloseParen) {
				body.ContentLanguage = ParseContentLocation (engine, cancellationToken);
				token = engine.PeekToken (cancellationToken);
			}

			if (token.Type != ImapTokenType.CloseParen) {
				body.ContentLocation = ReadNStringToken (engine, false, cancellationToken);
				token = engine.PeekToken (cancellationToken);
			}

			if (token.Type != ImapTokenType.CloseParen)
				SkipBodyExtensions (engine, cancellationToken);

			// read the ')'
			token = engine.ReadToken (cancellationToken);

			if (token.Type != ImapTokenType.CloseParen)
				throw ImapEngine.UnexpectedToken (token, false);

			return body;
		}

		static void AddEnvelopeAddress (InternetAddressList list, ImapEngine engine, CancellationToken cancellationToken)
		{
			string[] values = new string[4];
			ImapToken token;
			int index = 0;

			do {
				token = engine.ReadToken (cancellationToken);

				switch (token.Type) {
				case ImapTokenType.Literal:
					values[index] = engine.ReadLiteral (cancellationToken);
					break;
				case ImapTokenType.QString:
				case ImapTokenType.Atom:
					values[index] = (string) token.Value;
					break;
				case ImapTokenType.Nil:
					break;
				default:
					throw ImapEngine.UnexpectedToken (token, false);
				}

				index++;
			} while (index < 4);

			token = engine.ReadToken (cancellationToken);

			if (token.Type != ImapTokenType.CloseParen)
				throw ImapEngine.UnexpectedToken (token, false);

			string name = null;

			if (values[0] != null) {
				// Note: since the ImapEngine.ReadLiteral() uses iso-8859-1
				// to convert bytes to unicode, we can undo that here:
				name = Rfc2047.DecodePhrase (Latin1.GetBytes (values[0]));
			}

			string address = values[3] != null ? values[2] + "@" + values[3] : values[2];

			list.Add (new MailboxAddress (name, address));
		}

		static InternetAddressList ParseEnvelopeAddressList (ImapEngine engine, CancellationToken cancellationToken)
		{
			var token = engine.ReadToken (cancellationToken);
			var list = new InternetAddressList ();

			if (token.Type == ImapTokenType.Nil)
				return list;

			if (token.Type != ImapTokenType.OpenParen)
				throw ImapEngine.UnexpectedToken (token, false);

			do {
				token = engine.ReadToken (cancellationToken);

				if (token.Type == ImapTokenType.CloseParen)
					break;

				if (token.Type != ImapTokenType.OpenParen)
					throw ImapEngine.UnexpectedToken (token, false);

				AddEnvelopeAddress (list, engine, cancellationToken);
			} while (true);

			return list;
		}

		static DateTimeOffset? ParseEnvelopeDate (ImapEngine engine, CancellationToken cancellationToken)
		{
			var token = engine.ReadToken (cancellationToken);
			DateTimeOffset date;
			string value;

			switch (token.Type) {
			case ImapTokenType.Literal:
				value = engine.ReadLiteral (cancellationToken);
				break;
			case ImapTokenType.QString:
			case ImapTokenType.Atom:
				value = (string) token.Value;
				break;
			case ImapTokenType.Nil:
				return null;
			default:
				throw ImapEngine.UnexpectedToken (token, false);
			}

			if (!DateUtils.TryParseDateTime (value, out date))
				return null;

			return date;
		}

		/// <summary>
		/// Parses the ENVELOPE parenthesized list.
		/// </summary>
		/// <returns>The envelope.</returns>
		/// <param name="engine">The IMAP engine.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Envelope ParseEnvelope (ImapEngine engine, CancellationToken cancellationToken)
		{
			var token = engine.ReadToken (cancellationToken);
			var envelope = new Envelope ();
			string nstring;

			if (token.Type != ImapTokenType.OpenParen)
				throw ImapEngine.UnexpectedToken (token, false);

			envelope.Date = ParseEnvelopeDate (engine, cancellationToken);
			envelope.Subject = ReadNStringToken (engine, true, cancellationToken);
			envelope.From = ParseEnvelopeAddressList (engine, cancellationToken);
			envelope.Sender = ParseEnvelopeAddressList (engine, cancellationToken);
			envelope.ReplyTo = ParseEnvelopeAddressList (engine, cancellationToken);
			envelope.To = ParseEnvelopeAddressList (engine, cancellationToken);
			envelope.Cc = ParseEnvelopeAddressList (engine, cancellationToken);
			envelope.Bcc = ParseEnvelopeAddressList (engine, cancellationToken);
			envelope.InReplyTo = new MessageIdList ();

			if ((nstring = ReadNStringToken (engine, false, cancellationToken)) != null) {
				foreach (var msgid in MimeUtils.EnumerateReferences (nstring))
					envelope.InReplyTo.Add (msgid);
			}

			if ((nstring = ReadNStringToken (engine, false, cancellationToken)) != null) {
				envelope.MessageId = MimeUtils.EnumerateReferences (nstring).FirstOrDefault ();
			}

			token = engine.ReadToken (cancellationToken);

			if (token.Type != ImapTokenType.CloseParen)
				throw ImapEngine.UnexpectedToken (token, false);

			return envelope;
		}

		/// <summary>
		/// Formats a flags list suitable for use with the APPEND command.
		/// </summary>
		/// <returns>The flags list string.</returns>
		/// <param name="flags">The message flags.</param>
		public static string FormatFlagsList (MessageFlags flags)
		{
			var builder = new StringBuilder ();

			builder.Append ('(');
			if ((flags & MessageFlags.Answered) != 0)
				builder.Append ("\\Answered ");
			if ((flags & MessageFlags.Deleted) != 0)
				builder.Append ("\\Deleted ");
			if ((flags & MessageFlags.Draft) != 0)
				builder.Append ("\\Draft ");
			if ((flags & MessageFlags.Flagged) != 0)
				builder.Append ("\\Flagged ");
			if ((flags & MessageFlags.Seen) != 0)
				builder.Append ("\\Seen ");
			if (builder.Length > 1)
				builder.Length--;
			builder.Append (')');

			return builder.ToString ();
		}

		/// <summary>
		/// Parses the flags list.
		/// </summary>
		/// <returns>The message flags.</returns>
		/// <param name="engine">The IMAP engine.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static MessageFlags ParseFlagsList (ImapEngine engine, CancellationToken cancellationToken)
		{
			var token = engine.ReadToken (cancellationToken);
			var flags = MessageFlags.None;

			if (token.Type != ImapTokenType.OpenParen) {
				Debug.WriteLine ("Expected '(' at the start of the flags list, but got: {0}", token);
				throw ImapEngine.UnexpectedToken (token, false);
			}

			token = engine.ReadToken (cancellationToken);

			while (token.Type == ImapTokenType.Atom || token.Type == ImapTokenType.Flag) {
				string flag = (string) token.Value;
				switch (flag) {
				case "\\Answered": flags |= MessageFlags.Answered; break;
				case "\\Deleted":  flags |= MessageFlags.Deleted; break;
				case "\\Draft":    flags |= MessageFlags.Draft; break;
				case "\\Flagged":  flags |= MessageFlags.Flagged; break;
				case "\\Seen":     flags |= MessageFlags.Seen; break;
				case "\\Recent":   flags |= MessageFlags.Recent; break;
				case "\\*":        flags |= MessageFlags.UserDefined; break;
				}

				token = engine.ReadToken (cancellationToken);
			}

			if (token.Type != ImapTokenType.CloseParen) {
				Debug.WriteLine ("Expected to find a ')' token terminating the flags list, but got: {0}", token);
				throw ImapEngine.UnexpectedToken (token, false);
			}

			return flags;
		}
	}
}

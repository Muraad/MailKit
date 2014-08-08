﻿//
// BodyPartBasic.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013-2014 Xamarin Inc. (www.xamarin.com)
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
using System.Text;

using MimeKit;

namespace MailKit {
	/// <summary>
	/// A basic message body part.
	/// </summary>
	/// <remarks>
	/// Represents any message body part that is not a multipart,
	/// message/rfc822 part, or a text part.
	/// </remarks>
	public class BodyPartBasic : BodyPart
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.BodyPartBasic"/> class.
		/// </summary>
		/// <remarks>
		/// Creates a new <see cref="BodyPartBasic"/>.
		/// </remarks>
		public BodyPartBasic ()
		{
		}

		/// <summary>
		/// Gets the Content-Id of the body part, if available.
		/// </summary>
		/// <remarks>
		/// Gets the Content-Id of the body part, if available.
		/// </remarks>
		/// <value>The content identifier.</value>
		public string ContentId {
			get; set;
		}

		/// <summary>
		/// Gets the Content-Description of the body part, if available.
		/// </summary>
		/// <remarks>
		/// Gets the Content-Description of the body part, if available.
		/// </remarks>
		/// <value>The content description.</value>
		public string ContentDescription {
			get; set;
		}

		/// <summary>
		/// Gets the Content-Transfer-Encoding of the body part.
		/// </summary>
		/// <remarks>
		/// Gets the Content-Transfer-Encoding of the body part.
		/// </remarks>
		/// <value>The content transfer encoding.</value>
		public string ContentTransferEncoding {
			get; set;
		}

		/// <summary>
		/// Gets the size of the body part, in bytes.
		/// </summary>
		/// <remarks>
		/// Gets the size of the body part, in bytes.
		/// </remarks>
		/// <value>The number of octets.</value>
		public uint Octets {
			get; set;
		}

		/// <summary>
		/// Gets the MD5 hash of the content, if available.
		/// </summary>
		/// <remarks>
		/// Gets the MD5 hash of the content, if available.
		/// </remarks>
		/// <value>The content md5.</value>
		public string ContentMd5 {
			get; set;
		}

		/// <summary>
		/// Gets the Content-Disposition of the body part, if available.
		/// </summary>
		/// <remarks>
		/// Gets the Content-Disposition of the body part, if available.
		/// </remarks>
		/// <value>The content disposition.</value>
		public ContentDisposition ContentDisposition {
			get; set;
		}

		/// <summary>
		/// Gets the Content-Language of the body part, if available.
		/// </summary>
		/// <remarks>
		/// Gets the Content-Language of the body part, if available.
		/// </remarks>
		/// <value>The content language.</value>
		public string[] ContentLanguage {
			get; set;
		}

		/// <summary>
		/// Gets the Content-Location of the body part, if available.
		/// </summary>
		/// <remarks>
		/// Gets the Content-Location of the body part, if available.
		/// </remarks>
		/// <value>The content location.</value>
		public Uri ContentLocation {
			get; set;
		}

		/// <summary>
		/// Determines whether or not the body part is an attachment.
		/// </summary>
		/// <remarks>
		/// Determines whether or not the body part is an attachment.
		/// </remarks>
		/// <value><c>true</c> if thie part is an attachment; otherwise, <c>false</c>.</value>
		public bool IsAttachment {
			get { return ContentDisposition != null && ContentDisposition.IsAttachment; }
		}

		/// <summary>
		/// Encodes the <see cref="BodyPart"/> into the <see cref="System.Text.StringBuilder"/>.
		/// </summary>
		/// <remarks>
		/// Encodes the <see cref="BodyPart"/> into the <see cref="System.Text.StringBuilder"/>.
		/// </remarks>
		/// <param name="builder">The string builder.</param>
		protected override void Encode (StringBuilder builder)
		{
			Encode (builder, ContentType);
			builder.Append (' ');
			Encode (builder, ContentId);
			builder.Append (' ');
			Encode (builder, ContentDescription);
			builder.Append (' ');
			Encode (builder, ContentTransferEncoding);
			builder.Append (' ');
			Encode (builder, Octets);
			builder.Append (' ');
			Encode (builder, ContentMd5);
			builder.Append (' ');
			Encode (builder, ContentDisposition);
			builder.Append (' ');
			Encode (builder, ContentLanguage);
			builder.Append (' ');
			Encode (builder, ContentLocation);
		}
	}
}

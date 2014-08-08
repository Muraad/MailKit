﻿//
// MailStore.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (www.xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;

namespace MailKit {
	/// <summary>
	/// An abstract mail store implementation.
	/// </summary>
	/// <remarks>
	/// An abstract mail store implementation.
	/// </remarks>
	public abstract class MailStore : MailService, IMailStore
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MailKit.MailStore"/> class.
		/// </summary>
		/// <remarks>
		/// Initializes a new instance of the <see cref="MailKit.MailStore"/> class.
		/// </remarks>
		protected MailStore ()
		{
		}

		/// <summary>
		/// Gets the personal namespaces.
		/// </summary>
		/// <remarks>
		/// The personal folder namespaces contain a user's personal mailbox folders.
		/// </remarks>
		/// <value>The personal namespaces.</value>
		public abstract FolderNamespaceCollection PersonalNamespaces {
			get;
		}

		/// <summary>
		/// Gets the shared namespaces.
		/// </summary>
		/// <remarks>
		/// The shared folder namespaces contain mailbox folders that are shared with the user.
		/// </remarks>
		/// <value>The shared namespaces.</value>
		public abstract FolderNamespaceCollection SharedNamespaces {
			get;
		}

		/// <summary>
		/// Gets the other namespaces.
		/// </summary>
		/// <remarks>
		/// The other folder namespaces contain other mailbox folders.
		/// </remarks>
		/// <value>The other namespaces.</value>
		public abstract FolderNamespaceCollection OtherNamespaces {
			get;
		}

		/// <summary>
		/// Get whether or not the mail store supports quotas.
		/// </summary>
		/// <remarks>
		/// Gets whether or not the mail store supports quotas.
		/// </remarks>
		/// <value><c>true</c> if the mail store supports quotas; otherwise, <c>false</c>.</value>
		public abstract bool SupportsQuotas {
			get;
		}

		/// <summary>
		/// Gets the Inbox folder.
		/// </summary>
		/// <remarks>
		/// The Inbox folder is the default folder and always exists.
		/// </remarks>
		/// <value>The Inbox folder.</value>
		public abstract IMailFolder Inbox {
			get;
		}

		/// <summary>
		/// Enable the quick resynchronization feature.
		/// </summary>
		/// <remarks>
		/// <para>Enables quick resynchronization when a folder is opened using the
		/// <see cref="MailFolder.Open(FolderAccess,UniqueId,ulong,System.Collections.Generic.IList&lt;UniqueId&gt;,System.Threading.CancellationToken)"/>
		/// method.</para>
		/// <para>If this feature is enabled, the <see cref="MailFolder.MessageExpunged"/> event
		/// is replaced with the <see cref="MailFolder.MessagesVanished"/> event.</para>
		/// <para>This method needs to be called immediately after
		/// <see cref="MailService.Authenticate(System.Net.ICredentials,System.Threading.CancellationToken)"/>,
		/// before the opening of any folders.</para>
		/// </remarks>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="MailStore"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="MailStore"/> is not connected, not authenticated, or a folder has been selected.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The mail store does not support quick resynchronization.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		public abstract void EnableQuickResync (CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Asynchronously enable the quick resynchronization feature.
		/// </summary>
		/// <remarks>
		/// <para>Enables quick resynchronization when a folder is opened using the
		/// <see cref="MailFolder.Open(FolderAccess,UniqueId,ulong,System.Collections.Generic.IList&lt;UniqueId&gt;,System.Threading.CancellationToken)"/>
		/// method.</para>
		/// <para>If this feature is enabled, the <see cref="MailFolder.MessageExpunged"/> event
		/// is replaced with the <see cref="MailFolder.MessagesVanished"/> event.</para>
		/// <para>This method needs to be called immediately after
		/// <see cref="MailService.Authenticate(System.Net.ICredentials,System.Threading.CancellationToken)"/>,
		/// before the opening of any folders.</para>
		/// </remarks>
		/// <returns>An asynchronous task context.</returns>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="MailStore"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// The <see cref="MailStore"/> is not connected, not authenticated, or a folder has been selected.
		/// </exception>
		/// <exception cref="System.NotSupportedException">
		/// The mail store does not support quick resynchronization.
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// An I/O error occurred.
		/// </exception>
		/// <exception cref="ProtocolException">
		/// A protocol error occurred.
		/// </exception>
		public virtual Task EnableQuickResyncAsync (CancellationToken cancellationToken = default (CancellationToken))
		{
			return Task.Factory.StartNew (() => {
				lock (SyncRoot) {
					EnableQuickResync (cancellationToken);
				}
			}, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
		}

		/// <summary>
		/// Get the specified special folder.
		/// </summary>
		/// <remarks>
		/// Not all mail stores support special folders. Each implementation
		/// should provide a way to determine if special folders are supported.
		/// </remarks>
		/// <returns>The folder if available; otherwise <c>null</c>.</returns>
		/// <param name="folder">The type of special folder.</param>
		/// <exception cref="System.ArgumentOutOfRangeException">
		/// <paramref name="folder"/> is out of range.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="MailStore"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <para>The <see cref="MailStore"/> is not connected.</para>
		/// <para>-or-</para>
		/// <para>The <see cref="MailStore"/> is not authenticated.</para>
		/// </exception>
		public abstract IMailFolder GetFolder (SpecialFolder folder);

		/// <summary>
		/// Get the folder for the specified namespace.
		/// </summary>
		/// <remarks>
		/// Gets the folder for the specified namespace.
		/// </remarks>
		/// <returns>The folder.</returns>
		/// <param name="namespace">The namespace.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="namespace"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="MailStore"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <para>The <see cref="MailStore"/> is not connected.</para>
		/// <para>-or-</para>
		/// <para>The <see cref="MailStore"/> is not authenticated.</para>
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The folder could not be found.
		/// </exception>
		public abstract IMailFolder GetFolder (FolderNamespace @namespace);

		/// <summary>
		/// Get the folder for the specified path.
		/// </summary>
		/// <remarks>
		/// Gets the folder for the specified path.
		/// </remarks>
		/// <returns>The folder.</returns>
		/// <param name="path">The folder path.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="path"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="MailStore"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <para>The <see cref="MailStore"/> is not connected.</para>
		/// <para>-or-</para>
		/// <para>The <see cref="MailStore"/> is not authenticated.</para>
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The folder could not be found.
		/// </exception>
		public abstract IMailFolder GetFolder (string path, CancellationToken cancellationToken = default (CancellationToken));

		/// <summary>
		/// Asynchronously get the folder for the specified path.
		/// </summary>
		/// <remarks>
		/// Asynchronously gets the folder for the specified path.
		/// </remarks>
		/// <returns>The folder.</returns>
		/// <param name="path">The folder path.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <exception cref="System.ArgumentNullException">
		/// <paramref name="path"/> is <c>null</c>.
		/// </exception>
		/// <exception cref="System.ObjectDisposedException">
		/// The <see cref="MailStore"/> has been disposed.
		/// </exception>
		/// <exception cref="System.InvalidOperationException">
		/// <para>The <see cref="MailStore"/> is not connected.</para>
		/// <para>-or-</para>
		/// <para>The <see cref="MailStore"/> is not authenticated.</para>
		/// </exception>
		/// <exception cref="System.OperationCanceledException">
		/// The operation was canceled via the cancellation token.
		/// </exception>
		/// <exception cref="FolderNotFoundException">
		/// The folder could not be found.
		/// </exception>
		public virtual Task<IMailFolder> GetFolderAsync (string path, CancellationToken cancellationToken = default (CancellationToken))
		{
			if (path == null)
				throw new ArgumentNullException ("path");

			return Task.Factory.StartNew (() => {
				lock (SyncRoot) {
					return GetFolder (path, cancellationToken);
				}
			}, cancellationToken, TaskCreationOptions.None, TaskScheduler.Default);
		}

		/// <summary>
		/// Occurs when a remote message store receives an alert message from the server.
		/// </summary>
		/// <remarks>
		/// The <see cref="Alert"/> event is raised whenever the mail server sends an
		/// alert message.
		/// </remarks>
		public event EventHandler<AlertEventArgs> Alert;

		/// <summary>
		/// Raise the alert event.
		/// </summary>
		/// <remarks>
		/// Raises the alert event.
		/// </remarks>
		/// <param name="e">The alert event args.</param>
		protected virtual void OnAlert (AlertEventArgs e)
		{
			var handler = Alert;

			if (handler != null)
				handler (this, e);
		}
	}
}

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.VisualBasic.CompilerServices;

namespace LCARSMail.My;

[GeneratedCode("MyTemplate", "14.0.0.0")]
[HideModuleName]
[StandardModule]
internal sealed class MyProject
{
	[MyGroupCollection("System.Windows.Forms.Form", "Create__Instance__", "Dispose__Instance__", "My.MyProject.Forms")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal sealed class MyForms
	{
		public frmAddressBook m_frmAddressBook;

		public frmCalDavSync m_frmCalDavSync;

		public frmCardDavSync m_frmCardDavSync;

		public frmChat m_frmChat;

		public frmContactEdit m_frmContactEdit;

		public frmEwsSync m_frmEwsSync;

		public frmFilters m_frmFilters;

		public frmGlodaSavedSearchPicker m_frmGlodaSavedSearchPicker;

		public frmNews m_frmNews;

		public frmOfflineSettings m_frmOfflineSettings;

		public frmPgpKeys m_frmPgpKeys;

		public frmRssFeeds m_frmRssFeeds;

		[ThreadStatic]
		private static Hashtable m_FormBeingCreated;

		public frmAddressBook frmAddressBook
		{
			[DebuggerNonUserCode]
			get
			{
				m_frmAddressBook = Create__Instance__(m_frmAddressBook);
				return m_frmAddressBook;
			}
			[DebuggerNonUserCode]
			set
			{
				if (value != m_frmAddressBook)
				{
					if (value != null)
					{
						throw new ArgumentException("Property can only be set to Nothing");
					}
					Dispose__Instance__(ref m_frmAddressBook);
				}
			}
		}

		public frmCalDavSync frmCalDavSync
		{
			[DebuggerNonUserCode]
			get
			{
				m_frmCalDavSync = Create__Instance__(m_frmCalDavSync);
				return m_frmCalDavSync;
			}
			[DebuggerNonUserCode]
			set
			{
				if (value != m_frmCalDavSync)
				{
					if (value != null)
					{
						throw new ArgumentException("Property can only be set to Nothing");
					}
					Dispose__Instance__(ref m_frmCalDavSync);
				}
			}
		}

		public frmCardDavSync frmCardDavSync
		{
			[DebuggerNonUserCode]
			get
			{
				m_frmCardDavSync = Create__Instance__(m_frmCardDavSync);
				return m_frmCardDavSync;
			}
			[DebuggerNonUserCode]
			set
			{
				if (value != m_frmCardDavSync)
				{
					if (value != null)
					{
						throw new ArgumentException("Property can only be set to Nothing");
					}
					Dispose__Instance__(ref m_frmCardDavSync);
				}
			}
		}

		public frmChat frmChat
		{
			[DebuggerNonUserCode]
			get
			{
				m_frmChat = Create__Instance__(m_frmChat);
				return m_frmChat;
			}
			[DebuggerNonUserCode]
			set
			{
				if (value != m_frmChat)
				{
					if (value != null)
					{
						throw new ArgumentException("Property can only be set to Nothing");
					}
					Dispose__Instance__(ref m_frmChat);
				}
			}
		}

		public frmContactEdit frmContactEdit
		{
			[DebuggerNonUserCode]
			get
			{
				m_frmContactEdit = Create__Instance__(m_frmContactEdit);
				return m_frmContactEdit;
			}
			[DebuggerNonUserCode]
			set
			{
				if (value != m_frmContactEdit)
				{
					if (value != null)
					{
						throw new ArgumentException("Property can only be set to Nothing");
					}
					Dispose__Instance__(ref m_frmContactEdit);
				}
			}
		}

		public frmEwsSync frmEwsSync
		{
			[DebuggerNonUserCode]
			get
			{
				m_frmEwsSync = Create__Instance__(m_frmEwsSync);
				return m_frmEwsSync;
			}
			[DebuggerNonUserCode]
			set
			{
				if (value != m_frmEwsSync)
				{
					if (value != null)
					{
						throw new ArgumentException("Property can only be set to Nothing");
					}
					Dispose__Instance__(ref m_frmEwsSync);
				}
			}
		}

		public frmFilters frmFilters
		{
			[DebuggerNonUserCode]
			get
			{
				m_frmFilters = Create__Instance__(m_frmFilters);
				return m_frmFilters;
			}
			[DebuggerNonUserCode]
			set
			{
				if (value != m_frmFilters)
				{
					if (value != null)
					{
						throw new ArgumentException("Property can only be set to Nothing");
					}
					Dispose__Instance__(ref m_frmFilters);
				}
			}
		}

		public frmGlodaSavedSearchPicker frmGlodaSavedSearchPicker
		{
			[DebuggerNonUserCode]
			get
			{
				m_frmGlodaSavedSearchPicker = Create__Instance__(m_frmGlodaSavedSearchPicker);
				return m_frmGlodaSavedSearchPicker;
			}
			[DebuggerNonUserCode]
			set
			{
				if (value != m_frmGlodaSavedSearchPicker)
				{
					if (value != null)
					{
						throw new ArgumentException("Property can only be set to Nothing");
					}
					Dispose__Instance__(ref m_frmGlodaSavedSearchPicker);
				}
			}
		}

		public frmNews frmNews
		{
			[DebuggerNonUserCode]
			get
			{
				m_frmNews = Create__Instance__(m_frmNews);
				return m_frmNews;
			}
			[DebuggerNonUserCode]
			set
			{
				if (value != m_frmNews)
				{
					if (value != null)
					{
						throw new ArgumentException("Property can only be set to Nothing");
					}
					Dispose__Instance__(ref m_frmNews);
				}
			}
		}

		public frmOfflineSettings frmOfflineSettings
		{
			[DebuggerNonUserCode]
			get
			{
				m_frmOfflineSettings = Create__Instance__(m_frmOfflineSettings);
				return m_frmOfflineSettings;
			}
			[DebuggerNonUserCode]
			set
			{
				if (value != m_frmOfflineSettings)
				{
					if (value != null)
					{
						throw new ArgumentException("Property can only be set to Nothing");
					}
					Dispose__Instance__(ref m_frmOfflineSettings);
				}
			}
		}

		public frmPgpKeys frmPgpKeys
		{
			[DebuggerNonUserCode]
			get
			{
				m_frmPgpKeys = Create__Instance__(m_frmPgpKeys);
				return m_frmPgpKeys;
			}
			[DebuggerNonUserCode]
			set
			{
				if (value != m_frmPgpKeys)
				{
					if (value != null)
					{
						throw new ArgumentException("Property can only be set to Nothing");
					}
					Dispose__Instance__(ref m_frmPgpKeys);
				}
			}
		}

		public frmRssFeeds frmRssFeeds
		{
			[DebuggerNonUserCode]
			get
			{
				m_frmRssFeeds = Create__Instance__(m_frmRssFeeds);
				return m_frmRssFeeds;
			}
			[DebuggerNonUserCode]
			set
			{
				if (value != m_frmRssFeeds)
				{
					if (value != null)
					{
						throw new ArgumentException("Property can only be set to Nothing");
					}
					Dispose__Instance__(ref m_frmRssFeeds);
				}
			}
		}

		[DebuggerHidden]
		private static T Create__Instance__<T>(T Instance) where T : Form, new()
		{
			if (Instance == null || Instance.IsDisposed)
			{
				if (m_FormBeingCreated != null)
				{
					if (m_FormBeingCreated.ContainsKey(typeof(T)))
					{
						throw new InvalidOperationException(Utils.GetResourceString("WinForms_RecursiveFormCreate"));
					}
				}
				else
				{
					m_FormBeingCreated = new Hashtable();
				}
				m_FormBeingCreated.Add(typeof(T), null);
				try
				{
					return new T();
				}
				catch (TargetInvocationException ex) when (((Func<bool>)delegate
				{
					// Could not convert BlockContainer to single expression
					ProjectData.SetProjectError(ex);
					return ex.InnerException != null;
				}).Invoke())
				{
					string resourceString = Utils.GetResourceString("WinForms_SeeInnerException", ex.InnerException.Message);
					throw new InvalidOperationException(resourceString, ex.InnerException);
				}
				finally
				{
					m_FormBeingCreated.Remove(typeof(T));
				}
			}
			return Instance;
		}

		[DebuggerHidden]
		private void Dispose__Instance__<T>(ref T instance) where T : Form
		{
			instance.Dispose();
			instance = null;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		[DebuggerHidden]
		public MyForms()
		{
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object o)
		{
			return base.Equals(RuntimeHelpers.GetObjectValue(o));
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal new Type GetType()
		{
			return typeof(MyForms);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		public override string ToString()
		{
			return base.ToString();
		}
	}

	[MyGroupCollection("System.Web.Services.Protocols.SoapHttpClientProtocol", "Create__Instance__", "Dispose__Instance__", "")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal sealed class MyWebServices
	{
		[DebuggerHidden]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override bool Equals(object o)
		{
			return base.Equals(RuntimeHelpers.GetObjectValue(o));
		}

		[DebuggerHidden]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		[DebuggerHidden]
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal new Type GetType()
		{
			return typeof(MyWebServices);
		}

		[DebuggerHidden]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override string ToString()
		{
			return base.ToString();
		}

		[DebuggerHidden]
		private static T Create__Instance__<T>(T instance) where T : new()
		{
			if (instance == null)
			{
				return new T();
			}
			return instance;
		}

		[DebuggerHidden]
		private void Dispose__Instance__<T>(ref T instance)
		{
			instance = default(T);
		}

		[DebuggerHidden]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public MyWebServices()
		{
		}
	}

	[ComVisible(false)]
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal sealed class ThreadSafeObjectProvider<T> where T : new()
	{
		[CompilerGenerated]
		[ThreadStatic]
		private static T m_ThreadStaticValue;

		internal T GetInstance
		{
			[DebuggerHidden]
			get
			{
				if (m_ThreadStaticValue == null)
				{
					m_ThreadStaticValue = new T();
				}
				return m_ThreadStaticValue;
			}
		}

		[DebuggerHidden]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public ThreadSafeObjectProvider()
		{
		}
	}

	private static readonly ThreadSafeObjectProvider<MyComputer> m_ComputerObjectProvider = new ThreadSafeObjectProvider<MyComputer>();

	private static readonly ThreadSafeObjectProvider<MyApplication> m_AppObjectProvider = new ThreadSafeObjectProvider<MyApplication>();

	private static readonly ThreadSafeObjectProvider<User> m_UserObjectProvider = new ThreadSafeObjectProvider<User>();

	private static ThreadSafeObjectProvider<MyForms> m_MyFormsObjectProvider = new ThreadSafeObjectProvider<MyForms>();

	private static readonly ThreadSafeObjectProvider<MyWebServices> m_MyWebServicesObjectProvider = new ThreadSafeObjectProvider<MyWebServices>();

	[HelpKeyword("My.Computer")]
	internal static MyComputer Computer
	{
		[DebuggerHidden]
		get
		{
			return m_ComputerObjectProvider.GetInstance;
		}
	}

	[HelpKeyword("My.Application")]
	internal static MyApplication Application
	{
		[DebuggerHidden]
		get
		{
			return m_AppObjectProvider.GetInstance;
		}
	}

	[HelpKeyword("My.User")]
	internal static User User
	{
		[DebuggerHidden]
		get
		{
			return m_UserObjectProvider.GetInstance;
		}
	}

	[HelpKeyword("My.Forms")]
	internal static MyForms Forms
	{
		[DebuggerHidden]
		get
		{
			return m_MyFormsObjectProvider.GetInstance;
		}
	}

	[HelpKeyword("My.WebServices")]
	internal static MyWebServices WebServices
	{
		[DebuggerHidden]
		get
		{
			return m_MyWebServicesObjectProvider.GetInstance;
		}
	}
}

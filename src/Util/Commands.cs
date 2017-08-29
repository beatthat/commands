using UnityEngine;
using System.Collections.Generic;
using System;

namespace BeatThat.App
{
	/// <summary>
	/// Singleton Front Controller that manages a set of global Commands for an app.
	/// </summary>
	public class Commands : MonoBehaviour
	{
		public delegate Command CommandFactory();

		private static Commands INSTANCE;

		void Awake() 
		{
			INSTANCE = this;
			if(this.transform.root == this.transform) {
				DontDestroyOnLoad(this.gameObject);
			}
			Init();
		}

		public static Commands Get() 
		{
			if(INSTANCE == null) {
				INSTANCE = FindObjectOfType(typeof(Commands)) as Commands;
				if(INSTANCE == null) {
					INSTANCE = new GameObject("Controller").AddComponent<Commands>();
				}
			}
			return INSTANCE;
		}

		[Obsolete("use Init")]
		public void InitController()
		{
			Init();
		}

		public void Init()
		{
			if(!m_hasInit) {
				
				using(var tmp = ListPool<RegistersCommand>.Get()) {
					GetComponentsInChildren<RegistersCommand>(true, tmp);
					foreach(var cb in tmp) {
						cb.RegisterTo(this);
					}
				}
				m_hasInit = true;
			}
		}

		[Obsolete("use Execute")]
		public void ExecuteCommand(Notification n) 
		{
			Execute(n);
		}

		public void Execute(Notification n) 
		{
			CommandFactory fac;
			if(m_commandFactoryByType.TryGetValue(n.type, out fac)) {
				Command c = fac();
				if(c != null) {
					c.Execute(n);
				}
				else {
					Debug.LogWarning("[" + Time.time + "] " + GetType() 
					                 + "::ExecuteCommand factory failed to create command for type: '" + n.type + "'");
				}
			}
			else {
				Debug.LogWarning("[" + Time.time + "] " + GetType() 
				                 + "::ExecuteCommand unknown type: '" + n.type + "'");
			}
		}

		[Obsolete("use Add")]
		public void RegisterCommand<T>(string type) where T : Command
		{
			Add<T>(type);
		}

		public void Add<T>(string type) where T : Command
		{
			Add(type, () => (Command)typeof(T).GetConstructor (new Type[0]).Invoke (new object[0]));
		}

		[Obsolete("use Add")]
		public Binding RegisterCommand(string type, CommandFactory factory)
		{
			return Add(type, factory);
		}

		public Binding Add(string type, CommandFactory factory)
		{
			m_commandFactoryByType[type] = factory;
			NotificationBus.Add<Notification>(type, this.Execute, this.gameObject);
			return new CommandBinding(this, type);
		}

		public void UnregisterAllCommands()
		{
			foreach(string type in new List<string>(m_commandFactoryByType.Keys)) {
				UnregisterCommand(type);
			}
		}

		public void UnregisterCommand(string type)
		{
			NotificationBus.Remove<Notification>(type, this.Execute);
			m_commandFactoryByType.Remove(type);
		}

		void OnDestroy()
		{
			UnregisterAllCommands();
		}

		private class CommandBinding : Binding
		{
			public CommandBinding(Commands c, string type)
			{
				m_controller = new SafeRef<Commands>(c);
				m_type = type;
				this.isBound = true;
			}

			#region Binding implementation

			public void Unbind ()
			{
				if(!this.isBound) {
					return;
				}
				if(m_controller.isValid) {
					m_controller.value.UnregisterCommand(m_type);
				}
				this.isBound = false;
			}

			public bool isBound { get; private set; }

			#endregion

			private readonly string m_type;
			private SafeRef<Commands> m_controller;
		}

		private bool m_hasInit;
		private Dictionary<string, CommandFactory> m_commandFactoryByType = new Dictionary<string, CommandFactory>();

	}
}

using UnityEngine;
using System;
using System.Collections.Generic;

namespace BeatThat.App
{
	/// <summary>
	/// Simple component that manages a set of Command's.
	/// 
	/// Can be placed on a scene and configured 
	/// so that Command's that get created/registered when the scene starts
	/// and then unregistered/destroyed when the player leaves the scene.
	/// 
	/// Commands can be configured by either adding CommandBinding gameObjects
	/// as direct children of the CommandSet instance or by overriding 
	/// the template function 'CreateCommandBindings'.
	/// </summary>
	public class CommandSet : MonoBehaviour
	{
		public bool m_registerCommandsOnStart = true;
		public bool m_requireAppStarted = true;

		[NotificationType]
		public const string INIT_SCENE = "init-scene";

		public NotificationType m_sendOnStart = null;

		public NotificationType sendOnStart
		{
			get {
				return m_sendOnStart;
			}
			set {
				m_sendOnStart = value;
			}
		}

		public static CommandSet CreateOn(GameObject go) 
		{
			var c = go.AddComponent<CommandSet>();
			c.m_registerCommandsOnStart = false;
			c.m_requireAppStarted = false;
			return c;
		}

		void Start() 
		{
			if(m_requireAppStarted && AppStartup.StartOnce()) {
				return;
			}

			if(m_registerCommandsOnStart) {
				RegisterCommands();
			}

			if(m_sendOnStart != null && !string.IsNullOrEmpty(m_sendOnStart.notificationType)) {
				NotificationBus.Send(m_sendOnStart.notificationType);
			}
		}

		void OnDestroy()
		{
			UnregisterCommands();
		}

		/// <summary>
		/// Registers the scene commands. Called by Start, but can be force manually
		/// and is safe to call multiple times.
		/// </summary>
		public void RegisterCommands()
		{
			if(this.hasRegistered) {
				return;
			}

			var tmpCmds = ListPool<RegistersCommand>.Get();

			FindCommandBindings(tmpCmds);
			CreateCommandBindings(tmpCmds);

			Commands ctl = this.controller;

			var tmpRegs = ListPool<Binding>.Get();

			foreach(RegistersCommand cb in tmpCmds) {
				var r = cb.RegisterTo(ctl);
				tmpRegs.Add(r);
			}

			ListPool<RegistersCommand>.Return(tmpCmds);

			this.registrations.AddRange(tmpRegs);

			ListPool<Binding>.Return(tmpRegs);

			this.hasRegistered = true;
		}

		/// <summary>
		/// Unregisters the scene commands. Called by Destroy, but can be force manually
		/// and is safe to call multiple times.
		/// </summary>
		public void UnregisterCommands()
		{
			if(!this.hasRegistered) {
				return;
			}

			if(this.registrations.Count > 0) {
				var tmp = ListPool<Binding>.Get();

				tmp.AddRange(this.registrations);

				this.registrations.Clear();

				foreach(Binding b in tmp) {
					b.Unbind();
				}

				ListPool<Binding>.Return(tmp);
			}

			this.hasRegistered = false;
		}

		/// <summary>
		/// Finds any CommandBindings that are children of this CommandSet object in the scene.
		/// </summary>
		/// <param name="workingList">Any command created must be added to this list.</param>
		protected void FindCommandBindings(List<RegistersCommand> workingList)
		{
			GetComponentsInChildren<RegistersCommand>(true, workingList);
		}

		/// <summary>
		/// Override this function to create scene-scoped Command's 
		/// that are created as opposed to found in the scene.
		/// Default implementation does nothing.
		/// </summary>
		/// <param name="workingList">Any command created must be added to this list.</param>
		virtual protected void CreateCommandBindings(List<RegistersCommand> workingList)
		{
		}

		private static readonly Type[] EMPTY_TYPES = new Type[0];
		private static readonly object[] EMPTY_OBJECTS = new object[0];

		/// <summary>
		/// Create a command of the given type and adds it to the set of scene commands.
		/// If commands are already registered, registers the new command. 
		/// If not, the command will be registered on next call to RegisterSceneCommands.
		/// 
		/// By default, sets Command.destroyOnUnbind to true for the new command.
		/// If you don't want this behavoir, just change that property on the returned Command
		/// </summary>
		/// <returns>The self binding.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T AddCommand<T>(bool disableAutoRegistration = false) where T : SingleTypeCommand
		{
			T command;

			if(typeof(Component).IsAssignableFrom(typeof(T))) {
				
				command = (T)((object)(new GameObject(typeof(T).Name).AddComponent(typeof(T))));

				var cmdTrans = (command as Component).transform;
				cmdTrans.SetParent(this.transform, true);
				cmdTrans.localPosition = Vector3.zero;
				cmdTrans.localScale = Vector3.one;
				cmdTrans.localRotation = Quaternion.identity;

				if(command is MonoCommand) {
					(command as MonoCommand).destroyOnUnbind = true;
				}
			}
			else {
				command = (T)typeof(T).GetConstructor(EMPTY_TYPES).Invoke(EMPTY_OBJECTS);
			}

			if(this.hasRegistered && !disableAutoRegistration) {
				var r = command.RegisterTo(this.controller);
				this.registrations.Add(r);
			}

			return command;
		}

		private Commands controller { get { return Commands.Get(); } }

		protected List<Binding> registrations { get { return m_registrations?? (m_registrations = new List<Binding>()); } }
		private List<Binding> m_registrations;

		protected bool hasRegistered { get; private set; }

	}
}

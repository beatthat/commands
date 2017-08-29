using UnityEngine;
using BeatThat.Service;

namespace BeatThat.App
{
	/// <summary>
	/// Base class for a command that lives in the scene (presumably so it can use editor references to other scene objects)
	/// and binds itself to the controller, executing without creating a new instance.
	/// </summary>
	public abstract class MonoCommand : MonoBehaviour, SingleTypeCommand
	{
		public bool m_destoryOnUnbind = false;

		public bool destroyOnUnbind
		{
			get {
				return m_destoryOnUnbind;
			}
			set {
				m_destoryOnUnbind = value;
			}
		}

		/// <summary>
		/// The NotificationType that will trigger this command's execution
		/// </summary>
		protected abstract string type { get; }

		/// <summary>
		/// The execution implementation for the command.
		/// </summary>
		abstract public void Execute(Notification n);

		/// <summary>
		/// CRITICAL NOTE: as of Unity 4.6, you can still only find 'active' objects. 
		/// So if you're using this, the object you're finding needs to be active.
		/// </summary>
		/// <returns>The cached.</returns>
		/// <param name="cache">Cache.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		protected T FindCached<T>(ref T cache) where T : Component
		{
			if(cache == null) {
				cache = GameObject.FindObjectOfType<T>();
			}
			return cache;
		}

		protected T LocateService<T>(ref T cache) where T : class
		{
			if(cache == null) {
				cache = Services.Locate<T>();
			}
			return cache;
		}

		public Binding RegisterTo(Commands c)
		{
			this.controller = c;

			var cRef = new SafeRef<Command>(this);

			var r = c.Add(this.type, cRef.GetValue);
			return new MonoCommandBinding(r, this);
		}

		virtual protected void OnCommandUnregistered()
		{
			if(this.destroyOnUnbind) {
				Destroy(this.gameObject);
			}
		}

		protected Commands controller
		{
			get {
				return m_controller.value;
			}
			set {
				m_controller = new SafeRef<Commands>(value);
			}
		}

		protected System.Action IfValid(System.Action cb) 
		{
			GameObject goRef = this.gameObject;
			
			return SafeCallback.Wrap(cb, () => { 
				return goRef != null; 
			});
		}

		protected System.Action<T> IfValid<T>(System.Action<T> cb) 
		{
			GameObject goRef = this.gameObject;
			
			return SafeCallback.Wrap<T>(cb, () => { 
				return goRef != null; 
			});
		}

		private class MonoCommandBinding : Binding
		{
			public MonoCommandBinding(Binding ctlReg, MonoCommand cmd)
			{
				m_controllerRegistration = ctlReg;
				m_command = new SafeRef<MonoCommand>(cmd);
				this.isBound = true;
			}

			#region Binding implementation

			public void Unbind()
			{
				if(!this.isBound) {
					return;
				}

				m_controllerRegistration.Unbind();

				if(m_command.isValid) {
					m_command.value.OnCommandUnregistered();
				}

				this.isBound = false;
			}

			public bool isBound { get; private set; }

			#endregion

			private Binding m_controllerRegistration;
			private SafeRef<MonoCommand> m_command;
		}

		private SafeRef<MonoCommand> m_bindingRef;
		private SafeRef<Commands> m_controller;
	}
}

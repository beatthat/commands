namespace BeatThat.App
{
	/// <summary>
	/// Responsible for registering/unregistering a single command type to the controlller.
	/// </summary>
	public interface RegistersCommand
	{
		/// <summary>
		/// Registers a command to a given frontcontroller
		/// </summary>
		/// <param name="c">The controller</param>
		Binding RegisterTo(Commands c);
	}
}

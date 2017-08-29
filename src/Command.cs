

namespace BeatThat.App
{
	public interface Command : Command<Notification> {}

	public interface Command<ArgType>
	{
		void Execute(ArgType n);
	}
}

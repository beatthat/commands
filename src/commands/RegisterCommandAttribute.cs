using System;
using BeatThat.Service;

namespace BeatThat.App
{
	/// <summary>
	/// Marks a Command to be autowire registered to Services.
	/// For now, functions in every way identically to RegisterService,
	/// but since registering commands is so common wanted at least the syntactic sugar.
	/// 
	/// @param serviceInterface (optional) interface used to retrieve the Command from services, 
	/// when left null, registers to the concrete type.
	/// 
	/// 
	/// @param priority (optional/default 0) used in the case that multiple implementations 
	/// 	have autowireservice attributes for the same service interface.
	/// 	Resolves to the implementation with the LOWEST priority value.
	/// 
	/// @param proxyInterfaces (optional) list of alternative interfaces that can be used to locate the service
	/// 
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class RegisterCommandAttribute : RegisterServiceAttribute 
	{
		public RegisterCommandAttribute(Type serviceInterface = null, int priority = 0, Type[] proxyInterfaces = null) : base(serviceInterface, proxyInterfaces, priority) {}
	}
}

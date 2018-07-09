using System;

namespace Kernel.ServiceLocator
{
	[AttributeUsage(AttributeTargets.Class)]
	public class RegisterSingletonAttribute : Attribute
	{
		public Type AbstractType { get; set; }


		public RegisterSingletonAttribute(Type abstractType)
		{
			AbstractType = abstractType;
		}
	}
}

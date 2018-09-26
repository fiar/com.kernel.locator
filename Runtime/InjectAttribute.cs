using System;

namespace Kernel.ServiceLocator
{
	[AttributeUsage(AttributeTargets.Field)]
	public class InjectAttribute : Attribute
	{
	}
}

using System;

namespace Kernel.ServiceLocator
{
	[AttributeUsage(AttributeTargets.Field)]
	public class StaticInjectAttribute : Attribute
	{

		public StaticInjectAttribute()
		{
		}
	}
}
